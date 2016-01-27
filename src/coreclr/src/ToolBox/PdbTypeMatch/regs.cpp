// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdafx.h"
#include "cvconst.h"
#include "regs.h"

const wchar_t * const rgRegX86[] = {
    L"None",         // 0   CV_REG_NONE
    L"al",           // 1   CV_REG_AL
    L"cl",           // 2   CV_REG_CL
    L"dl",           // 3   CV_REG_DL
    L"bl",           // 4   CV_REG_BL
    L"ah",           // 5   CV_REG_AH
    L"ch",           // 6   CV_REG_CH
    L"dh",           // 7   CV_REG_DH
    L"bh",           // 8   CV_REG_BH
    L"ax",           // 9   CV_REG_AX
    L"cx",           // 10  CV_REG_CX
    L"dx",           // 11  CV_REG_DX
    L"bx",           // 12  CV_REG_BX
    L"sp",           // 13  CV_REG_SP
    L"bp",           // 14  CV_REG_BP
    L"si",           // 15  CV_REG_SI
    L"di",           // 16  CV_REG_DI
    L"eax",          // 17  CV_REG_EAX
    L"ecx",          // 18  CV_REG_ECX
    L"edx",          // 19  CV_REG_EDX
    L"ebx",          // 20  CV_REG_EBX
    L"esp",          // 21  CV_REG_ESP
    L"ebp",          // 22  CV_REG_EBP
    L"esi",          // 23  CV_REG_ESI
    L"edi",          // 24  CV_REG_EDI
    L"es",           // 25  CV_REG_ES
    L"cs",           // 26  CV_REG_CS
    L"ss",           // 27  CV_REG_SS
    L"ds",           // 28  CV_REG_DS
    L"fs",           // 29  CV_REG_FS
    L"gs",           // 30  CV_REG_GS
    L"IP",           // 31  CV_REG_IP
    L"FLAGS",        // 32  CV_REG_FLAGS
    L"EIP",          // 33  CV_REG_EIP
    L"EFLAGS",       // 34  CV_REG_EFLAG
    L"???",          // 35
    L"???",          // 36
    L"???",          // 37
    L"???",          // 38
    L"???",          // 39
    L"TEMP",         // 40  CV_REG_TEMP
    L"TEMPH"         // 41  CV_REG_TEMPH
    L"QUOTE",        // 42  CV_REG_QUOTE
    L"PCDR3",        // 43  CV_REG_PCDR3
    L"PCDR4",        // 44  CV_REG_PCDR4
    L"PCDR5",        // 45  CV_REG_PCDR5
    L"PCDR6",        // 46  CV_REG_PCDR6
    L"PCDR7",        // 47  CV_REG_PCDR7
    L"???",          // 48
    L"???",          // 49
    L"???",          // 50
    L"???",          // 51
    L"???",          // 52
    L"???",          // 53
    L"???",          // 54
    L"???",          // 55
    L"???",          // 56
    L"???",          // 57
    L"???",          // 58
    L"???",          // 59
    L"???",          // 60
    L"???",          // 61
    L"???",          // 62
    L"???",          // 63
    L"???",          // 64
    L"???",          // 65
    L"???",          // 66
    L"???",          // 67
    L"???",          // 68
    L"???",          // 69
    L"???",          // 70
    L"???",          // 71
    L"???",          // 72
    L"???",          // 73
    L"???",          // 74
    L"???",          // 75
    L"???",          // 76
    L"???",          // 77
    L"???",          // 78
    L"???",          // 79
    L"cr0",          // 80  CV_REG_CR0
    L"cr1",          // 81  CV_REG_CR1
    L"cr2",          // 82  CV_REG_CR2
    L"cr3",          // 83  CV_REG_CR3
    L"cr4",          // 84  CV_REG_CR4
    L"???",          // 85
    L"???",          // 86
    L"???",          // 87
    L"???",          // 88
    L"???",          // 89
    L"dr0",          // 90  CV_REG_DR0
    L"dr1",          // 91  CV_REG_DR1
    L"dr2",          // 92  CV_REG_DR2
    L"dr3",          // 93  CV_REG_DR3
    L"dr4",          // 94  CV_REG_DR4
    L"dr5",          // 95  CV_REG_DR5
    L"dr6",          // 96  CV_REG_DR6
    L"dr7",          // 97  CV_REG_DR7
    L"???",          // 98
    L"???",          // 99
    L"???",          // 10
    L"???",          // 101
    L"???",          // 102
    L"???",          // 103
    L"???",          // 104
    L"???",          // 105
    L"???",          // 106
    L"???",          // 107
    L"???",          // 108
    L"???",          // 109
    L"GDTR",         // 110 CV_REG_GDTR
    L"GDTL",         // 111 CV_REG_GDTL
    L"IDTR",         // 112 CV_REG_IDTR
    L"IDTL",         // 113 CV_REG_IDTL
    L"LDTR",         // 114 CV_REG_LDTR
    L"TR",           // 115 CV_REG_TR
    L"???",          // 116
    L"???",          // 117
    L"???",          // 118
    L"???",          // 119
    L"???",          // 120
    L"???",          // 121
    L"???",          // 122
    L"???",          // 123
    L"???",          // 124
    L"???",          // 125
    L"???",          // 126
    L"???",          // 127
    L"st(0)",        // 128 CV_REG_ST0
    L"st(1)",        // 129 CV_REG_ST1
    L"st(2)",        // 130 CV_REG_ST2
    L"st(3)",        // 131 CV_REG_ST3
    L"st(4)",        // 132 CV_REG_ST4
    L"st(5)",        // 133 CV_REG_ST5
    L"st(6)",        // 134 CV_REG_ST6
    L"st(7)",        // 135 CV_REG_ST7
    L"CTRL",         // 136 CV_REG_CTRL
    L"STAT",         // 137 CV_REG_STAT
    L"TAG",          // 138 CV_REG_TAG
    L"FPIP",         // 139 CV_REG_FPIP
    L"FPCS",         // 140 CV_REG_FPCS
    L"FPDO",         // 141 CV_REG_FPDO
    L"FPDS",         // 142 CV_REG_FPDS
    L"ISEM",         // 143 CV_REG_ISEM
    L"FPEIP",        // 144 CV_REG_FPEIP
    L"FPED0"         // 145 CV_REG_FPEDO
};

const wchar_t * const rgRegAMD64[] = {
    L"None",         // 0   CV_REG_NONE
    L"al",           // 1   CV_AMD64_AL
    L"cl",           // 2   CV_AMD64_CL
    L"dl",           // 3   CV_AMD64_DL
    L"bl",           // 4   CV_AMD64_BL
    L"ah",           // 5   CV_AMD64_AH
    L"ch",           // 6   CV_AMD64_CH
    L"dh",           // 7   CV_AMD64_DH
    L"bh",           // 8   CV_AMD64_BH
    L"ax",           // 9   CV_AMD64_AX
    L"cx",           // 10  CV_AMD64_CX
    L"dx",           // 11  CV_AMD64_DX
    L"bx",           // 12  CV_AMD64_BX
    L"sp",           // 13  CV_AMD64_SP
    L"bp",           // 14  CV_AMD64_BP
    L"si",           // 15  CV_AMD64_SI
    L"di",           // 16  CV_AMD64_DI
    L"eax",          // 17  CV_AMD64_EAX
    L"ecx",          // 18  CV_AMD64_ECX
    L"edx",          // 19  CV_AMD64_EDX
    L"ebx",          // 20  CV_AMD64_EBX
    L"esp",          // 21  CV_AMD64_ESP
    L"ebp",          // 22  CV_AMD64_EBP
    L"esi",          // 23  CV_AMD64_ESI
    L"edi",          // 24  CV_AMD64_EDI
    L"es",           // 25  CV_AMD64_ES
    L"cs",           // 26  CV_AMD64_CS
    L"ss",           // 27  CV_AMD64_SS
    L"ds",           // 28  CV_AMD64_DS
    L"fs",           // 29  CV_AMD64_FS
    L"gs",           // 30  CV_AMD64_GS
    L"???",          // 31  Not filled up
    L"flags",        // 32  CV_AMD64_FLAGS
    L"rip",          // 33  CV_AMD64_RIP
    L"eflags",       // 34  CV_AMD64_EFLAGS
    L"???",          // 35
    L"???",          // 36
    L"???",          // 37
    L"???",          // 38
    L"???",          // 39
    L"???",          // 40
    L"???",          // 41
    L"???",          // 42
    L"???",          // 43
    L"???",          // 44
    L"???",          // 45
    L"???",          // 46
    L"???",          // 47
    L"???",          // 48
    L"???",          // 49
    L"???",          // 50
    L"???",          // 51
    L"???",          // 52
    L"???",          // 53
    L"???",          // 54
    L"???",          // 55
    L"???",          // 56
    L"???",          // 57
    L"???",          // 58
    L"???",          // 59
    L"???",          // 60
    L"???",          // 61
    L"???",          // 62
    L"???",          // 63
    L"???",          // 64
    L"???",          // 65
    L"???",          // 66
    L"???",          // 67
    L"???",          // 68
    L"???",          // 69
    L"???",          // 70
    L"???",          // 71
    L"???",          // 72
    L"???",          // 73
    L"???",          // 74
    L"???",          // 75
    L"???",          // 76
    L"???",          // 77
    L"???",          // 78
    L"???",          // 79
    L"cr0",          // 80  CV_AMD64_CR0
    L"cr1",          // 81  CV_AMD64_CR1
    L"cr2",          // 82  CV_AMD64_CR2
    L"cr3",          // 83  CV_AMD64_CR3
    L"cr4",          // 84  CV_AMD64_CR4
    L"???",          // 85
    L"???",          // 86
    L"???",          // 87
    L"cr8",          // 88  CV_AMD64_CR8
    L"???",          // 89
    L"dr0",          // 90  CV_AMD64_DR0
    L"dr1",          // 91  CV_AMD64_DR1
    L"dr2",          // 92  CV_AMD64_DR2
    L"dr3",          // 93  CV_AMD64_DR3
    L"dr4",          // 94  CV_AMD64_DR4
    L"dr5",          // 95  CV_AMD64_DR5
    L"dr6",          // 96  CV_AMD64_DR6
    L"dr7",          // 97  CV_AMD64_DR7
    L"dr8",          // 98  CV_AMD64_DR8
    L"dr9",          // 99  CV_AMD64_DR9
    L"dr10",         // 100 CV_AMD64_DR10
    L"dr11",         // 101 CV_AMD64_DR11
    L"dr12",         // 102 CV_AMD64_DR12
    L"dr13",         // 103 CV_AMD64_DR13
    L"dr14",         // 104 CV_AMD64_DR14
    L"dr15",         // 105 CV_AMD64_DR15
    L"???",          // 106
    L"???",          // 107
    L"???",          // 108
    L"???",          // 109
    L"gdtr",         // 110 CV_AMD64_GDTR
    L"gdt",         // 111 CV_AMD64_GDTL
    L"idtr",         // 112 CV_AMD64_IDTR
    L"idt",         // 113 CV_AMD64_IDTL
    L"ldtr",         // 114 CV_AMD64_LDTR
    L"tr",           // 115 CV_AMD64_TR
    L"???",          // 116
    L"???",          // 117
    L"???",          // 118
    L"???",          // 119
    L"???",          // 120
    L"???",          // 121
    L"???",          // 122
    L"???",          // 123
    L"???",          // 124
    L"???",          // 125
    L"???",          // 126
    L"???",          // 127
    L"st(0)",        // 128 CV_AMD64_ST0
    L"st(1)",        // 129 CV_AMD64_ST1
    L"st(2)",        // 130 CV_AMD64_ST2
    L"st(3)",        // 131 CV_AMD64_ST3
    L"st(4)",        // 132 CV_AMD64_ST4
    L"st(5)",        // 133 CV_AMD64_ST5
    L"st(6)",        // 134 CV_AMD64_ST6
    L"st(7)",        // 135 CV_AMD64_ST7
    L"ctr",         // 136 CV_AMD64_CTRL
    L"stat",         // 137 CV_AMD64_STAT
    L"tag",          // 138 CV_AMD64_TAG
    L"fpip",         // 139 CV_AMD64_FPIP
    L"fpcs",         // 140 CV_AMD64_FPCS
    L"fpdo",         // 141 CV_AMD64_FPDO
    L"fpds",         // 142 CV_AMD64_FPDS
    L"isem",         // 143 CV_AMD64_ISEM
    L"fpeip",        // 144 CV_AMD64_FPEIP
    L"fped0",        // 145 CV_AMD64_FPEDO
    L"mm0",          // 146 CV_AMD64_MM0
    L"mm1",          // 147 CV_AMD64_MM1
    L"mm2",          // 148 CV_AMD64_MM2
    L"mm3",          // 149 CV_AMD64_MM3
    L"mm4",          // 150 CV_AMD64_MM4
    L"mm5",          // 151 CV_AMD64_MM5
    L"mm6",          // 152 CV_AMD64_MM6
    L"mm7",          // 153 CV_AMD64_MM7
    L"xmm0",         // 154 CV_AMD64_XMM0
    L"xmm1",         // 155 CV_AMD64_XMM1
    L"xmm2",         // 156 CV_AMD64_XMM2
    L"xmm3",         // 157 CV_AMD64_XMM3
    L"xmm4",         // 158 CV_AMD64_XMM4
    L"xmm5",         // 159 CV_AMD64_XMM5
    L"xmm6",         // 160 CV_AMD64_XMM6
    L"xmm7",         // 161 CV_AMD64_XMM7
    L"xmm0_0",       // 162 CV_AMD64_XMM0_0
    L"xmm0_1",       // 163 CV_AMD64_XMM0_1
    L"xmm0_2",       // 164 CV_AMD64_XMM0_2
    L"xmm0_3",       // 165 CV_AMD64_XMM0_3
    L"xmm1_0",       // 166 CV_AMD64_XMM1_0
    L"xmm1_1",       // 167 CV_AMD64_XMM1_1
    L"xmm1_2",       // 168 CV_AMD64_XMM1_2
    L"xmm1_3",       // 169 CV_AMD64_XMM1_3
    L"xmm2_0",       // 170 CV_AMD64_XMM2_0
    L"xmm2_1",       // 171 CV_AMD64_XMM2_1
    L"xmm2_2",       // 172 CV_AMD64_XMM2_2
    L"xmm2_3",       // 173 CV_AMD64_XMM2_3
    L"xmm3_0",       // 174 CV_AMD64_XMM3_0
    L"xmm3_1",       // 175 CV_AMD64_XMM3_1
    L"xmm3_2",       // 176 CV_AMD64_XMM3_2
    L"xmm3_3",       // 177 CV_AMD64_XMM3_3
    L"xmm4_0",       // 178 CV_AMD64_XMM4_0
    L"xmm4_1",       // 179 CV_AMD64_XMM4_1
    L"xmm4_2",       // 180 CV_AMD64_XMM4_2
    L"xmm4_3",       // 181 CV_AMD64_XMM4_3
    L"xmm5_0",       // 182 CV_AMD64_XMM5_0
    L"xmm5_1",       // 183 CV_AMD64_XMM5_1
    L"xmm5_2",       // 184 CV_AMD64_XMM5_2
    L"xmm5_3",       // 185 CV_AMD64_XMM5_3
    L"xmm6_0",       // 186 CV_AMD64_XMM6_0
    L"xmm6_1",       // 187 CV_AMD64_XMM6_1
    L"xmm6_2",       // 188 CV_AMD64_XMM6_2
    L"xmm6_3",       // 189 CV_AMD64_XMM6_3
    L"xmm7_0",       // 190 CV_AMD64_XMM7_0
    L"xmm7_1",       // 191 CV_AMD64_XMM7_1
    L"xmm7_2",       // 192 CV_AMD64_XMM7_2
    L"xmm7_3",       // 193 CV_AMD64_XMM7_3
    L"xmm0",        // 194 CV_AMD64_XMM0L
    L"xmm1",        // 195 CV_AMD64_XMM1L
    L"xmm2",        // 196 CV_AMD64_XMM2L
    L"xmm3",        // 197 CV_AMD64_XMM3L
    L"xmm4",        // 198 CV_AMD64_XMM4L
    L"xmm5",        // 199 CV_AMD64_XMM5L
    L"xmm6",        // 200 CV_AMD64_XMM6L
    L"xmm7",        // 201 CV_AMD64_XMM7L
    L"xmm0h",        // 202 CV_AMD64_XMM0H
    L"xmm1h",        // 203 CV_AMD64_XMM1H
    L"xmm2h",        // 204 CV_AMD64_XMM2H
    L"xmm3h",        // 205 CV_AMD64_XMM3H
    L"xmm4h",        // 206 CV_AMD64_XMM4H
    L"xmm5h",        // 207 CV_AMD64_XMM5H
    L"xmm6h",        // 208 CV_AMD64_XMM6H
    L"xmm7h",        // 209 CV_AMD64_XMM7H
    L"???",          // 210
    L"mxcsr",        // 211 CV_AMD64_MXCSR
    L"???",          // 212
    L"???",          // 213
    L"???",          // 214
    L"???",          // 215
    L"???",          // 216
    L"???",          // 217
    L"???",          // 218
    L"???",          // 219
    L"emm0",        // 220 CV_AMD64_EMM0L
    L"emm1",        // 221 CV_AMD64_EMM1L
    L"emm2",        // 222 CV_AMD64_EMM2L
    L"emm3",        // 223 CV_AMD64_EMM3L
    L"emm4",        // 224 CV_AMD64_EMM4L
    L"emm5",        // 225 CV_AMD64_EMM5L
    L"emm6",        // 226 CV_AMD64_EMM6L
    L"emm7",        // 227 CV_AMD64_EMM7L
    L"emm0h",        // 228 CV_AMD64_EMM0H
    L"emm1h",        // 229 CV_AMD64_EMM1H
    L"emm2h",        // 230 CV_AMD64_EMM2H
    L"emm3h",        // 231 CV_AMD64_EMM3H
    L"emm4h",        // 232 CV_AMD64_EMM4H
    L"emm5h",        // 233 CV_AMD64_EMM5H
    L"emm6h",        // 234 CV_AMD64_EMM6H
    L"emm7h",        // 235 CV_AMD64_EMM7H
    L"mm00",         // 236 CV_AMD64_MM00
    L"mm01",         // 237 CV_AMD64_MM01
    L"mm10",         // 238 CV_AMD64_MM10
    L"mm11",         // 239 CV_AMD64_MM11
    L"mm20",         // 240 CV_AMD64_MM20
    L"mm21",         // 241 CV_AMD64_MM21
    L"mm30",         // 242 CV_AMD64_MM30
    L"mm31",         // 243 CV_AMD64_MM31
    L"mm40",         // 244 CV_AMD64_MM40
    L"mm41",         // 245 CV_AMD64_MM41
    L"mm50",         // 246 CV_AMD64_MM50
    L"mm51",         // 247 CV_AMD64_MM51
    L"mm60",         // 248 CV_AMD64_MM60
    L"mm61",         // 249 CV_AMD64_MM61
    L"mm70",         // 250 CV_AMD64_MM70
    L"mm71",         // 251 CV_AMD64_MM71
    L"xmm8",         // 252 CV_AMD64_XMM8
    L"xmm9",         // 253 CV_AMD64_XMM9
    L"xmm10",        // 254 CV_AMD64_XMM10
    L"xmm11",        // 255 CV_AMD64_XMM11
    L"xmm12",        // 256 CV_AMD64_XMM12
    L"xmm13",        // 257 CV_AMD64_XMM13
    L"xmm14",        // 258 CV_AMD64_XMM14
    L"xmm15",        // 259 CV_AMD64_XMM15
    L"xmm8_0",       // 260 CV_AMD64_XMM8_0
    L"xmm8_1",       // 261 CV_AMD64_XMM8_1
    L"xmm8_2",       // 262 CV_AMD64_XMM8_2
    L"xmm8_3",       // 263 CV_AMD64_XMM8_3
    L"xmm9_0",       // 264 CV_AMD64_XMM9_0
    L"xmm9_1",       // 265 CV_AMD64_XMM9_1
    L"xmm9_2",       // 266 CV_AMD64_XMM9_2
    L"xmm9_3",       // 267 CV_AMD64_XMM9_3
    L"xmm10_0",      // 268 CV_AMD64_XMM10_0
    L"xmm10_1",      // 269 CV_AMD64_XMM10_1
    L"xmm10_2",      // 270 CV_AMD64_XMM10_2
    L"xmm10_3",      // 271 CV_AMD64_XMM10_3
    L"xmm11_0",      // 272 CV_AMD64_XMM11_0
    L"xmm11_1",      // 273 CV_AMD64_XMM11_1
    L"xmm11_2",      // 274 CV_AMD64_XMM11_2
    L"xmm11_3",      // 275 CV_AMD64_XMM11_3
    L"xmm12_0",      // 276 CV_AMD64_XMM12_0
    L"xmm12_1",      // 277 CV_AMD64_XMM12_1
    L"xmm12_2",      // 278 CV_AMD64_XMM12_2
    L"xmm12_3",      // 279 CV_AMD64_XMM12_3
    L"xmm13_0",      // 280 CV_AMD64_XMM13_0
    L"xmm13_1",      // 281 CV_AMD64_XMM13_1
    L"xmm13_2",      // 282 CV_AMD64_XMM13_2
    L"xmm13_3",      // 283 CV_AMD64_XMM13_3
    L"xmm14_0",      // 284 CV_AMD64_XMM14_0
    L"xmm14_1",      // 285 CV_AMD64_XMM14_1
    L"xmm14_2",      // 286 CV_AMD64_XMM14_2
    L"xmm14_3",      // 287 CV_AMD64_XMM14_3
    L"xmm15_0",      // 288 CV_AMD64_XMM15_0
    L"xmm15_1",      // 289 CV_AMD64_XMM15_1
    L"xmm15_2",      // 290 CV_AMD64_XMM15_2
    L"xmm15_3",      // 291 CV_AMD64_XMM15_3
    L"xmm8",        // 292 CV_AMD64_XMM8L
    L"xmm9",        // 293 CV_AMD64_XMM9L
    L"xmm10",       // 294 CV_AMD64_XMM10L
    L"xmm11",       // 295 CV_AMD64_XMM11L
    L"xmm12",       // 296 CV_AMD64_XMM12L
    L"xmm13",       // 297 CV_AMD64_XMM13L
    L"xmm14",       // 298 CV_AMD64_XMM14L
    L"xmm15",       // 299 CV_AMD64_XMM15L
    L"xmm8h",        // 300 CV_AMD64_XMM8H
    L"xmm9h",        // 301 CV_AMD64_XMM9H
    L"xmm10h",       // 302 CV_AMD64_XMM10H
    L"xmm11h",       // 303 CV_AMD64_XMM11H
    L"xmm12h",       // 304 CV_AMD64_XMM12H
    L"xmm13h",       // 305 CV_AMD64_XMM13H
    L"xmm14h",       // 306 CV_AMD64_XMM14H
    L"xmm15h",       // 307 CV_AMD64_XMM15H
    L"emm8",        // 308 CV_AMD64_EMM8L
    L"emm9",        // 309 CV_AMD64_EMM9L
    L"emm10",       // 310 CV_AMD64_EMM10L
    L"emm11",       // 311 CV_AMD64_EMM11L
    L"emm12",       // 312 CV_AMD64_EMM12L
    L"emm13",       // 313 CV_AMD64_EMM13L
    L"emm14",       // 314 CV_AMD64_EMM14L
    L"emm15",       // 315 CV_AMD64_EMM15L
    L"emm8h",        // 316 CV_AMD64_EMM8H
    L"emm9h",        // 317 CV_AMD64_EMM9H
    L"emm10h",       // 318 CV_AMD64_EMM10H
    L"emm11h",       // 319 CV_AMD64_EMM11H
    L"emm12h",       // 320 CV_AMD64_EMM12H
    L"emm13h",       // 321 CV_AMD64_EMM13H
    L"emm14h",       // 322 CV_AMD64_EMM14H
    L"emm15h",       // 323 CV_AMD64_EMM15H
    L"si",          // 324 CV_AMD64_SIL
    L"di",          // 325 CV_AMD64_DIL
    L"bp",          // 326 CV_AMD64_BPL
    L"sp",          // 327 CV_AMD64_SPL
    L"rax",          // 328 CV_AMD64_RAX
    L"rbx",          // 329 CV_AMD64_RBX
    L"rcx",          // 330 CV_AMD64_RCX
    L"rdx",          // 331 CV_AMD64_RDX
    L"rsi",          // 332 CV_AMD64_RSI
    L"rdi",          // 333 CV_AMD64_RDI
    L"rbp",          // 334 CV_AMD64_RBP
    L"rsp",          // 335 CV_AMD64_RSP
    L"r8",           // 336 CV_AMD64_R8
    L"r9",           // 337 CV_AMD64_R9
    L"r10",          // 338 CV_AMD64_R10
    L"r11",          // 339 CV_AMD64_R11
    L"r12",          // 340 CV_AMD64_R12
    L"r13",          // 341 CV_AMD64_R13
    L"r14",          // 342 CV_AMD64_R14
    L"r15",          // 343 CV_AMD64_R15
    L"r8b",          // 344 CV_AMD64_R8B
    L"r9b",          // 345 CV_AMD64_R9B
    L"r10b",         // 346 CV_AMD64_R10B
    L"r11b",         // 347 CV_AMD64_R11B
    L"r12b",         // 348 CV_AMD64_R12B
    L"r13b",         // 349 CV_AMD64_R13B
    L"r14b",         // 350 CV_AMD64_R14B
    L"r15b",         // 351 CV_AMD64_R15B
    L"r8w",          // 352 CV_AMD64_R8W
    L"r9w",          // 353 CV_AMD64_R9W
    L"r10w",         // 354 CV_AMD64_R10W
    L"r11w",         // 355 CV_AMD64_R11W
    L"r12w",         // 356 CV_AMD64_R12W
    L"r13w",         // 357 CV_AMD64_R13W
    L"r14w",         // 358 CV_AMD64_R14W
    L"r15w",         // 359 CV_AMD64_R15W
    L"r8d",          // 360 CV_AMD64_R8D
    L"r9d",          // 361 CV_AMD64_R9D
    L"r10d",         // 362 CV_AMD64_R10D
    L"r11d",         // 363 CV_AMD64_R11D
    L"r12d",         // 364 CV_AMD64_R12D
    L"r13d",         // 365 CV_AMD64_R13D
    L"r14d",         // 366 CV_AMD64_R14D
    L"r15d"          // 367 CV_AMD64_R15D
};

const wchar_t * const rgRegMips[] = {
    L"None",         // 0   CV_M4_NOREG
    L"???",          // 1
    L"???",          // 2
    L"???",          // 3
    L"???",          // 4
    L"???",          // 5
    L"???",          // 6
    L"???",          // 7
    L"???",          // 8
    L"???",          // 9
    L"zero",         // 10  CV_M4_IntZERO
    L"at",           // 11  CV_M4_IntAT
    L"v0",           // 12  CV_M4_IntV0
    L"v1",           // 13  CV_M4_IntV1
    L"a0",           // 14  CV_M4_IntA0
    L"a1",           // 15  CV_M4_IntA1
    L"a2",           // 16  CV_M4_IntA2
    L"a3",           // 17  CV_M4_IntA3
    L"t0",           // 18  CV_M4_IntT0
    L"t1",           // 19  CV_M4_IntT1
    L"t2",           // 20  CV_M4_IntT2
    L"t3",           // 21  CV_M4_IntT3
    L"t4",           // 22  CV_M4_IntT4
    L"t5",           // 23  CV_M4_IntT5
    L"t6",           // 24  CV_M4_IntT6
    L"t7",           // 25  CV_M4_IntT7
    L"s0",           // 26  CV_M4_IntS0
    L"s1",           // 27  CV_M4_IntS1
    L"s2",           // 28  CV_M4_IntS2
    L"s3",           // 29  CV_M4_IntS3
    L"s4",           // 30  CV_M4_IntS4
    L"s5",           // 31  CV_M4_IntS5
    L"s6",           // 32  CV_M4_IntS6
    L"s7",           // 33  CV_M4_IntS7
    L"t8",           // 34  CV_M4_IntT8
    L"t9",           // 35  CV_M4_IntT9
    L"k0",           // 36  CV_M4_IntKT0
    L"k1",           // 37  CV_M4_IntKT1
    L"gp",           // 38  CV_M4_IntGP
    L"sp",           // 39  CV_M4_IntSP
    L"s8",           // 40  CV_M4_IntS8
    L"ra",           // 41  CV_M4_IntRA
    L"lo",           // 42  CV_M4_IntLO
    L"hi",           // 43  CV_M4_IntHI
    L"???",          // 44
    L"???",          // 45
    L"???",          // 46
    L"???",          // 47
    L"???",          // 48
    L"???",          // 49
    L"Fir",          // 50  CV_M4_Fir
    L"Psr",          // 51  CV_M4_Psr
    L"???",          // 52
    L"???",          // 53
    L"???",          // 54
    L"???",          // 55
    L"???",          // 56
    L"???",          // 57
    L"???",          // 58
    L"???",          // 59
    L"$f0",          // 60  CV_M4_FltF0
    L"$f1",          // 61  CV_M4_FltF1
    L"$f2",          // 62  CV_M4_FltF2
    L"$f3",          // 63  CV_M4_FltF3
    L"$f4",          // 64  CV_M4_FltF4
    L"$f5",          // 65  CV_M4_FltF5
    L"$f6",          // 66  CV_M4_FltF6
    L"$f7",          // 67  CV_M4_FltF7
    L"$f8",          // 68  CV_M4_FltF8
    L"$f9",          // 69  CV_M4_FltF9
    L"$f10",         // 70  CV_M4_FltF10
    L"$f11",         // 71  CV_M4_FltF11
    L"$f12",         // 72  CV_M4_FltF12
    L"$f13",         // 73  CV_M4_FltF13
    L"$f14",         // 74  CV_M4_FltF14
    L"$f15",         // 75  CV_M4_FltF15
    L"$f16",         // 76  CV_M4_FltF16
    L"$f17",         // 77  CV_M4_FltF17
    L"$f18",         // 78  CV_M4_FltF18
    L"$f19",         // 79  CV_M4_FltF19
    L"$f20",         // 80  CV_M4_FltF20
    L"$f21",         // 81  CV_M4_FltF21
    L"$f22",         // 82  CV_M4_FltF22
    L"$f23",         // 83  CV_M4_FltF23
    L"$f24",         // 84  CV_M4_FltF24
    L"$f25",         // 85  CV_M4_FltF25
    L"$f26",         // 86  CV_M4_FltF26
    L"$f27",         // 87  CV_M4_FltF27
    L"$f28",         // 88  CV_M4_FltF28
    L"$f29",         // 89  CV_M4_FltF29
    L"$f30",         // 90  CV_M4_FltF30
    L"$f31",         // 91  CV_M4_FltF31
    L"Fsr"           // 92  CV_M4_FltFsr
};

const wchar_t * const rgReg68k[] = {
    L"D0",           // 0   CV_R68_D0
    L"D1",           // 1   CV_R68_D1
    L"D2",           // 2   CV_R68_D2
    L"D3",           // 3   CV_R68_D3
    L"D4",           // 4   CV_R68_D4
    L"D5",           // 5   CV_R68_D5
    L"D6",           // 6   CV_R68_D6
    L"D7",           // 7   CV_R68_D7
    L"A0",           // 8   CV_R68_A0
    L"A1",           // 9   CV_R68_A1
    L"A2",           // 10  CV_R68_A2
    L"A3",           // 11  CV_R68_A3
    L"A4",           // 12  CV_R68_A4
    L"A5",           // 13  CV_R68_A5
    L"A6",           // 14  CV_R68_A6
    L"A7",           // 15  CV_R68_A7
    L"CCR",          // 16  CV_R68_CCR
    L"SR",           // 17  CV_R68_SR
    L"USP",          // 18  CV_R68_USP
    L"MSP",          // 19  CV_R68_MSP
    L"SFC",          // 20  CV_R68_SFC
    L"DFC",          // 21  CV_R68_DFC
    L"CACR",         // 22  CV_R68_CACR
    L"VBR",          // 23  CV_R68_VBR
    L"CAAR",         // 24  CV_R68_CAAR
    L"ISP",          // 25  CV_R68_ISP
    L"PC",           // 26  CV_R68_PC
    L"???",          // 27
    L"FPCR",         // 28  CV_R68_FPCR
    L"FPSR",         // 29  CV_R68_FPSR
    L"FPIAR",        // 30  CV_R68_FPIAR
    L"???",          // 31
    L"FP0",          // 32  CV_R68_FP0
    L"FP1",          // 33  CV_R68_FP1
    L"FP2",          // 34  CV_R68_FP2
    L"FP3",          // 35  CV_R68_FP3
    L"FP4",          // 36  CV_R68_FP4
    L"FP5",          // 37  CV_R68_FP5
    L"FP6",          // 38  CV_R68_FP6
    L"FP7",          // 39  CV_R68_FP7
    L"???",          // 40
    L"???",          // 41  CV_R68_MMUSR030
    L"???",          // 42  CV_R68_MMUSR
    L"???",          // 43  CV_R68_URP
    L"???",          // 44  CV_R68_DTT0
    L"???",          // 45  CV_R68_DTT1
    L"???",          // 46  CV_R68_ITT0
    L"???",          // 47  CV_R68_ITT1
    L"???",          // 48
    L"???",          // 49
    L"???",          // 50
    L"PSR",          // 51  CV_R68_PSR
    L"PCSR",         // 52  CV_R68_PCSR
    L"VAL",          // 53  CV_R68_VAL
    L"CRP",          // 54  CV_R68_CRP
    L"SRP",          // 55  CV_R68_SRP
    L"DRP",          // 56  CV_R68_DRP
    L"TC",           // 57  CV_R68_TC
    L"AC",           // 58  CV_R68_AC
    L"SCC",          // 59  CV_R68_SCC
    L"CAL",          // 60  CV_R68_CAL
    L"TT0",          // 61  CV_R68_TT0
    L"TT1",          // 62  CV_R68_TT1
    L"???",          // 63
    L"BAD0",         // 64  CV_R68_BAD0
    L"BAD1",         // 65  CV_R68_BAD1
    L"BAD2",         // 66  CV_R68_BAD2
    L"BAD3",         // 67  CV_R68_BAD3
    L"BAD4",         // 68  CV_R68_BAD4
    L"BAD5",         // 69  CV_R68_BAD5
    L"BAD6",         // 70  CV_R68_BAD6
    L"BAD7",         // 71  CV_R68_BAD7
    L"BAC0",         // 72  CV_R68_BAC0
    L"BAC1",         // 73  CV_R68_BAC1
    L"BAC2",         // 74  CV_R68_BAC2
    L"BAC3",         // 75  CV_R68_BAC3
    L"BAC4",         // 76  CV_R68_BAC4
    L"BAC5",         // 77  CV_R68_BAC5
    L"BAC6",         // 78  CV_R68_BAC6
    L"BAC7"          // 79  CV_R68_BAC7
};

const wchar_t * const rgRegAlpha[] = {
    L"None",         // 0   CV_ALPHA_NOREG
    L"???",          // 1
    L"???",          // 2
    L"???",          // 3
    L"???",          // 4
    L"???",          // 5
    L"???",          // 6
    L"???",          // 7
    L"???",          // 8
    L"???",          // 9
    L"$f0",          // 10  CV_ALPHA_FltF0
    L"$f1",          // 11  CV_ALPHA_FltF1
    L"$f2",          // 12  CV_ALPHA_FltF2
    L"$f3",          // 13  CV_ALPHA_FltF3
    L"$f4",          // 14  CV_ALPHA_FltF4
    L"$f5",          // 15  CV_ALPHA_FltF5
    L"$f6",          // 16  CV_ALPHA_FltF6
    L"$f7",          // 17  CV_ALPHA_FltF7
    L"$f8",          // 18  CV_ALPHA_FltF8
    L"$f9",          // 19  CV_ALPHA_FltF9
    L"$f10",         // 20  CV_ALPHA_FltF10
    L"$f11",         // 21  CV_ALPHA_FltF11
    L"$f12",         // 22  CV_ALPHA_FltF12
    L"$f13",         // 23  CV_ALPHA_FltF13
    L"$f14",         // 24  CV_ALPHA_FltF14
    L"$f15",         // 25  CV_ALPHA_FltF15
    L"$f16",         // 26  CV_ALPHA_FltF16
    L"$f17",         // 27  CV_ALPHA_FltF17
    L"$f18",         // 28  CV_ALPHA_FltF18
    L"$f19",         // 29  CV_ALPHA_FltF19
    L"$f20",         // 30  CV_ALPHA_FltF20
    L"$f21",         // 31  CV_ALPHA_FltF21
    L"$f22",         // 32  CV_ALPHA_FltF22
    L"$f23",         // 33  CV_ALPHA_FltF23
    L"$f24",         // 34  CV_ALPHA_FltF24
    L"$f25",         // 35  CV_ALPHA_FltF25
    L"$f26",         // 36  CV_ALPHA_FltF26
    L"$f27",         // 37  CV_ALPHA_FltF27
    L"$f28",         // 38  CV_ALPHA_FltF28
    L"$f29",         // 39  CV_ALPHA_FltF29
    L"$f30",         // 40  CV_ALPHA_FltF30
    L"$f31",         // 41  CV_ALPHA_FltF31
    L"v0",           // 42  CV_ALPHA_IntV0
    L"t0",           // 43  CV_ALPHA_IntT0
    L"t1",           // 44  CV_ALPHA_IntT1
    L"t2",           // 45  CV_ALPHA_IntT2
    L"t3",           // 46  CV_ALPHA_IntT3
    L"t4",           // 47  CV_ALPHA_IntT4
    L"t5",           // 48  CV_ALPHA_IntT5
    L"t6",           // 49  CV_ALPHA_IntT6
    L"t7",           // 50  CV_ALPHA_IntT7
    L"s0",           // 51  CV_ALPHA_IntS0
    L"s1",           // 52  CV_ALPHA_IntS1
    L"s2",           // 53  CV_ALPHA_IntS2
    L"s3",           // 54  CV_ALPHA_IntS3
    L"s4",           // 55  CV_ALPHA_IntS4
    L"s5",           // 56  CV_ALPHA_IntS5
    L"fp",           // 57  CV_ALPHA_IntFP
    L"a0",           // 58  CV_ALPHA_IntA0
    L"a1",           // 59  CV_ALPHA_IntA1
    L"a2",           // 60  CV_ALPHA_IntA2
    L"a3",           // 61  CV_ALPHA_IntA3
    L"a4",           // 62  CV_ALPHA_IntA4
    L"a5",           // 63  CV_ALPHA_IntA5
    L"t8",           // 64  CV_ALPHA_IntT8
    L"t9",           // 65  CV_ALPHA_IntT9
    L"t10",          // 66  CV_ALPHA_IntT10
    L"t11",          // 67  CV_ALPHA_IntT11
    L"ra",           // 68  CV_ALPHA_IntRA
    L"t12",          // 69  CV_ALPHA_IntT12
    L"at",           // 70  CV_ALPHA_IntAT
    L"gp",           // 71  CV_ALPHA_IntGP
    L"sp",           // 72  CV_ALPHA_IntSP
    L"zero",         // 73  CV_ALPHA_IntZERO
    L"Fpcr",         // 74  CV_ALPHA_Fpcr
    L"Fir",          // 75  CV_ALPHA_Fir
    L"Psr",          // 76  CV_ALPHA_Psr
    L"FltFsr"        // 77  CV_ALPHA_FltFsr
};

const wchar_t * const rgRegPpc[] = {
    L"None",         // 0
    L"r0",           // 1   CV_PPC_GPR0
    L"r1",           // 2   CV_PPC_GPR1
    L"r2",           // 3   CV_PPC_GPR2
    L"r3",           // 4   CV_PPC_GPR3
    L"r4",           // 5   CV_PPC_GPR4
    L"r5",           // 6   CV_PPC_GPR5
    L"r6",           // 7   CV_PPC_GPR6
    L"r7",           // 8   CV_PPC_GPR7
    L"r8",           // 9   CV_PPC_GPR8
    L"r9",           // 10  CV_PPC_GPR9
    L"r10",          // 11  CV_PPC_GPR10
    L"r11",          // 12  CV_PPC_GPR11
    L"r12",          // 13  CV_PPC_GPR12
    L"r13",          // 14  CV_PPC_GPR13
    L"r14",          // 15  CV_PPC_GPR14
    L"r15",          // 16  CV_PPC_GPR15
    L"r16",          // 17  CV_PPC_GPR16
    L"r17",          // 18  CV_PPC_GPR17
    L"r18",          // 19  CV_PPC_GPR18
    L"r19",          // 20  CV_PPC_GPR19
    L"r20",          // 21  CV_PPC_GPR20
    L"r21",          // 22  CV_PPC_GPR21
    L"r22",          // 23  CV_PPC_GPR22
    L"r23",          // 24  CV_PPC_GPR23
    L"r24",          // 25  CV_PPC_GPR24
    L"r25",          // 26  CV_PPC_GPR25
    L"r26",          // 27  CV_PPC_GPR26
    L"r27",          // 28  CV_PPC_GPR27
    L"r28",          // 29  CV_PPC_GPR28
    L"r29",          // 30  CV_PPC_GPR29
    L"r30",          // 31  CV_PPC_GPR30
    L"r31",          // 32  CV_PPC_GPR31
    L"cr",           // 33  CV_PPC_CR
    L"cr0",          // 34  CV_PPC_CR0
    L"cr1",          // 35  CV_PPC_CR1
    L"cr2",          // 36  CV_PPC_CR2
    L"cr3",          // 37  CV_PPC_CR3
    L"cr4",          // 38  CV_PPC_CR4
    L"cr5",          // 39  CV_PPC_CR5
    L"cr6",          // 40  CV_PPC_CR6
    L"cr7",          // 41  CV_PPC_CR7
    L"f0",           // 42  CV_PPC_FPR0
    L"f1",           // 43  CV_PPC_FPR1
    L"f2",           // 44  CV_PPC_FPR2
    L"f3",           // 45  CV_PPC_FPR3
    L"f4",           // 46  CV_PPC_FPR4
    L"f5",           // 47  CV_PPC_FPR5
    L"f6",           // 48  CV_PPC_FPR6
    L"f7",           // 49  CV_PPC_FPR7
    L"f8",           // 50  CV_PPC_FPR8
    L"f9",           // 51  CV_PPC_FPR9
    L"f10",          // 52  CV_PPC_FPR10
    L"f11",          // 53  CV_PPC_FPR11
    L"f12",          // 54  CV_PPC_FPR12
    L"f13",          // 55  CV_PPC_FPR13
    L"f14",          // 56  CV_PPC_FPR14
    L"f15",          // 57  CV_PPC_FPR15
    L"f16",          // 58  CV_PPC_FPR16
    L"f17",          // 59  CV_PPC_FPR17
    L"f18",          // 60  CV_PPC_FPR18
    L"f19",          // 61  CV_PPC_FPR19
    L"f20",          // 62  CV_PPC_FPR20
    L"f21",          // 63  CV_PPC_FPR21
    L"f22",          // 64  CV_PPC_FPR22
    L"f23",          // 65  CV_PPC_FPR23
    L"f24",          // 66  CV_PPC_FPR24
    L"f25",          // 67  CV_PPC_FPR25
    L"f26",          // 68  CV_PPC_FPR26
    L"f27",          // 69  CV_PPC_FPR27
    L"f28",          // 70  CV_PPC_FPR28
    L"f29",          // 71  CV_PPC_FPR29
    L"f30",          // 72  CV_PPC_FPR30
    L"f31",          // 73  CV_PPC_FPR31
    L"Fpscr",        // 74  CV_PPC_FPSCR
    L"Msr"           // 75  CV_PPC_MSR
};

const wchar_t * const rgRegSh[] = {
    L"None",         // 0   CV_SH3_NOREG
    L"???",          // 1
    L"???",          // 2
    L"???",          // 3
    L"???",          // 4
    L"???",          // 5
    L"???",          // 6
    L"???",          // 7
    L"???",          // 8
    L"???",          // 9
    L"r0",           // 10  CV_SH3_IntR0
    L"r1",           // 11  CV_SH3_IntR1
    L"r2",           // 12  CV_SH3_IntR2
    L"r3",           // 13  CV_SH3_IntR3
    L"r4",           // 14  CV_SH3_IntR4
    L"r5",           // 15  CV_SH3_IntR5
    L"r6",           // 16  CV_SH3_IntR6
    L"r7",           // 17  CV_SH3_IntR7
    L"r8",           // 18  CV_SH3_IntR8
    L"r9",           // 19  CV_SH3_IntR9
    L"r10",          // 20  CV_SH3_IntR10
    L"r11",          // 21  CV_SH3_IntR11
    L"r12",          // 22  CV_SH3_IntR12
    L"r13",          // 23  CV_SH3_IntR13
    L"fp",           // 24  CV_SH3_IntFp
    L"sp",           // 25  CV_SH3_IntSp
    L"???",          // 26
    L"???",          // 27
    L"???",          // 28
    L"???",          // 29
    L"???",          // 30
    L"???",          // 31
    L"???",          // 32
    L"???",          // 33
    L"???",          // 34
    L"???",          // 35
    L"???",          // 36
    L"???",          // 37
    L"gbr",          // 38  CV_SH3_Gbr
    L"pr",           // 39  CV_SH3_Pr
    L"mach",         // 40  CV_SH3_Mach
    L"macl",         // 41  CV_SH3_Macl
    L"???",          // 42
    L"???",          // 43
    L"???",          // 44
    L"???",          // 45
    L"???",          // 46
    L"???",          // 47
    L"???",          // 48
    L"???",          // 49
    L"pc",           // 50
    L"sr",           // 51
    L"???",          // 52
    L"???",          // 53
    L"???",          // 54
    L"???",          // 55
    L"???",          // 56
    L"???",          // 57
    L"???",          // 58
    L"???",          // 59
    L"bara",         // 60  CV_SH3_BarA
    L"basra",        // 61  CV_SH3_BasrA
    L"bamra",        // 62  CV_SH3_BamrA
    L"bbra",         // 63  CV_SH3_BbrA
    L"barb",         // 64  CV_SH3_BarB
    L"basrb",        // 65  CV_SH3_BasrB
    L"bamrb",        // 66  CV_SH3_BamrB
    L"bbrb",         // 67  CV_SH3_BbrB
    L"bdrb",         // 68  CV_SH3_BdrB
    L"bdmrb",        // 69  CV_SH3_BdmrB
    L"brcr"          // 70  CV_SH3_Brcr
};

const wchar_t * const rgRegArm[] = {
    L"None",         // 0   CV_ARM_NOREG
    L"???",          // 1
    L"???",          // 2
    L"???",          // 3
    L"???",          // 4
    L"???",          // 5
    L"???",          // 6
    L"???",          // 7
    L"???",          // 8
    L"???",          // 9
    L"r0",           // 10  CV_ARM_R0
    L"r1",           // 11  CV_ARM_R1
    L"r2",           // 12  CV_ARM_R2
    L"r3",           // 13  CV_ARM_R3
    L"r4",           // 14  CV_ARM_R4
    L"r5",           // 15  CV_ARM_R5
    L"r6",           // 16  CV_ARM_R6
    L"r7",           // 17  CV_ARM_R7
    L"r8",           // 18  CV_ARM_R8
    L"r9",           // 19  CV_ARM_R9
    L"r10",          // 20  CV_ARM_R10
    L"r11",          // 21  CV_ARM_R11
    L"r12",          // 22  CV_ARM_R12
    L"sp",           // 23  CV_ARM_SP
    L"lr",           // 24  CV_ARM_LR
    L"pc",           // 25  CV_ARM_PC
    L"cpsr"          // 26  CV_ARM_CPSR
};

const MapIa64Reg mpIa64regSz[] = {
    { CV_IA64_Br0, L"Br0" },
    { CV_IA64_Br1, L"Br1" },
    { CV_IA64_Br2, L"Br2" },
    { CV_IA64_Br3, L"Br3" },
    { CV_IA64_Br4, L"Br4" },
    { CV_IA64_Br5, L"Br5" },
    { CV_IA64_Br6, L"Br6" },
    { CV_IA64_Br7, L"Br7" },
    { CV_IA64_Preds, L"Preds" },
    { CV_IA64_IntH0, L"IntH0" },
    { CV_IA64_IntH1, L"IntH1" },
    { CV_IA64_IntH2, L"IntH2" },
    { CV_IA64_IntH3, L"IntH3" },
    { CV_IA64_IntH4, L"IntH4" },
    { CV_IA64_IntH5, L"IntH5" },
    { CV_IA64_IntH6, L"IntH6" },
    { CV_IA64_IntH7, L"IntH7" },
    { CV_IA64_IntH8, L"IntH8" },
    { CV_IA64_IntH9, L"IntH9" },
    { CV_IA64_IntH10, L"IntH10" },
    { CV_IA64_IntH11, L"IntH11" },
    { CV_IA64_IntH12, L"IntH12" },
    { CV_IA64_IntH13, L"IntH13" },
    { CV_IA64_IntH14, L"IntH14" },
    { CV_IA64_IntH15, L"IntH15" },
    { CV_IA64_Ip, L"Ip" },
    { CV_IA64_Umask, L"Umask" },
    { CV_IA64_Cfm, L"Cfm" },
    { CV_IA64_Psr, L"Psr" },
    { CV_IA64_Nats, L"Nats" },
    { CV_IA64_Nats2, L"Nats2" },
    { CV_IA64_Nats3, L"Nats3" },
    { CV_IA64_IntR0, L"IntR0" },
    { CV_IA64_IntR1, L"IntR1" },
    { CV_IA64_IntR2, L"IntR2" },
    { CV_IA64_IntR3, L"IntR3" },
    { CV_IA64_IntR4, L"IntR4" },
    { CV_IA64_IntR5, L"IntR5" },
    { CV_IA64_IntR6, L"IntR6" },
    { CV_IA64_IntR7, L"IntR7" },
    { CV_IA64_IntR8, L"IntR8" },
    { CV_IA64_IntR9, L"IntR9" },
    { CV_IA64_IntR10, L"IntR10" },
    { CV_IA64_IntR11, L"IntR11" },
    { CV_IA64_IntR12, L"IntR12" },
    { CV_IA64_IntR13, L"IntR13" },
    { CV_IA64_IntR14, L"IntR14" },
    { CV_IA64_IntR15, L"IntR15" },
    { CV_IA64_IntR16, L"IntR16" },
    { CV_IA64_IntR17, L"IntR17" },
    { CV_IA64_IntR18, L"IntR18" },
    { CV_IA64_IntR19, L"IntR19" },
    { CV_IA64_IntR20, L"IntR20" },
    { CV_IA64_IntR21, L"IntR21" },
    { CV_IA64_IntR22, L"IntR22" },
    { CV_IA64_IntR23, L"IntR23" },
    { CV_IA64_IntR24, L"IntR24" },
    { CV_IA64_IntR25, L"IntR25" },
    { CV_IA64_IntR26, L"IntR26" },
    { CV_IA64_IntR27, L"IntR27" },
    { CV_IA64_IntR28, L"IntR28" },
    { CV_IA64_IntR29, L"IntR29" },
    { CV_IA64_IntR30, L"IntR30" },
    { CV_IA64_IntR31, L"IntR31" },
    { CV_IA64_IntR32, L"IntR32" },
    { CV_IA64_IntR33, L"IntR33" },
    { CV_IA64_IntR34, L"IntR34" },
    { CV_IA64_IntR35, L"IntR35" },
    { CV_IA64_IntR36, L"IntR36" },
    { CV_IA64_IntR37, L"IntR37" },
    { CV_IA64_IntR38, L"IntR38" },
    { CV_IA64_IntR39, L"IntR39" },
    { CV_IA64_IntR40, L"IntR40" },
    { CV_IA64_IntR41, L"IntR41" },
    { CV_IA64_IntR42, L"IntR42" },
    { CV_IA64_IntR43, L"IntR43" },
    { CV_IA64_IntR44, L"IntR44" },
    { CV_IA64_IntR45, L"IntR45" },
    { CV_IA64_IntR46, L"IntR46" },
    { CV_IA64_IntR47, L"IntR47" },
    { CV_IA64_IntR48, L"IntR48" },
    { CV_IA64_IntR49, L"IntR49" },
    { CV_IA64_IntR50, L"IntR50" },
    { CV_IA64_IntR51, L"IntR51" },
    { CV_IA64_IntR52, L"IntR52" },
    { CV_IA64_IntR53, L"IntR53" },
    { CV_IA64_IntR54, L"IntR54" },
    { CV_IA64_IntR55, L"IntR55" },
    { CV_IA64_IntR56, L"IntR56" },
    { CV_IA64_IntR57, L"IntR57" },
    { CV_IA64_IntR58, L"IntR58" },
    { CV_IA64_IntR59, L"IntR59" },
    { CV_IA64_IntR60, L"IntR60" },
    { CV_IA64_IntR61, L"IntR61" },
    { CV_IA64_IntR62, L"IntR62" },
    { CV_IA64_IntR63, L"IntR63" },
    { CV_IA64_IntR64, L"IntR64" },
    { CV_IA64_IntR65, L"IntR65" },
    { CV_IA64_IntR66, L"IntR66" },
    { CV_IA64_IntR67, L"IntR67" },
    { CV_IA64_IntR68, L"IntR68" },
    { CV_IA64_IntR69, L"IntR69" },
    { CV_IA64_IntR70, L"IntR70" },
    { CV_IA64_IntR71, L"IntR71" },
    { CV_IA64_IntR72, L"IntR72" },
    { CV_IA64_IntR73, L"IntR73" },
    { CV_IA64_IntR74, L"IntR74" },
    { CV_IA64_IntR75, L"IntR75" },
    { CV_IA64_IntR76, L"IntR76" },
    { CV_IA64_IntR77, L"IntR77" },
    { CV_IA64_IntR78, L"IntR78" },
    { CV_IA64_IntR79, L"IntR79" },
    { CV_IA64_IntR80, L"IntR80" },
    { CV_IA64_IntR81, L"IntR81" },
    { CV_IA64_IntR82, L"IntR82" },
    { CV_IA64_IntR83, L"IntR83" },
    { CV_IA64_IntR84, L"IntR84" },
    { CV_IA64_IntR85, L"IntR85" },
    { CV_IA64_IntR86, L"IntR86" },
    { CV_IA64_IntR87, L"IntR87" },
    { CV_IA64_IntR88, L"IntR88" },
    { CV_IA64_IntR89, L"IntR89" },
    { CV_IA64_IntR90, L"IntR90" },
    { CV_IA64_IntR91, L"IntR91" },
    { CV_IA64_IntR92, L"IntR92" },
    { CV_IA64_IntR93, L"IntR93" },
    { CV_IA64_IntR94, L"IntR94" },
    { CV_IA64_IntR95, L"IntR95" },
    { CV_IA64_IntR96, L"IntR96" },
    { CV_IA64_IntR97, L"IntR97" },
    { CV_IA64_IntR98, L"IntR98" },
    { CV_IA64_IntR99, L"IntR99" },
    { CV_IA64_IntR100, L"IntR100" },
    { CV_IA64_IntR101, L"IntR101" },
    { CV_IA64_IntR102, L"IntR102" },
    { CV_IA64_IntR103, L"IntR103" },
    { CV_IA64_IntR104, L"IntR104" },
    { CV_IA64_IntR105, L"IntR105" },
    { CV_IA64_IntR106, L"IntR106" },
    { CV_IA64_IntR107, L"IntR107" },
    { CV_IA64_IntR108, L"IntR108" },
    { CV_IA64_IntR109, L"IntR109" },
    { CV_IA64_IntR110, L"IntR110" },
    { CV_IA64_IntR111, L"IntR111" },
    { CV_IA64_IntR112, L"IntR112" },
    { CV_IA64_IntR113, L"IntR113" },
    { CV_IA64_IntR114, L"IntR114" },
    { CV_IA64_IntR115, L"IntR115" },
    { CV_IA64_IntR116, L"IntR116" },
    { CV_IA64_IntR117, L"IntR117" },
    { CV_IA64_IntR118, L"IntR118" },
    { CV_IA64_IntR119, L"IntR119" },
    { CV_IA64_IntR120, L"IntR120" },
    { CV_IA64_IntR121, L"IntR121" },
    { CV_IA64_IntR122, L"IntR122" },
    { CV_IA64_IntR123, L"IntR123" },
    { CV_IA64_IntR124, L"IntR124" },
    { CV_IA64_IntR125, L"IntR125" },
    { CV_IA64_IntR126, L"IntR126" },
    { CV_IA64_IntR127, L"IntR127" },
    { CV_IA64_FltF0, L"FltF0" },
    { CV_IA64_FltF1, L"FltF1" },
    { CV_IA64_FltF2, L"FltF2" },
    { CV_IA64_FltF3, L"FltF3" },
    { CV_IA64_FltF4, L"FltF4" },
    { CV_IA64_FltF5, L"FltF5" },
    { CV_IA64_FltF6, L"FltF6" },
    { CV_IA64_FltF7, L"FltF7" },
    { CV_IA64_FltF8, L"FltF8" },
    { CV_IA64_FltF9, L"FltF9" },
    { CV_IA64_FltF10, L"FltF10" },
    { CV_IA64_FltF11, L"FltF11" },
    { CV_IA64_FltF12, L"FltF12" },
    { CV_IA64_FltF13, L"FltF13" },
    { CV_IA64_FltF14, L"FltF14" },
    { CV_IA64_FltF15, L"FltF15" },
    { CV_IA64_FltF16, L"FltF16" },
    { CV_IA64_FltF17, L"FltF17" },
    { CV_IA64_FltF18, L"FltF18" },
    { CV_IA64_FltF19, L"FltF19" },
    { CV_IA64_FltF20, L"FltF20" },
    { CV_IA64_FltF21, L"FltF21" },
    { CV_IA64_FltF22, L"FltF22" },
    { CV_IA64_FltF23, L"FltF23" },
    { CV_IA64_FltF24, L"FltF24" },
    { CV_IA64_FltF25, L"FltF25" },
    { CV_IA64_FltF26, L"FltF26" },
    { CV_IA64_FltF27, L"FltF27" },
    { CV_IA64_FltF28, L"FltF28" },
    { CV_IA64_FltF29, L"FltF29" },
    { CV_IA64_FltF30, L"FltF30" },
    { CV_IA64_FltF31, L"FltF31" },
    { CV_IA64_FltF32, L"FltF32" },
    { CV_IA64_FltF33, L"FltF33" },
    { CV_IA64_FltF34, L"FltF34" },
    { CV_IA64_FltF35, L"FltF35" },
    { CV_IA64_FltF36, L"FltF36" },
    { CV_IA64_FltF37, L"FltF37" },
    { CV_IA64_FltF38, L"FltF38" },
    { CV_IA64_FltF39, L"FltF39" },
    { CV_IA64_FltF40, L"FltF40" },
    { CV_IA64_FltF41, L"FltF41" },
    { CV_IA64_FltF42, L"FltF42" },
    { CV_IA64_FltF43, L"FltF43" },
    { CV_IA64_FltF44, L"FltF44" },
    { CV_IA64_FltF45, L"FltF45" },
    { CV_IA64_FltF46, L"FltF46" },
    { CV_IA64_FltF47, L"FltF47" },
    { CV_IA64_FltF48, L"FltF48" },
    { CV_IA64_FltF49, L"FltF49" },
    { CV_IA64_FltF50, L"FltF50" },
    { CV_IA64_FltF51, L"FltF51" },
    { CV_IA64_FltF52, L"FltF52" },
    { CV_IA64_FltF53, L"FltF53" },
    { CV_IA64_FltF54, L"FltF54" },
    { CV_IA64_FltF55, L"FltF55" },
    { CV_IA64_FltF56, L"FltF56" },
    { CV_IA64_FltF57, L"FltF57" },
    { CV_IA64_FltF58, L"FltF58" },
    { CV_IA64_FltF59, L"FltF59" },
    { CV_IA64_FltF60, L"FltF60" },
    { CV_IA64_FltF61, L"FltF61" },
    { CV_IA64_FltF62, L"FltF62" },
    { CV_IA64_FltF63, L"FltF63" },
    { CV_IA64_FltF64, L"FltF64" },
    { CV_IA64_FltF65, L"FltF65" },
    { CV_IA64_FltF66, L"FltF66" },
    { CV_IA64_FltF67, L"FltF67" },
    { CV_IA64_FltF68, L"FltF68" },
    { CV_IA64_FltF69, L"FltF69" },
    { CV_IA64_FltF70, L"FltF70" },
    { CV_IA64_FltF71, L"FltF71" },
    { CV_IA64_FltF72, L"FltF72" },
    { CV_IA64_FltF73, L"FltF73" },
    { CV_IA64_FltF74, L"FltF74" },
    { CV_IA64_FltF75, L"FltF75" },
    { CV_IA64_FltF76, L"FltF76" },
    { CV_IA64_FltF77, L"FltF77" },
    { CV_IA64_FltF78, L"FltF78" },
    { CV_IA64_FltF79, L"FltF79" },
    { CV_IA64_FltF80, L"FltF80" },
    { CV_IA64_FltF81, L"FltF81" },
    { CV_IA64_FltF82, L"FltF82" },
    { CV_IA64_FltF83, L"FltF83" },
    { CV_IA64_FltF84, L"FltF84" },
    { CV_IA64_FltF85, L"FltF85" },
    { CV_IA64_FltF86, L"FltF86" },
    { CV_IA64_FltF87, L"FltF87" },
    { CV_IA64_FltF88, L"FltF88" },
    { CV_IA64_FltF89, L"FltF89" },
    { CV_IA64_FltF90, L"FltF90" },
    { CV_IA64_FltF91, L"FltF91" },
    { CV_IA64_FltF92, L"FltF92" },
    { CV_IA64_FltF93, L"FltF93" },
    { CV_IA64_FltF94, L"FltF94" },
    { CV_IA64_FltF95, L"FltF95" },
    { CV_IA64_FltF96, L"FltF96" },
    { CV_IA64_FltF97, L"FltF97" },
    { CV_IA64_FltF98, L"FltF98" },
    { CV_IA64_FltF99, L"FltF99" },
    { CV_IA64_FltF100, L"FltF100" },
    { CV_IA64_FltF101, L"FltF101" },
    { CV_IA64_FltF102, L"FltF102" },
    { CV_IA64_FltF103, L"FltF103" },
    { CV_IA64_FltF104, L"FltF104" },
    { CV_IA64_FltF105, L"FltF105" },
    { CV_IA64_FltF106, L"FltF106" },
    { CV_IA64_FltF107, L"FltF107" },
    { CV_IA64_FltF108, L"FltF108" },
    { CV_IA64_FltF109, L"FltF109" },
    { CV_IA64_FltF110, L"FltF110" },
    { CV_IA64_FltF111, L"FltF111" },
    { CV_IA64_FltF112, L"FltF112" },
    { CV_IA64_FltF113, L"FltF113" },
    { CV_IA64_FltF114, L"FltF114" },
    { CV_IA64_FltF115, L"FltF115" },
    { CV_IA64_FltF116, L"FltF116" },
    { CV_IA64_FltF117, L"FltF117" },
    { CV_IA64_FltF118, L"FltF118" },
    { CV_IA64_FltF119, L"FltF119" },
    { CV_IA64_FltF120, L"FltF120" },
    { CV_IA64_FltF121, L"FltF121" },
    { CV_IA64_FltF122, L"FltF122" },
    { CV_IA64_FltF123, L"FltF123" },
    { CV_IA64_FltF124, L"FltF124" },
    { CV_IA64_FltF125, L"FltF125" },
    { CV_IA64_FltF126, L"FltF126" },
    { CV_IA64_FltF127, L"FltF127" },
    { CV_IA64_ApKR0, L"ApKR0" },
    { CV_IA64_ApKR1, L"ApKR1" },
    { CV_IA64_ApKR2, L"ApKR2" },
    { CV_IA64_ApKR3, L"ApKR3" },
    { CV_IA64_ApKR4, L"ApKR4" },
    { CV_IA64_ApKR5, L"ApKR5" },
    { CV_IA64_ApKR6, L"ApKR6" },
    { CV_IA64_ApKR7, L"ApKR7" },
    { CV_IA64_AR8, L"AR8" },
    { CV_IA64_AR9, L"AR9" },
    { CV_IA64_AR10, L"AR10" },
    { CV_IA64_AR11, L"AR11" },
    { CV_IA64_AR12, L"AR12" },
    { CV_IA64_AR13, L"AR13" },
    { CV_IA64_AR14, L"AR14" },
    { CV_IA64_AR15, L"AR15" },
    { CV_IA64_RsRSC, L"RsRSC" },
    { CV_IA64_RsBSP, L"RsBSP" },
    { CV_IA64_RsBSPSTORE, L"RsBSPSTORE" },
    { CV_IA64_RsRNAT, L"RsRNAT" },
    { CV_IA64_AR20, L"AR20" },
    { CV_IA64_StFCR, L"StFCR" },
    { CV_IA64_AR22, L"AR22" },
    { CV_IA64_AR23, L"AR23" },
    { CV_IA64_EFLAG, L"EFLAG" },
    { CV_IA64_CSD, L"CSD" },
    { CV_IA64_SSD, L"SSD" },
    { CV_IA64_CFLG, L"CFLG" },
    { CV_IA64_StFSR, L"StFSR" },
    { CV_IA64_StFIR, L"StFIR" },
    { CV_IA64_StFDR, L"StFDR" },
    { CV_IA64_AR31, L"AR31" },
    { CV_IA64_ApCCV, L"ApCCV" },
    { CV_IA64_AR33, L"AR33" },
    { CV_IA64_AR34, L"AR34" },
    { CV_IA64_AR35, L"AR35" },
    { CV_IA64_ApUNAT, L"ApUNAT" },
    { CV_IA64_AR37, L"AR37" },
    { CV_IA64_AR38, L"AR38" },
    { CV_IA64_AR39, L"AR39" },
    { CV_IA64_StFPSR, L"StFPSR" },
    { CV_IA64_AR41, L"AR41" },
    { CV_IA64_AR42, L"AR42" },
    { CV_IA64_AR43, L"AR43" },
    { CV_IA64_ApITC, L"ApITC" },
    { CV_IA64_AR45, L"AR45" },
    { CV_IA64_AR46, L"AR46" },
    { CV_IA64_AR47, L"AR47" },
    { CV_IA64_AR48, L"AR48" },
    { CV_IA64_AR49, L"AR49" },
    { CV_IA64_AR50, L"AR50" },
    { CV_IA64_AR51, L"AR51" },
    { CV_IA64_AR52, L"AR52" },
    { CV_IA64_AR53, L"AR53" },
    { CV_IA64_AR54, L"AR54" },
    { CV_IA64_AR55, L"AR55" },
    { CV_IA64_AR56, L"AR56" },
    { CV_IA64_AR57, L"AR57" },
    { CV_IA64_AR58, L"AR58" },
    { CV_IA64_AR59, L"AR59" },
    { CV_IA64_AR60, L"AR60" },
    { CV_IA64_AR61, L"AR61" },
    { CV_IA64_AR62, L"AR62" },
    { CV_IA64_AR63, L"AR63" },
    { CV_IA64_RsPFS, L"RsPFS" },
    { CV_IA64_ApLC, L"ApLC" },
    { CV_IA64_ApEC, L"ApEC" },
    { CV_IA64_AR67, L"AR67" },
    { CV_IA64_AR68, L"AR68" },
    { CV_IA64_AR69, L"AR69" },
    { CV_IA64_AR70, L"AR70" },
    { CV_IA64_AR71, L"AR71" },
    { CV_IA64_AR72, L"AR72" },
    { CV_IA64_AR73, L"AR73" },
    { CV_IA64_AR74, L"AR74" },
    { CV_IA64_AR75, L"AR75" },
    { CV_IA64_AR76, L"AR76" },
    { CV_IA64_AR77, L"AR77" },
    { CV_IA64_AR78, L"AR78" },
    { CV_IA64_AR79, L"AR79" },
    { CV_IA64_AR80, L"AR80" },
    { CV_IA64_AR81, L"AR81" },
    { CV_IA64_AR82, L"AR82" },
    { CV_IA64_AR83, L"AR83" },
    { CV_IA64_AR84, L"AR84" },
    { CV_IA64_AR85, L"AR85" },
    { CV_IA64_AR86, L"AR86" },
    { CV_IA64_AR87, L"AR87" },
    { CV_IA64_AR88, L"AR88" },
    { CV_IA64_AR89, L"AR89" },
    { CV_IA64_AR90, L"AR90" },
    { CV_IA64_AR91, L"AR91" },
    { CV_IA64_AR92, L"AR92" },
    { CV_IA64_AR93, L"AR93" },
    { CV_IA64_AR94, L"AR94" },
    { CV_IA64_AR95, L"AR95" },
    { CV_IA64_AR96, L"AR96" },
    { CV_IA64_AR97, L"AR97" },
    { CV_IA64_AR98, L"AR98" },
    { CV_IA64_AR99, L"AR99" },
    { CV_IA64_AR100, L"AR100" },
    { CV_IA64_AR101, L"AR101" },
    { CV_IA64_AR102, L"AR102" },
    { CV_IA64_AR103, L"AR103" },
    { CV_IA64_AR104, L"AR104" },
    { CV_IA64_AR105, L"AR105" },
    { CV_IA64_AR106, L"AR106" },
    { CV_IA64_AR107, L"AR107" },
    { CV_IA64_AR108, L"AR108" },
    { CV_IA64_AR109, L"AR109" },
    { CV_IA64_AR110, L"AR110" },
    { CV_IA64_AR111, L"AR111" },
    { CV_IA64_AR112, L"AR112" },
    { CV_IA64_AR113, L"AR113" },
    { CV_IA64_AR114, L"AR114" },
    { CV_IA64_AR115, L"AR115" },
    { CV_IA64_AR116, L"AR116" },
    { CV_IA64_AR117, L"AR117" },
    { CV_IA64_AR118, L"AR118" },
    { CV_IA64_AR119, L"AR119" },
    { CV_IA64_AR120, L"AR120" },
    { CV_IA64_AR121, L"AR121" },
    { CV_IA64_AR122, L"AR122" },
    { CV_IA64_AR123, L"AR123" },
    { CV_IA64_AR124, L"AR124" },
    { CV_IA64_AR125, L"AR125" },
    { CV_IA64_AR126, L"AR126" },
    { CV_IA64_AR127, L"AR127" },
    { CV_IA64_ApDCR, L"ApDCR" },
    { CV_IA64_ApITM, L"ApITM" },
    { CV_IA64_ApIVA, L"ApIVA" },
    { CV_IA64_CR3, L"CR3" },
    { CV_IA64_CR4, L"CR4" },
    { CV_IA64_CR5, L"CR5" },
    { CV_IA64_CR6, L"CR6" },
    { CV_IA64_CR7, L"CR7" },
    { CV_IA64_ApPTA, L"ApPTA" },
    { CV_IA64_ApGPTA, L"ApGPTA" },
    { CV_IA64_CR10, L"CR10" },
    { CV_IA64_CR11, L"CR11" },
    { CV_IA64_CR12, L"CR12" },
    { CV_IA64_CR13, L"CR13" },
    { CV_IA64_CR14, L"CR14" },
    { CV_IA64_CR15, L"CR15" },
    { CV_IA64_StIPSR, L"StIPSR" },
    { CV_IA64_StISR, L"StISR" },
    { CV_IA64_CR18, L"CR18" },
    { CV_IA64_StIIP, L"StIIP" },
    { CV_IA64_StIFA, L"StIFA" },
    { CV_IA64_StITIR, L"StITIR" },
    { CV_IA64_StIIPA, L"StIIPA" },
    { CV_IA64_StIFS, L"StIFS" },
    { CV_IA64_StIIM, L"StIIM" },
    { CV_IA64_StIHA, L"StIHA" },
    { CV_IA64_CR26, L"CR26" },
    { CV_IA64_CR27, L"CR27" },
    { CV_IA64_CR28, L"CR28" },
    { CV_IA64_CR29, L"CR29" },
    { CV_IA64_CR30, L"CR30" },
    { CV_IA64_CR31, L"CR31" },
    { CV_IA64_CR32, L"CR32" },
    { CV_IA64_CR33, L"CR33" },
    { CV_IA64_CR34, L"CR34" },
    { CV_IA64_CR35, L"CR35" },
    { CV_IA64_CR36, L"CR36" },
    { CV_IA64_CR37, L"CR37" },
    { CV_IA64_CR38, L"CR38" },
    { CV_IA64_CR39, L"CR39" },
    { CV_IA64_CR40, L"CR40" },
    { CV_IA64_CR41, L"CR41" },
    { CV_IA64_CR42, L"CR42" },
    { CV_IA64_CR43, L"CR43" },
    { CV_IA64_CR44, L"CR44" },
    { CV_IA64_CR45, L"CR45" },
    { CV_IA64_CR46, L"CR46" },
    { CV_IA64_CR47, L"CR47" },
    { CV_IA64_CR48, L"CR48" },
    { CV_IA64_CR49, L"CR49" },
    { CV_IA64_CR50, L"CR50" },
    { CV_IA64_CR51, L"CR51" },
    { CV_IA64_CR52, L"CR52" },
    { CV_IA64_CR53, L"CR53" },
    { CV_IA64_CR54, L"CR54" },
    { CV_IA64_CR55, L"CR55" },
    { CV_IA64_CR56, L"CR56" },
    { CV_IA64_CR57, L"CR57" },
    { CV_IA64_CR58, L"CR58" },
    { CV_IA64_CR59, L"CR59" },
    { CV_IA64_CR60, L"CR60" },
    { CV_IA64_CR61, L"CR61" },
    { CV_IA64_CR62, L"CR62" },
    { CV_IA64_CR63, L"CR63" },
    { CV_IA64_SaLID, L"SaLID" },
    { CV_IA64_SaIVR, L"SaIVR" },
    { CV_IA64_SaTPR, L"SaTPR" },
    { CV_IA64_SaEOI, L"SaEOI" },
    { CV_IA64_SaIRR0, L"SaIRR0" },
    { CV_IA64_SaIRR1, L"SaIRR1" },
    { CV_IA64_SaIRR2, L"SaIRR2" },
    { CV_IA64_SaIRR3, L"SaIRR3" },
    { CV_IA64_SaITV, L"SaITV" },
    { CV_IA64_SaPMV, L"SaPMV" },
    { CV_IA64_SaCMCV, L"SaCMCV" },
    { CV_IA64_CR75, L"CR75" },
    { CV_IA64_CR76, L"CR76" },
    { CV_IA64_CR77, L"CR77" },
    { CV_IA64_CR78, L"CR78" },
    { CV_IA64_CR79, L"CR79" },
    { CV_IA64_SaLRR0, L"SaLRR0" },
    { CV_IA64_SaLRR1, L"SaLRR1" },
    { CV_IA64_CR82, L"CR82" },
    { CV_IA64_CR83, L"CR83" },
    { CV_IA64_CR84, L"CR84" },
    { CV_IA64_CR85, L"CR85" },
    { CV_IA64_CR86, L"CR86" },
    { CV_IA64_CR87, L"CR87" },
    { CV_IA64_CR88, L"CR88" },
    { CV_IA64_CR89, L"CR89" },
    { CV_IA64_CR90, L"CR90" },
    { CV_IA64_CR91, L"CR91" },
    { CV_IA64_CR92, L"CR92" },
    { CV_IA64_CR93, L"CR93" },
    { CV_IA64_CR94, L"CR94" },
    { CV_IA64_CR95, L"CR95" },
    { CV_IA64_SaIRR0, L"SaIRR0" },
    { CV_IA64_CR97, L"CR97" },
    { CV_IA64_SaIRR1, L"SaIRR1" },
    { CV_IA64_CR99, L"CR99" },
    { CV_IA64_SaIRR2, L"SaIRR2" },
    { CV_IA64_CR101, L"CR101" },
    { CV_IA64_SaIRR3, L"SaIRR3" },
    { CV_IA64_CR103, L"CR103" },
    { CV_IA64_CR104, L"CR104" },
    { CV_IA64_CR105, L"CR105" },
    { CV_IA64_CR106, L"CR106" },
    { CV_IA64_CR107, L"CR107" },
    { CV_IA64_CR108, L"CR108" },
    { CV_IA64_CR109, L"CR109" },
    { CV_IA64_CR110, L"CR110" },
    { CV_IA64_CR111, L"CR111" },
    { CV_IA64_CR112, L"CR112" },
    { CV_IA64_CR113, L"CR113" },
    { CV_IA64_SaITV, L"SaITV" },
    { CV_IA64_CR115, L"CR115" },
    { CV_IA64_SaPMV, L"SaPMV" },
    { CV_IA64_SaLRR0, L"SaLRR0" },
    { CV_IA64_SaLRR1, L"SaLRR1" },
    { CV_IA64_SaCMCV, L"SaCMCV" },
    { CV_IA64_CR120, L"CR120" },
    { CV_IA64_CR121, L"CR121" },
    { CV_IA64_CR122, L"CR122" },
    { CV_IA64_CR123, L"CR123" },
    { CV_IA64_CR124, L"CR124" },
    { CV_IA64_CR125, L"CR125" },
    { CV_IA64_CR126, L"CR126" },
    { CV_IA64_CR127, L"CR127" },
    { CV_IA64_Pkr0, L"Pkr0" },
    { CV_IA64_Pkr1, L"Pkr1" },
    { CV_IA64_Pkr2, L"Pkr2" },
    { CV_IA64_Pkr3, L"Pkr3" },
    { CV_IA64_Pkr4, L"Pkr4" },
    { CV_IA64_Pkr5, L"Pkr5" },
    { CV_IA64_Pkr6, L"Pkr6" },
    { CV_IA64_Pkr7, L"Pkr7" },
    { CV_IA64_Pkr8, L"Pkr8" },
    { CV_IA64_Pkr9, L"Pkr9" },
    { CV_IA64_Pkr10, L"Pkr10" },
    { CV_IA64_Pkr11, L"Pkr11" },
    { CV_IA64_Pkr12, L"Pkr12" },
    { CV_IA64_Pkr13, L"Pkr13" },
    { CV_IA64_Pkr14, L"Pkr14" },
    { CV_IA64_Pkr15, L"Pkr15" },
    { CV_IA64_Rr0, L"Rr0" },
    { CV_IA64_Rr1, L"Rr1" },
    { CV_IA64_Rr2, L"Rr2" },
    { CV_IA64_Rr3, L"Rr3" },
    { CV_IA64_Rr4, L"Rr4" },
    { CV_IA64_Rr5, L"Rr5" },
    { CV_IA64_Rr6, L"Rr6" },
    { CV_IA64_Rr7, L"Rr7" },
    { CV_IA64_PFD0, L"PFD0" },
    { CV_IA64_PFD1, L"PFD1" },
    { CV_IA64_PFD2, L"PFD2" },
    { CV_IA64_PFD3, L"PFD3" },
    { CV_IA64_PFD4, L"PFD4" },
    { CV_IA64_PFD5, L"PFD5" },
    { CV_IA64_PFD6, L"PFD6" },
    { CV_IA64_PFD7, L"PFD7" },
    { CV_IA64_PFC0, L"PFC0" },
    { CV_IA64_PFC1, L"PFC1" },
    { CV_IA64_PFC2, L"PFC2" },
    { CV_IA64_PFC3, L"PFC3" },
    { CV_IA64_PFC4, L"PFC4" },
    { CV_IA64_PFC5, L"PFC5" },
    { CV_IA64_PFC6, L"PFC6" },
    { CV_IA64_PFC7, L"PFC7" },
    { CV_IA64_TrI0, L"TrI0" },
    { CV_IA64_TrI1, L"TrI1" },
    { CV_IA64_TrI2, L"TrI2" },
    { CV_IA64_TrI3, L"TrI3" },
    { CV_IA64_TrI4, L"TrI4" },
    { CV_IA64_TrI5, L"TrI5" },
    { CV_IA64_TrI6, L"TrI6" },
    { CV_IA64_TrI7, L"TrI7" },
    { CV_IA64_TrD0, L"TrD0" },
    { CV_IA64_TrD1, L"TrD1" },
    { CV_IA64_TrD2, L"TrD2" },
    { CV_IA64_TrD3, L"TrD3" },
    { CV_IA64_TrD4, L"TrD4" },
    { CV_IA64_TrD5, L"TrD5" },
    { CV_IA64_TrD6, L"TrD6" },
    { CV_IA64_TrD7, L"TrD7" },
    { CV_IA64_DbI0, L"DbI0" },
    { CV_IA64_DbI1, L"DbI1" },
    { CV_IA64_DbI2, L"DbI2" },
    { CV_IA64_DbI3, L"DbI3" },
    { CV_IA64_DbI4, L"DbI4" },
    { CV_IA64_DbI5, L"DbI5" },
    { CV_IA64_DbI6, L"DbI6" },
    { CV_IA64_DbI7, L"DbI7" },
    { CV_IA64_DbD0, L"DbD0" },
    { CV_IA64_DbD1, L"DbD1" },
    { CV_IA64_DbD2, L"DbD2" },
    { CV_IA64_DbD3, L"DbD3" },
    { CV_IA64_DbD4, L"DbD4" },
    { CV_IA64_DbD5, L"DbD5" },
    { CV_IA64_DbD6, L"DbD6" },
    { CV_IA64_DbD7, L"DbD7" }
};

////////////////////////////////////////////////////////////
// Map an IA64 registry ID with the corresponding string name
//
int __cdecl cmpIa64regSz(const void *pv1, const void *pv2) {
  const MapIa64Reg *p1 = (MapIa64Reg *) pv1;
  const MapIa64Reg *p2 = (MapIa64Reg *) pv2;
  
  if(p1->iCvReg < p2->iCvReg){
    return -1;
  }
  if(p1->iCvReg > p2->iCvReg){
    return 1;
  }
  return 0;
}

////////////////////////////////////////////////////////////
// Map a registry id code with the corresponding string name
//
const wchar_t* SzNameC7Reg(USHORT reg, DWORD MachineType){
  static wchar_t wszRegNum[64];
  
	switch(reg){
		case CV_ALLREG_LOCALS : return L"BaseOfLocals";
		case CV_ALLREG_PARAMS : return L"BaseOfParams";
		case CV_ALLREG_VFRAME : return L"VFrame";
	}
  swprintf_s(wszRegNum, L"???(0x%x)", reg);
  switch(MachineType) {
    case CV_CFL_8080:
    case CV_CFL_8086:
    case CV_CFL_80286:
    case CV_CFL_80386:
    case CV_CFL_80486:
    case CV_CFL_PENTIUM:
      if(reg < (sizeof(rgRegX86)/sizeof(*rgRegX86))){
        return(rgRegX86[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_ALPHA:
      if(reg < (sizeof(rgRegAlpha)/sizeof(*rgRegAlpha))){
        return(rgRegAlpha[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_MIPSR4000:
    case CV_CFL_MIPS16:
      if(reg < (sizeof(rgRegMips)/sizeof(*rgRegMips))) {
        return(rgRegMips[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_M68000:
    case CV_CFL_M68010:
    case CV_CFL_M68020:
    case CV_CFL_M68030:
    case CV_CFL_M68040:
      if(reg < (sizeof(rgReg68k)/sizeof(*rgReg68k))){
        return(rgReg68k[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_PPC601:
    case CV_CFL_PPC603:
    case CV_CFL_PPC604:
    case CV_CFL_PPC620:
      if(reg < (sizeof(rgRegPpc)/sizeof(*rgRegPpc))){
        return(rgRegPpc[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_SH3:
      if(reg < (sizeof(rgRegSh)/sizeof(*rgRegSh))){
        return(rgRegSh[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_ARM3:
    case CV_CFL_ARM4:
    case CV_CFL_ARM4T:
      if(reg < (sizeof(rgRegArm)/sizeof(*rgRegArm))){
        return(rgRegArm[reg]);
      }
      return wszRegNum;
      break;
    case CV_CFL_IA64: {
      MapIa64Reg *p;
      MapIa64Reg  m = {(CV_HREG_e) reg};
      p = (MapIa64Reg *) bsearch(&m,
                                 mpIa64regSz,
                                 sizeof(mpIa64regSz)/sizeof(*mpIa64regSz),
                                 sizeof(MapIa64Reg),
                                 cmpIa64regSz);
      if (p) {
        return p->wszRegName;
      }else{
        return wszRegNum;
      }
      break;
    }
    case CV_CFL_AMD64 :
      if (reg < sizeof(rgRegAMD64)/sizeof(*rgRegAMD64)) {
        return rgRegAMD64[reg];
      }else{
        return wszRegNum;
      }
      break;
    default:
      return wszRegNum;
      break;
  }
}

const wchar_t* SzNameC7Reg(USHORT reg){
    return SzNameC7Reg(reg, g_dwMachineType);
}

