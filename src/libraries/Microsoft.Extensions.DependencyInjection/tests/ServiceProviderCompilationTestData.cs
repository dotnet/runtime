// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    interface I0 { }
    class S0 : I0 { }
    interface I1 { }
    class S1 : I1 { public S1(I0 s) { } }
    interface I2 { }
    class S2 : I2 { public S2(I1 s) { } }
    interface I3 { }
    class S3 : I3 { public S3(I2 s) { } }
    interface I4 { }
    class S4 : I4 { public S4(I3 s) { } }
    interface I5 { }
    class S5 : I5 { public S5(I4 s) { } }
    interface I6 { }
    class S6 : I6 { public S6(I5 s) { } }
    interface I7 { }
    class S7 : I7 { public S7(I6 s) { } }
    interface I8 { }
    class S8 : I8 { public S8(I7 s) { } }
    interface I9 { }
    class S9 : I9 { public S9(I8 s) { } }
    interface I10 { }
    class S10 : I10 { public S10(I9 s) { } }
    interface I11 { }
    class S11 : I11 { public S11(I10 s) { } }
    interface I12 { }
    class S12 : I12 { public S12(I11 s) { } }
    interface I13 { }
    class S13 : I13 { public S13(I12 s) { } }
    interface I14 { }
    class S14 : I14 { public S14(I13 s) { } }
    interface I15 { }
    class S15 : I15 { public S15(I14 s) { } }
    interface I16 { }
    class S16 : I16 { public S16(I15 s) { } }
    interface I17 { }
    class S17 : I17 { public S17(I16 s) { } }
    interface I18 { }
    class S18 : I18 { public S18(I17 s) { } }
    interface I19 { }
    class S19 : I19 { public S19(I18 s) { } }
    interface I20 { }
    class S20 : I20 { public S20(I19 s) { } }
    interface I21 { }
    class S21 : I21 { public S21(I20 s) { } }
    interface I22 { }
    class S22 : I22 { public S22(I21 s) { } }
    interface I23 { }
    class S23 : I23 { public S23(I22 s) { } }
    interface I24 { }
    class S24 : I24 { public S24(I23 s) { } }
    interface I25 { }
    class S25 : I25 { public S25(I24 s) { } }
    interface I26 { }
    class S26 : I26 { public S26(I25 s) { } }
    interface I27 { }
    class S27 : I27 { public S27(I26 s) { } }
    interface I28 { }
    class S28 : I28 { public S28(I27 s) { } }
    interface I29 { }
    class S29 : I29 { public S29(I28 s) { } }
    interface I30 { }
    class S30 : I30 { public S30(I29 s) { } }
    interface I31 { }
    class S31 : I31 { public S31(I30 s) { } }
    interface I32 { }
    class S32 : I32 { public S32(I31 s) { } }
    interface I33 { }
    class S33 : I33 { public S33(I32 s) { } }
    interface I34 { }
    class S34 : I34 { public S34(I33 s) { } }
    interface I35 { }
    class S35 : I35 { public S35(I34 s) { } }
    interface I36 { }
    class S36 : I36 { public S36(I35 s) { } }
    interface I37 { }
    class S37 : I37 { public S37(I36 s) { } }
    interface I38 { }
    class S38 : I38 { public S38(I37 s) { } }
    interface I39 { }
    class S39 : I39 { public S39(I38 s) { } }
    interface I40 { }
    class S40 : I40 { public S40(I39 s) { } }
    interface I41 { }
    class S41 : I41 { public S41(I40 s) { } }
    interface I42 { }
    class S42 : I42 { public S42(I41 s) { } }
    interface I43 { }
    class S43 : I43 { public S43(I42 s) { } }
    interface I44 { }
    class S44 : I44 { public S44(I43 s) { } }
    interface I45 { }
    class S45 : I45 { public S45(I44 s) { } }
    interface I46 { }
    class S46 : I46 { public S46(I45 s) { } }
    interface I47 { }
    class S47 : I47 { public S47(I46 s) { } }
    interface I48 { }
    class S48 : I48 { public S48(I47 s) { } }
    interface I49 { }
    class S49 : I49 { public S49(I48 s) { } }
    interface I50 { }
    class S50 : I50 { public S50(I49 s) { } }
    interface I51 { }
    class S51 : I51 { public S51(I50 s) { } }
    interface I52 { }
    class S52 : I52 { public S52(I51 s) { } }
    interface I53 { }
    class S53 : I53 { public S53(I52 s) { } }
    interface I54 { }
    class S54 : I54 { public S54(I53 s) { } }
    interface I55 { }
    class S55 : I55 { public S55(I54 s) { } }
    interface I56 { }
    class S56 : I56 { public S56(I55 s) { } }
    interface I57 { }
    class S57 : I57 { public S57(I56 s) { } }
    interface I58 { }
    class S58 : I58 { public S58(I57 s) { } }
    interface I59 { }
    class S59 : I59 { public S59(I58 s) { } }
    interface I60 { }
    class S60 : I60 { public S60(I59 s) { } }
    interface I61 { }
    class S61 : I61 { public S61(I60 s) { } }
    interface I62 { }
    class S62 : I62 { public S62(I61 s) { } }
    interface I63 { }
    class S63 : I63 { public S63(I62 s) { } }
    interface I64 { }
    class S64 : I64 { public S64(I63 s) { } }
    interface I65 { }
    class S65 : I65 { public S65(I64 s) { } }
    interface I66 { }
    class S66 : I66 { public S66(I65 s) { } }
    interface I67 { }
    class S67 : I67 { public S67(I66 s) { } }
    interface I68 { }
    class S68 : I68 { public S68(I67 s) { } }
    interface I69 { }
    class S69 : I69 { public S69(I68 s) { } }
    interface I70 { }
    class S70 : I70 { public S70(I69 s) { } }
    interface I71 { }
    class S71 : I71 { public S71(I70 s) { } }
    interface I72 { }
    class S72 : I72 { public S72(I71 s) { } }
    interface I73 { }
    class S73 : I73 { public S73(I72 s) { } }
    interface I74 { }
    class S74 : I74 { public S74(I73 s) { } }
    interface I75 { }
    class S75 : I75 { public S75(I74 s) { } }
    interface I76 { }
    class S76 : I76 { public S76(I75 s) { } }
    interface I77 { }
    class S77 : I77 { public S77(I76 s) { } }
    interface I78 { }
    class S78 : I78 { public S78(I77 s) { } }
    interface I79 { }
    class S79 : I79 { public S79(I78 s) { } }
    interface I80 { }
    class S80 : I80 { public S80(I79 s) { } }
    interface I81 { }
    class S81 : I81 { public S81(I80 s) { } }
    interface I82 { }
    class S82 : I82 { public S82(I81 s) { } }
    interface I83 { }
    class S83 : I83 { public S83(I82 s) { } }
    interface I84 { }
    class S84 : I84 { public S84(I83 s) { } }
    interface I85 { }
    class S85 : I85 { public S85(I84 s) { } }
    interface I86 { }
    class S86 : I86 { public S86(I85 s) { } }
    interface I87 { }
    class S87 : I87 { public S87(I86 s) { } }
    interface I88 { }
    class S88 : I88 { public S88(I87 s) { } }
    interface I89 { }
    class S89 : I89 { public S89(I88 s) { } }
    interface I90 { }
    class S90 : I90 { public S90(I89 s) { } }
    interface I91 { }
    class S91 : I91 { public S91(I90 s) { } }
    interface I92 { }
    class S92 : I92 { public S92(I91 s) { } }
    interface I93 { }
    class S93 : I93 { public S93(I92 s) { } }
    interface I94 { }
    class S94 : I94 { public S94(I93 s) { } }
    interface I95 { }
    class S95 : I95 { public S95(I94 s) { } }
    interface I96 { }
    class S96 : I96 { public S96(I95 s) { } }
    interface I97 { }
    class S97 : I97 { public S97(I96 s) { } }
    interface I98 { }
    class S98 : I98 { public S98(I97 s) { } }
    interface I99 { }
    class S99 : I99 { public S99(I98 s) { } }
    interface I100 { }
    class S100 : I100 { public S100(I99 s) { } }
    interface I101 { }
    class S101 : I101 { public S101(I100 s) { } }
    interface I102 { }
    class S102 : I102 { public S102(I101 s) { } }
    interface I103 { }
    class S103 : I103 { public S103(I102 s) { } }
    interface I104 { }
    class S104 : I104 { public S104(I103 s) { } }
    interface I105 { }
    class S105 : I105 { public S105(I104 s) { } }
    interface I106 { }
    class S106 : I106 { public S106(I105 s) { } }
    interface I107 { }
    class S107 : I107 { public S107(I106 s) { } }
    interface I108 { }
    class S108 : I108 { public S108(I107 s) { } }
    interface I109 { }
    class S109 : I109 { public S109(I108 s) { } }
    interface I110 { }
    class S110 : I110 { public S110(I109 s) { } }
    interface I111 { }
    class S111 : I111 { public S111(I110 s) { } }
    interface I112 { }
    class S112 : I112 { public S112(I111 s) { } }
    interface I113 { }
    class S113 : I113 { public S113(I112 s) { } }
    interface I114 { }
    class S114 : I114 { public S114(I113 s) { } }
    interface I115 { }
    class S115 : I115 { public S115(I114 s) { } }
    interface I116 { }
    class S116 : I116 { public S116(I115 s) { } }
    interface I117 { }
    class S117 : I117 { public S117(I116 s) { } }
    interface I118 { }
    class S118 : I118 { public S118(I117 s) { } }
    interface I119 { }
    class S119 : I119 { public S119(I118 s) { } }
    interface I120 { }
    class S120 : I120 { public S120(I119 s) { } }
    interface I121 { }
    class S121 : I121 { public S121(I120 s) { } }
    interface I122 { }
    class S122 : I122 { public S122(I121 s) { } }
    interface I123 { }
    class S123 : I123 { public S123(I122 s) { } }
    interface I124 { }
    class S124 : I124 { public S124(I123 s) { } }
    interface I125 { }
    class S125 : I125 { public S125(I124 s) { } }
    interface I126 { }
    class S126 : I126 { public S126(I125 s) { } }
    interface I127 { }
    class S127 : I127 { public S127(I126 s) { } }
    interface I128 { }
    class S128 : I128 { public S128(I127 s) { } }
    interface I129 { }
    class S129 : I129 { public S129(I128 s) { } }
    interface I130 { }
    class S130 : I130 { public S130(I129 s) { } }
    interface I131 { }
    class S131 : I131 { public S131(I130 s) { } }
    interface I132 { }
    class S132 : I132 { public S132(I131 s) { } }
    interface I133 { }
    class S133 : I133 { public S133(I132 s) { } }
    interface I134 { }
    class S134 : I134 { public S134(I133 s) { } }
    interface I135 { }
    class S135 : I135 { public S135(I134 s) { } }
    interface I136 { }
    class S136 : I136 { public S136(I135 s) { } }
    interface I137 { }
    class S137 : I137 { public S137(I136 s) { } }
    interface I138 { }
    class S138 : I138 { public S138(I137 s) { } }
    interface I139 { }
    class S139 : I139 { public S139(I138 s) { } }
    interface I140 { }
    class S140 : I140 { public S140(I139 s) { } }
    interface I141 { }
    class S141 : I141 { public S141(I140 s) { } }
    interface I142 { }
    class S142 : I142 { public S142(I141 s) { } }
    interface I143 { }
    class S143 : I143 { public S143(I142 s) { } }
    interface I144 { }
    class S144 : I144 { public S144(I143 s) { } }
    interface I145 { }
    class S145 : I145 { public S145(I144 s) { } }
    interface I146 { }
    class S146 : I146 { public S146(I145 s) { } }
    interface I147 { }
    class S147 : I147 { public S147(I146 s) { } }
    interface I148 { }
    class S148 : I148 { public S148(I147 s) { } }
    interface I149 { }
    class S149 : I149 { public S149(I148 s) { } }
    interface I150 { }
    class S150 : I150 { public S150(I149 s) { } }
    interface I151 { }
    class S151 : I151 { public S151(I150 s) { } }
    interface I152 { }
    class S152 : I152 { public S152(I151 s) { } }
    interface I153 { }
    class S153 : I153 { public S153(I152 s) { } }
    interface I154 { }
    class S154 : I154 { public S154(I153 s) { } }
    interface I155 { }
    class S155 : I155 { public S155(I154 s) { } }
    interface I156 { }
    class S156 : I156 { public S156(I155 s) { } }
    interface I157 { }
    class S157 : I157 { public S157(I156 s) { } }
    interface I158 { }
    class S158 : I158 { public S158(I157 s) { } }
    interface I159 { }
    class S159 : I159 { public S159(I158 s) { } }
    interface I160 { }
    class S160 : I160 { public S160(I159 s) { } }
    interface I161 { }
    class S161 : I161 { public S161(I160 s) { } }
    interface I162 { }
    class S162 : I162 { public S162(I161 s) { } }
    interface I163 { }
    class S163 : I163 { public S163(I162 s) { } }
    interface I164 { }
    class S164 : I164 { public S164(I163 s) { } }
    interface I165 { }
    class S165 : I165 { public S165(I164 s) { } }
    interface I166 { }
    class S166 : I166 { public S166(I165 s) { } }
    interface I167 { }
    class S167 : I167 { public S167(I166 s) { } }
    interface I168 { }
    class S168 : I168 { public S168(I167 s) { } }
    interface I169 { }
    class S169 : I169 { public S169(I168 s) { } }
    interface I170 { }
    class S170 : I170 { public S170(I169 s) { } }
    interface I171 { }
    class S171 : I171 { public S171(I170 s) { } }
    interface I172 { }
    class S172 : I172 { public S172(I171 s) { } }
    interface I173 { }
    class S173 : I173 { public S173(I172 s) { } }
    interface I174 { }
    class S174 : I174 { public S174(I173 s) { } }
    interface I175 { }
    class S175 : I175 { public S175(I174 s) { } }
    interface I176 { }
    class S176 : I176 { public S176(I175 s) { } }
    interface I177 { }
    class S177 : I177 { public S177(I176 s) { } }
    interface I178 { }
    class S178 : I178 { public S178(I177 s) { } }
    interface I179 { }
    class S179 : I179 { public S179(I178 s) { } }
    interface I180 { }
    class S180 : I180 { public S180(I179 s) { } }
    interface I181 { }
    class S181 : I181 { public S181(I180 s) { } }
    interface I182 { }
    class S182 : I182 { public S182(I181 s) { } }
    interface I183 { }
    class S183 : I183 { public S183(I182 s) { } }
    interface I184 { }
    class S184 : I184 { public S184(I183 s) { } }
    interface I185 { }
    class S185 : I185 { public S185(I184 s) { } }
    interface I186 { }
    class S186 : I186 { public S186(I185 s) { } }
    interface I187 { }
    class S187 : I187 { public S187(I186 s) { } }
    interface I188 { }
    class S188 : I188 { public S188(I187 s) { } }
    interface I189 { }
    class S189 : I189 { public S189(I188 s) { } }
    interface I190 { }
    class S190 : I190 { public S190(I189 s) { } }
    interface I191 { }
    class S191 : I191 { public S191(I190 s) { } }
    interface I192 { }
    class S192 : I192 { public S192(I191 s) { } }
    interface I193 { }
    class S193 : I193 { public S193(I192 s) { } }
    interface I194 { }
    class S194 : I194 { public S194(I193 s) { } }
    interface I195 { }
    class S195 : I195 { public S195(I194 s) { } }
    interface I196 { }
    class S196 : I196 { public S196(I195 s) { } }
    interface I197 { }
    class S197 : I197 { public S197(I196 s) { } }
    interface I198 { }
    class S198 : I198 { public S198(I197 s) { } }
    interface I199 { }
    class S199 : I199 { public S199(I198 s) { } }
    interface I200 { }
    class S200 : I200 { public S200(I199 s) { } }
    interface I201 { }
    class S201 : I201 { public S201(I200 s) { } }
    interface I202 { }
    class S202 : I202 { public S202(I201 s) { } }
    interface I203 { }
    class S203 : I203 { public S203(I202 s) { } }
    interface I204 { }
    class S204 : I204 { public S204(I203 s) { } }
    interface I205 { }
    class S205 : I205 { public S205(I204 s) { } }
    interface I206 { }
    class S206 : I206 { public S206(I205 s) { } }
    interface I207 { }
    class S207 : I207 { public S207(I206 s) { } }
    interface I208 { }
    class S208 : I208 { public S208(I207 s) { } }
    interface I209 { }
    class S209 : I209 { public S209(I208 s) { } }
    interface I210 { }
    class S210 : I210 { public S210(I209 s) { } }
    interface I211 { }
    class S211 : I211 { public S211(I210 s) { } }
    interface I212 { }
    class S212 : I212 { public S212(I211 s) { } }
    interface I213 { }
    class S213 : I213 { public S213(I212 s) { } }
    interface I214 { }
    class S214 : I214 { public S214(I213 s) { } }
    interface I215 { }
    class S215 : I215 { public S215(I214 s) { } }
    interface I216 { }
    class S216 : I216 { public S216(I215 s) { } }
    interface I217 { }
    class S217 : I217 { public S217(I216 s) { } }
    interface I218 { }
    class S218 : I218 { public S218(I217 s) { } }
    interface I219 { }
    class S219 : I219 { public S219(I218 s) { } }
    interface I220 { }
    class S220 : I220 { public S220(I219 s) { } }
    interface I221 { }
    class S221 : I221 { public S221(I220 s) { } }
    interface I222 { }
    class S222 : I222 { public S222(I221 s) { } }
    interface I223 { }
    class S223 : I223 { public S223(I222 s) { } }
    interface I224 { }
    class S224 : I224 { public S224(I223 s) { } }
    interface I225 { }
    class S225 : I225 { public S225(I224 s) { } }
    interface I226 { }
    class S226 : I226 { public S226(I225 s) { } }
    interface I227 { }
    class S227 : I227 { public S227(I226 s) { } }
    interface I228 { }
    class S228 : I228 { public S228(I227 s) { } }
    interface I229 { }
    class S229 : I229 { public S229(I228 s) { } }
    interface I230 { }
    class S230 : I230 { public S230(I229 s) { } }
    interface I231 { }
    class S231 : I231 { public S231(I230 s) { } }
    interface I232 { }
    class S232 : I232 { public S232(I231 s) { } }
    interface I233 { }
    class S233 : I233 { public S233(I232 s) { } }
    interface I234 { }
    class S234 : I234 { public S234(I233 s) { } }
    interface I235 { }
    class S235 : I235 { public S235(I234 s) { } }
    interface I236 { }
    class S236 : I236 { public S236(I235 s) { } }
    interface I237 { }
    class S237 : I237 { public S237(I236 s) { } }
    interface I238 { }
    class S238 : I238 { public S238(I237 s) { } }
    interface I239 { }
    class S239 : I239 { public S239(I238 s) { } }
    interface I240 { }
    class S240 : I240 { public S240(I239 s) { } }
    interface I241 { }
    class S241 : I241 { public S241(I240 s) { } }
    interface I242 { }
    class S242 : I242 { public S242(I241 s) { } }
    interface I243 { }
    class S243 : I243 { public S243(I242 s) { } }
    interface I244 { }
    class S244 : I244 { public S244(I243 s) { } }
    interface I245 { }
    class S245 : I245 { public S245(I244 s) { } }
    interface I246 { }
    class S246 : I246 { public S246(I245 s) { } }
    interface I247 { }
    class S247 : I247 { public S247(I246 s) { } }
    interface I248 { }
    class S248 : I248 { public S248(I247 s) { } }
    interface I249 { }
    class S249 : I249 { public S249(I248 s) { } }
    interface I250 { }
    class S250 : I250 { public S250(I249 s) { } }
    interface I251 { }
    class S251 : I251 { public S251(I250 s) { } }
    interface I252 { }
    class S252 : I252 { public S252(I251 s) { } }
    interface I253 { }
    class S253 : I253 { public S253(I252 s) { } }
    interface I254 { }
    class S254 : I254 { public S254(I253 s) { } }
    interface I255 { }
    class S255 : I255 { public S255(I254 s) { } }
    interface I256 { }
    class S256 : I256 { public S256(I255 s) { } }
    interface I257 { }
    class S257 : I257 { public S257(I256 s) { } }
    interface I258 { }
    class S258 : I258 { public S258(I257 s) { } }
    interface I259 { }
    class S259 : I259 { public S259(I258 s) { } }
    interface I260 { }
    class S260 : I260 { public S260(I259 s) { } }
    interface I261 { }
    class S261 : I261 { public S261(I260 s) { } }
    interface I262 { }
    class S262 : I262 { public S262(I261 s) { } }
    interface I263 { }
    class S263 : I263 { public S263(I262 s) { } }
    interface I264 { }
    class S264 : I264 { public S264(I263 s) { } }
    interface I265 { }
    class S265 : I265 { public S265(I264 s) { } }
    interface I266 { }
    class S266 : I266 { public S266(I265 s) { } }
    interface I267 { }
    class S267 : I267 { public S267(I266 s) { } }
    interface I268 { }
    class S268 : I268 { public S268(I267 s) { } }
    interface I269 { }
    class S269 : I269 { public S269(I268 s) { } }
    interface I270 { }
    class S270 : I270 { public S270(I269 s) { } }
    interface I271 { }
    class S271 : I271 { public S271(I270 s) { } }
    interface I272 { }
    class S272 : I272 { public S272(I271 s) { } }
    interface I273 { }
    class S273 : I273 { public S273(I272 s) { } }
    interface I274 { }
    class S274 : I274 { public S274(I273 s) { } }
    interface I275 { }
    class S275 : I275 { public S275(I274 s) { } }
    interface I276 { }
    class S276 : I276 { public S276(I275 s) { } }
    interface I277 { }
    class S277 : I277 { public S277(I276 s) { } }
    interface I278 { }
    class S278 : I278 { public S278(I277 s) { } }
    interface I279 { }
    class S279 : I279 { public S279(I278 s) { } }
    interface I280 { }
    class S280 : I280 { public S280(I279 s) { } }
    interface I281 { }
    class S281 : I281 { public S281(I280 s) { } }
    interface I282 { }
    class S282 : I282 { public S282(I281 s) { } }
    interface I283 { }
    class S283 : I283 { public S283(I282 s) { } }
    interface I284 { }
    class S284 : I284 { public S284(I283 s) { } }
    interface I285 { }
    class S285 : I285 { public S285(I284 s) { } }
    interface I286 { }
    class S286 : I286 { public S286(I285 s) { } }
    interface I287 { }
    class S287 : I287 { public S287(I286 s) { } }
    interface I288 { }
    class S288 : I288 { public S288(I287 s) { } }
    interface I289 { }
    class S289 : I289 { public S289(I288 s) { } }
    interface I290 { }
    class S290 : I290 { public S290(I289 s) { } }
    interface I291 { }
    class S291 : I291 { public S291(I290 s) { } }
    interface I292 { }
    class S292 : I292 { public S292(I291 s) { } }
    interface I293 { }
    class S293 : I293 { public S293(I292 s) { } }
    interface I294 { }
    class S294 : I294 { public S294(I293 s) { } }
    interface I295 { }
    class S295 : I295 { public S295(I294 s) { } }
    interface I296 { }
    class S296 : I296 { public S296(I295 s) { } }
    interface I297 { }
    class S297 : I297 { public S297(I296 s) { } }
    interface I298 { }
    class S298 : I298 { public S298(I297 s) { } }
    interface I299 { }
    class S299 : I299 { public S299(I298 s) { } }
    interface I300 { }
    class S300 : I300 { public S300(I299 s) { } }
    interface I301 { }
    class S301 : I301 { public S301(I300 s) { } }
    interface I302 { }
    class S302 : I302 { public S302(I301 s) { } }
    interface I303 { }
    class S303 : I303 { public S303(I302 s) { } }
    interface I304 { }
    class S304 : I304 { public S304(I303 s) { } }
    interface I305 { }
    class S305 : I305 { public S305(I304 s) { } }
    interface I306 { }
    class S306 : I306 { public S306(I305 s) { } }
    interface I307 { }
    class S307 : I307 { public S307(I306 s) { } }
    interface I308 { }
    class S308 : I308 { public S308(I307 s) { } }
    interface I309 { }
    class S309 : I309 { public S309(I308 s) { } }
    interface I310 { }
    class S310 : I310 { public S310(I309 s) { } }
    interface I311 { }
    class S311 : I311 { public S311(I310 s) { } }
    interface I312 { }
    class S312 : I312 { public S312(I311 s) { } }
    interface I313 { }
    class S313 : I313 { public S313(I312 s) { } }
    interface I314 { }
    class S314 : I314 { public S314(I313 s) { } }
    interface I315 { }
    class S315 : I315 { public S315(I314 s) { } }
    interface I316 { }
    class S316 : I316 { public S316(I315 s) { } }
    interface I317 { }
    class S317 : I317 { public S317(I316 s) { } }
    interface I318 { }
    class S318 : I318 { public S318(I317 s) { } }
    interface I319 { }
    class S319 : I319 { public S319(I318 s) { } }
    interface I320 { }
    class S320 : I320 { public S320(I319 s) { } }
    interface I321 { }
    class S321 : I321 { public S321(I320 s) { } }
    interface I322 { }
    class S322 : I322 { public S322(I321 s) { } }
    interface I323 { }
    class S323 : I323 { public S323(I322 s) { } }
    interface I324 { }
    class S324 : I324 { public S324(I323 s) { } }
    interface I325 { }
    class S325 : I325 { public S325(I324 s) { } }
    interface I326 { }
    class S326 : I326 { public S326(I325 s) { } }
    interface I327 { }
    class S327 : I327 { public S327(I326 s) { } }
    interface I328 { }
    class S328 : I328 { public S328(I327 s) { } }
    interface I329 { }
    class S329 : I329 { public S329(I328 s) { } }
    interface I330 { }
    class S330 : I330 { public S330(I329 s) { } }
    interface I331 { }
    class S331 : I331 { public S331(I330 s) { } }
    interface I332 { }
    class S332 : I332 { public S332(I331 s) { } }
    interface I333 { }
    class S333 : I333 { public S333(I332 s) { } }
    interface I334 { }
    class S334 : I334 { public S334(I333 s) { } }
    interface I335 { }
    class S335 : I335 { public S335(I334 s) { } }
    interface I336 { }
    class S336 : I336 { public S336(I335 s) { } }
    interface I337 { }
    class S337 : I337 { public S337(I336 s) { } }
    interface I338 { }
    class S338 : I338 { public S338(I337 s) { } }
    interface I339 { }
    class S339 : I339 { public S339(I338 s) { } }
    interface I340 { }
    class S340 : I340 { public S340(I339 s) { } }
    interface I341 { }
    class S341 : I341 { public S341(I340 s) { } }
    interface I342 { }
    class S342 : I342 { public S342(I341 s) { } }
    interface I343 { }
    class S343 : I343 { public S343(I342 s) { } }
    interface I344 { }
    class S344 : I344 { public S344(I343 s) { } }
    interface I345 { }
    class S345 : I345 { public S345(I344 s) { } }
    interface I346 { }
    class S346 : I346 { public S346(I345 s) { } }
    interface I347 { }
    class S347 : I347 { public S347(I346 s) { } }
    interface I348 { }
    class S348 : I348 { public S348(I347 s) { } }
    interface I349 { }
    class S349 : I349 { public S349(I348 s) { } }
    interface I350 { }
    class S350 : I350 { public S350(I349 s) { } }
    interface I351 { }
    class S351 : I351 { public S351(I350 s) { } }
    interface I352 { }
    class S352 : I352 { public S352(I351 s) { } }
    interface I353 { }
    class S353 : I353 { public S353(I352 s) { } }
    interface I354 { }
    class S354 : I354 { public S354(I353 s) { } }
    interface I355 { }
    class S355 : I355 { public S355(I354 s) { } }
    interface I356 { }
    class S356 : I356 { public S356(I355 s) { } }
    interface I357 { }
    class S357 : I357 { public S357(I356 s) { } }
    interface I358 { }
    class S358 : I358 { public S358(I357 s) { } }
    interface I359 { }
    class S359 : I359 { public S359(I358 s) { } }
    interface I360 { }
    class S360 : I360 { public S360(I359 s) { } }
    interface I361 { }
    class S361 : I361 { public S361(I360 s) { } }
    interface I362 { }
    class S362 : I362 { public S362(I361 s) { } }
    interface I363 { }
    class S363 : I363 { public S363(I362 s) { } }
    interface I364 { }
    class S364 : I364 { public S364(I363 s) { } }
    interface I365 { }
    class S365 : I365 { public S365(I364 s) { } }
    interface I366 { }
    class S366 : I366 { public S366(I365 s) { } }
    interface I367 { }
    class S367 : I367 { public S367(I366 s) { } }
    interface I368 { }
    class S368 : I368 { public S368(I367 s) { } }
    interface I369 { }
    class S369 : I369 { public S369(I368 s) { } }
    interface I370 { }
    class S370 : I370 { public S370(I369 s) { } }
    interface I371 { }
    class S371 : I371 { public S371(I370 s) { } }
    interface I372 { }
    class S372 : I372 { public S372(I371 s) { } }
    interface I373 { }
    class S373 : I373 { public S373(I372 s) { } }
    interface I374 { }
    class S374 : I374 { public S374(I373 s) { } }
    interface I375 { }
    class S375 : I375 { public S375(I374 s) { } }
    interface I376 { }
    class S376 : I376 { public S376(I375 s) { } }
    interface I377 { }
    class S377 : I377 { public S377(I376 s) { } }
    interface I378 { }
    class S378 : I378 { public S378(I377 s) { } }
    interface I379 { }
    class S379 : I379 { public S379(I378 s) { } }
    interface I380 { }
    class S380 : I380 { public S380(I379 s) { } }
    interface I381 { }
    class S381 : I381 { public S381(I380 s) { } }
    interface I382 { }
    class S382 : I382 { public S382(I381 s) { } }
    interface I383 { }
    class S383 : I383 { public S383(I382 s) { } }
    interface I384 { }
    class S384 : I384 { public S384(I383 s) { } }
    interface I385 { }
    class S385 : I385 { public S385(I384 s) { } }
    interface I386 { }
    class S386 : I386 { public S386(I385 s) { } }
    interface I387 { }
    class S387 : I387 { public S387(I386 s) { } }
    interface I388 { }
    class S388 : I388 { public S388(I387 s) { } }
    interface I389 { }
    class S389 : I389 { public S389(I388 s) { } }
    interface I390 { }
    class S390 : I390 { public S390(I389 s) { } }
    interface I391 { }
    class S391 : I391 { public S391(I390 s) { } }
    interface I392 { }
    class S392 : I392 { public S392(I391 s) { } }
    interface I393 { }
    class S393 : I393 { public S393(I392 s) { } }
    interface I394 { }
    class S394 : I394 { public S394(I393 s) { } }
    interface I395 { }
    class S395 : I395 { public S395(I394 s) { } }
    interface I396 { }
    class S396 : I396 { public S396(I395 s) { } }
    interface I397 { }
    class S397 : I397 { public S397(I396 s) { } }
    interface I398 { }
    class S398 : I398 { public S398(I397 s) { } }
    interface I399 { }
    class S399 : I399 { public S399(I398 s) { } }
    interface I400 { }
    class S400 : I400 { public S400(I399 s) { } }
    interface I401 { }
    class S401 : I401 { public S401(I400 s) { } }
    interface I402 { }
    class S402 : I402 { public S402(I401 s) { } }
    interface I403 { }
    class S403 : I403 { public S403(I402 s) { } }
    interface I404 { }
    class S404 : I404 { public S404(I403 s) { } }
    interface I405 { }
    class S405 : I405 { public S405(I404 s) { } }
    interface I406 { }
    class S406 : I406 { public S406(I405 s) { } }
    interface I407 { }
    class S407 : I407 { public S407(I406 s) { } }
    interface I408 { }
    class S408 : I408 { public S408(I407 s) { } }
    interface I409 { }
    class S409 : I409 { public S409(I408 s) { } }
    interface I410 { }
    class S410 : I410 { public S410(I409 s) { } }
    interface I411 { }
    class S411 : I411 { public S411(I410 s) { } }
    interface I412 { }
    class S412 : I412 { public S412(I411 s) { } }
    interface I413 { }
    class S413 : I413 { public S413(I412 s) { } }
    interface I414 { }
    class S414 : I414 { public S414(I413 s) { } }
    interface I415 { }
    class S415 : I415 { public S415(I414 s) { } }
    interface I416 { }
    class S416 : I416 { public S416(I415 s) { } }
    interface I417 { }
    class S417 : I417 { public S417(I416 s) { } }
    interface I418 { }
    class S418 : I418 { public S418(I417 s) { } }
    interface I419 { }
    class S419 : I419 { public S419(I418 s) { } }
    interface I420 { }
    class S420 : I420 { public S420(I419 s) { } }
    interface I421 { }
    class S421 : I421 { public S421(I420 s) { } }
    interface I422 { }
    class S422 : I422 { public S422(I421 s) { } }
    interface I423 { }
    class S423 : I423 { public S423(I422 s) { } }
    interface I424 { }
    class S424 : I424 { public S424(I423 s) { } }
    interface I425 { }
    class S425 : I425 { public S425(I424 s) { } }
    interface I426 { }
    class S426 : I426 { public S426(I425 s) { } }
    interface I427 { }
    class S427 : I427 { public S427(I426 s) { } }
    interface I428 { }
    class S428 : I428 { public S428(I427 s) { } }
    interface I429 { }
    class S429 : I429 { public S429(I428 s) { } }
    interface I430 { }
    class S430 : I430 { public S430(I429 s) { } }
    interface I431 { }
    class S431 : I431 { public S431(I430 s) { } }
    interface I432 { }
    class S432 : I432 { public S432(I431 s) { } }
    interface I433 { }
    class S433 : I433 { public S433(I432 s) { } }
    interface I434 { }
    class S434 : I434 { public S434(I433 s) { } }
    interface I435 { }
    class S435 : I435 { public S435(I434 s) { } }
    interface I436 { }
    class S436 : I436 { public S436(I435 s) { } }
    interface I437 { }
    class S437 : I437 { public S437(I436 s) { } }
    interface I438 { }
    class S438 : I438 { public S438(I437 s) { } }
    interface I439 { }
    class S439 : I439 { public S439(I438 s) { } }
    interface I440 { }
    class S440 : I440 { public S440(I439 s) { } }
    interface I441 { }
    class S441 : I441 { public S441(I440 s) { } }
    interface I442 { }
    class S442 : I442 { public S442(I441 s) { } }
    interface I443 { }
    class S443 : I443 { public S443(I442 s) { } }
    interface I444 { }
    class S444 : I444 { public S444(I443 s) { } }
    interface I445 { }
    class S445 : I445 { public S445(I444 s) { } }
    interface I446 { }
    class S446 : I446 { public S446(I445 s) { } }
    interface I447 { }
    class S447 : I447 { public S447(I446 s) { } }
    interface I448 { }
    class S448 : I448 { public S448(I447 s) { } }
    interface I449 { }
    class S449 : I449 { public S449(I448 s) { } }
    interface I450 { }
    class S450 : I450 { public S450(I449 s) { } }
    interface I451 { }
    class S451 : I451 { public S451(I450 s) { } }
    interface I452 { }
    class S452 : I452 { public S452(I451 s) { } }
    interface I453 { }
    class S453 : I453 { public S453(I452 s) { } }
    interface I454 { }
    class S454 : I454 { public S454(I453 s) { } }
    interface I455 { }
    class S455 : I455 { public S455(I454 s) { } }
    interface I456 { }
    class S456 : I456 { public S456(I455 s) { } }
    interface I457 { }
    class S457 : I457 { public S457(I456 s) { } }
    interface I458 { }
    class S458 : I458 { public S458(I457 s) { } }
    interface I459 { }
    class S459 : I459 { public S459(I458 s) { } }
    interface I460 { }
    class S460 : I460 { public S460(I459 s) { } }
    interface I461 { }
    class S461 : I461 { public S461(I460 s) { } }
    interface I462 { }
    class S462 : I462 { public S462(I461 s) { } }
    interface I463 { }
    class S463 : I463 { public S463(I462 s) { } }
    interface I464 { }
    class S464 : I464 { public S464(I463 s) { } }
    interface I465 { }
    class S465 : I465 { public S465(I464 s) { } }
    interface I466 { }
    class S466 : I466 { public S466(I465 s) { } }
    interface I467 { }
    class S467 : I467 { public S467(I466 s) { } }
    interface I468 { }
    class S468 : I468 { public S468(I467 s) { } }
    interface I469 { }
    class S469 : I469 { public S469(I468 s) { } }
    interface I470 { }
    class S470 : I470 { public S470(I469 s) { } }
    interface I471 { }
    class S471 : I471 { public S471(I470 s) { } }
    interface I472 { }
    class S472 : I472 { public S472(I471 s) { } }
    interface I473 { }
    class S473 : I473 { public S473(I472 s) { } }
    interface I474 { }
    class S474 : I474 { public S474(I473 s) { } }
    interface I475 { }
    class S475 : I475 { public S475(I474 s) { } }
    interface I476 { }
    class S476 : I476 { public S476(I475 s) { } }
    interface I477 { }
    class S477 : I477 { public S477(I476 s) { } }
    interface I478 { }
    class S478 : I478 { public S478(I477 s) { } }
    interface I479 { }
    class S479 : I479 { public S479(I478 s) { } }
    interface I480 { }
    class S480 : I480 { public S480(I479 s) { } }
    interface I481 { }
    class S481 : I481 { public S481(I480 s) { } }
    interface I482 { }
    class S482 : I482 { public S482(I481 s) { } }
    interface I483 { }
    class S483 : I483 { public S483(I482 s) { } }
    interface I484 { }
    class S484 : I484 { public S484(I483 s) { } }
    interface I485 { }
    class S485 : I485 { public S485(I484 s) { } }
    interface I486 { }
    class S486 : I486 { public S486(I485 s) { } }
    interface I487 { }
    class S487 : I487 { public S487(I486 s) { } }
    interface I488 { }
    class S488 : I488 { public S488(I487 s) { } }
    interface I489 { }
    class S489 : I489 { public S489(I488 s) { } }
    interface I490 { }
    class S490 : I490 { public S490(I489 s) { } }
    interface I491 { }
    class S491 : I491 { public S491(I490 s) { } }
    interface I492 { }
    class S492 : I492 { public S492(I491 s) { } }
    interface I493 { }
    class S493 : I493 { public S493(I492 s) { } }
    interface I494 { }
    class S494 : I494 { public S494(I493 s) { } }
    interface I495 { }
    class S495 : I495 { public S495(I494 s) { } }
    interface I496 { }
    class S496 : I496 { public S496(I495 s) { } }
    interface I497 { }
    class S497 : I497 { public S497(I496 s) { } }
    interface I498 { }
    class S498 : I498 { public S498(I497 s) { } }
    interface I499 { }
    class S499 : I499 { public S499(I498 s) { } }
    interface I500 { }
    class S500 : I500 { public S500(I499 s) { } }
    interface I501 { }
    class S501 : I501 { public S501(I500 s) { } }
    interface I502 { }
    class S502 : I502 { public S502(I501 s) { } }
    interface I503 { }
    class S503 : I503 { public S503(I502 s) { } }
    interface I504 { }
    class S504 : I504 { public S504(I503 s) { } }
    interface I505 { }
    class S505 : I505 { public S505(I504 s) { } }
    interface I506 { }
    class S506 : I506 { public S506(I505 s) { } }
    interface I507 { }
    class S507 : I507 { public S507(I506 s) { } }
    interface I508 { }
    class S508 : I508 { public S508(I507 s) { } }
    interface I509 { }
    class S509 : I509 { public S509(I508 s) { } }
    interface I510 { }
    class S510 : I510 { public S510(I509 s) { } }
    interface I511 { }
    class S511 : I511 { public S511(I510 s) { } }
    interface I512 { }
    class S512 : I512 { public S512(I511 s) { } }
    interface I513 { }
    class S513 : I513 { public S513(I512 s) { } }
    interface I514 { }
    class S514 : I514 { public S514(I513 s) { } }
    interface I515 { }
    class S515 : I515 { public S515(I514 s) { } }
    interface I516 { }
    class S516 : I516 { public S516(I515 s) { } }
    interface I517 { }
    class S517 : I517 { public S517(I516 s) { } }
    interface I518 { }
    class S518 : I518 { public S518(I517 s) { } }
    interface I519 { }
    class S519 : I519 { public S519(I518 s) { } }
    interface I520 { }
    class S520 : I520 { public S520(I519 s) { } }
    interface I521 { }
    class S521 : I521 { public S521(I520 s) { } }
    interface I522 { }
    class S522 : I522 { public S522(I521 s) { } }
    interface I523 { }
    class S523 : I523 { public S523(I522 s) { } }
    interface I524 { }
    class S524 : I524 { public S524(I523 s) { } }
    interface I525 { }
    class S525 : I525 { public S525(I524 s) { } }
    interface I526 { }
    class S526 : I526 { public S526(I525 s) { } }
    interface I527 { }
    class S527 : I527 { public S527(I526 s) { } }
    interface I528 { }
    class S528 : I528 { public S528(I527 s) { } }
    interface I529 { }
    class S529 : I529 { public S529(I528 s) { } }
    interface I530 { }
    class S530 : I530 { public S530(I529 s) { } }
    interface I531 { }
    class S531 : I531 { public S531(I530 s) { } }
    interface I532 { }
    class S532 : I532 { public S532(I531 s) { } }
    interface I533 { }
    class S533 : I533 { public S533(I532 s) { } }
    interface I534 { }
    class S534 : I534 { public S534(I533 s) { } }
    interface I535 { }
    class S535 : I535 { public S535(I534 s) { } }
    interface I536 { }
    class S536 : I536 { public S536(I535 s) { } }
    interface I537 { }
    class S537 : I537 { public S537(I536 s) { } }
    interface I538 { }
    class S538 : I538 { public S538(I537 s) { } }
    interface I539 { }
    class S539 : I539 { public S539(I538 s) { } }
    interface I540 { }
    class S540 : I540 { public S540(I539 s) { } }
    interface I541 { }
    class S541 : I541 { public S541(I540 s) { } }
    interface I542 { }
    class S542 : I542 { public S542(I541 s) { } }
    interface I543 { }
    class S543 : I543 { public S543(I542 s) { } }
    interface I544 { }
    class S544 : I544 { public S544(I543 s) { } }
    interface I545 { }
    class S545 : I545 { public S545(I544 s) { } }
    interface I546 { }
    class S546 : I546 { public S546(I545 s) { } }
    interface I547 { }
    class S547 : I547 { public S547(I546 s) { } }
    interface I548 { }
    class S548 : I548 { public S548(I547 s) { } }
    interface I549 { }
    class S549 : I549 { public S549(I548 s) { } }
    interface I550 { }
    class S550 : I550 { public S550(I549 s) { } }
    interface I551 { }
    class S551 : I551 { public S551(I550 s) { } }
    interface I552 { }
    class S552 : I552 { public S552(I551 s) { } }
    interface I553 { }
    class S553 : I553 { public S553(I552 s) { } }
    interface I554 { }
    class S554 : I554 { public S554(I553 s) { } }
    interface I555 { }
    class S555 : I555 { public S555(I554 s) { } }
    interface I556 { }
    class S556 : I556 { public S556(I555 s) { } }
    interface I557 { }
    class S557 : I557 { public S557(I556 s) { } }
    interface I558 { }
    class S558 : I558 { public S558(I557 s) { } }
    interface I559 { }
    class S559 : I559 { public S559(I558 s) { } }
    interface I560 { }
    class S560 : I560 { public S560(I559 s) { } }
    interface I561 { }
    class S561 : I561 { public S561(I560 s) { } }
    interface I562 { }
    class S562 : I562 { public S562(I561 s) { } }
    interface I563 { }
    class S563 : I563 { public S563(I562 s) { } }
    interface I564 { }
    class S564 : I564 { public S564(I563 s) { } }
    interface I565 { }
    class S565 : I565 { public S565(I564 s) { } }
    interface I566 { }
    class S566 : I566 { public S566(I565 s) { } }
    interface I567 { }
    class S567 : I567 { public S567(I566 s) { } }
    interface I568 { }
    class S568 : I568 { public S568(I567 s) { } }
    interface I569 { }
    class S569 : I569 { public S569(I568 s) { } }
    interface I570 { }
    class S570 : I570 { public S570(I569 s) { } }
    interface I571 { }
    class S571 : I571 { public S571(I570 s) { } }
    interface I572 { }
    class S572 : I572 { public S572(I571 s) { } }
    interface I573 { }
    class S573 : I573 { public S573(I572 s) { } }
    interface I574 { }
    class S574 : I574 { public S574(I573 s) { } }
    interface I575 { }
    class S575 : I575 { public S575(I574 s) { } }
    interface I576 { }
    class S576 : I576 { public S576(I575 s) { } }
    interface I577 { }
    class S577 : I577 { public S577(I576 s) { } }
    interface I578 { }
    class S578 : I578 { public S578(I577 s) { } }
    interface I579 { }
    class S579 : I579 { public S579(I578 s) { } }
    interface I580 { }
    class S580 : I580 { public S580(I579 s) { } }
    interface I581 { }
    class S581 : I581 { public S581(I580 s) { } }
    interface I582 { }
    class S582 : I582 { public S582(I581 s) { } }
    interface I583 { }
    class S583 : I583 { public S583(I582 s) { } }
    interface I584 { }
    class S584 : I584 { public S584(I583 s) { } }
    interface I585 { }
    class S585 : I585 { public S585(I584 s) { } }
    interface I586 { }
    class S586 : I586 { public S586(I585 s) { } }
    interface I587 { }
    class S587 : I587 { public S587(I586 s) { } }
    interface I588 { }
    class S588 : I588 { public S588(I587 s) { } }
    interface I589 { }
    class S589 : I589 { public S589(I588 s) { } }
    interface I590 { }
    class S590 : I590 { public S590(I589 s) { } }
    interface I591 { }
    class S591 : I591 { public S591(I590 s) { } }
    interface I592 { }
    class S592 : I592 { public S592(I591 s) { } }
    interface I593 { }
    class S593 : I593 { public S593(I592 s) { } }
    interface I594 { }
    class S594 : I594 { public S594(I593 s) { } }
    interface I595 { }
    class S595 : I595 { public S595(I594 s) { } }
    interface I596 { }
    class S596 : I596 { public S596(I595 s) { } }
    interface I597 { }
    class S597 : I597 { public S597(I596 s) { } }
    interface I598 { }
    class S598 : I598 { public S598(I597 s) { } }
    interface I599 { }
    class S599 : I599 { public S599(I598 s) { } }
    interface I600 { }
    class S600 : I600 { public S600(I599 s) { } }
    interface I601 { }
    class S601 : I601 { public S601(I600 s) { } }
    interface I602 { }
    class S602 : I602 { public S602(I601 s) { } }
    interface I603 { }
    class S603 : I603 { public S603(I602 s) { } }
    interface I604 { }
    class S604 : I604 { public S604(I603 s) { } }
    interface I605 { }
    class S605 : I605 { public S605(I604 s) { } }
    interface I606 { }
    class S606 : I606 { public S606(I605 s) { } }
    interface I607 { }
    class S607 : I607 { public S607(I606 s) { } }
    interface I608 { }
    class S608 : I608 { public S608(I607 s) { } }
    interface I609 { }
    class S609 : I609 { public S609(I608 s) { } }
    interface I610 { }
    class S610 : I610 { public S610(I609 s) { } }
    interface I611 { }
    class S611 : I611 { public S611(I610 s) { } }
    interface I612 { }
    class S612 : I612 { public S612(I611 s) { } }
    interface I613 { }
    class S613 : I613 { public S613(I612 s) { } }
    interface I614 { }
    class S614 : I614 { public S614(I613 s) { } }
    interface I615 { }
    class S615 : I615 { public S615(I614 s) { } }
    interface I616 { }
    class S616 : I616 { public S616(I615 s) { } }
    interface I617 { }
    class S617 : I617 { public S617(I616 s) { } }
    interface I618 { }
    class S618 : I618 { public S618(I617 s) { } }
    interface I619 { }
    class S619 : I619 { public S619(I618 s) { } }
    interface I620 { }
    class S620 : I620 { public S620(I619 s) { } }
    interface I621 { }
    class S621 : I621 { public S621(I620 s) { } }
    interface I622 { }
    class S622 : I622 { public S622(I621 s) { } }
    interface I623 { }
    class S623 : I623 { public S623(I622 s) { } }
    interface I624 { }
    class S624 : I624 { public S624(I623 s) { } }
    interface I625 { }
    class S625 : I625 { public S625(I624 s) { } }
    interface I626 { }
    class S626 : I626 { public S626(I625 s) { } }
    interface I627 { }
    class S627 : I627 { public S627(I626 s) { } }
    interface I628 { }
    class S628 : I628 { public S628(I627 s) { } }
    interface I629 { }
    class S629 : I629 { public S629(I628 s) { } }
    interface I630 { }
    class S630 : I630 { public S630(I629 s) { } }
    interface I631 { }
    class S631 : I631 { public S631(I630 s) { } }
    interface I632 { }
    class S632 : I632 { public S632(I631 s) { } }
    interface I633 { }
    class S633 : I633 { public S633(I632 s) { } }
    interface I634 { }
    class S634 : I634 { public S634(I633 s) { } }
    interface I635 { }
    class S635 : I635 { public S635(I634 s) { } }
    interface I636 { }
    class S636 : I636 { public S636(I635 s) { } }
    interface I637 { }
    class S637 : I637 { public S637(I636 s) { } }
    interface I638 { }
    class S638 : I638 { public S638(I637 s) { } }
    interface I639 { }
    class S639 : I639 { public S639(I638 s) { } }
    interface I640 { }
    class S640 : I640 { public S640(I639 s) { } }
    interface I641 { }
    class S641 : I641 { public S641(I640 s) { } }
    interface I642 { }
    class S642 : I642 { public S642(I641 s) { } }
    interface I643 { }
    class S643 : I643 { public S643(I642 s) { } }
    interface I644 { }
    class S644 : I644 { public S644(I643 s) { } }
    interface I645 { }
    class S645 : I645 { public S645(I644 s) { } }
    interface I646 { }
    class S646 : I646 { public S646(I645 s) { } }
    interface I647 { }
    class S647 : I647 { public S647(I646 s) { } }
    interface I648 { }
    class S648 : I648 { public S648(I647 s) { } }
    interface I649 { }
    class S649 : I649 { public S649(I648 s) { } }
    interface I650 { }
    class S650 : I650 { public S650(I649 s) { } }
    interface I651 { }
    class S651 : I651 { public S651(I650 s) { } }
    interface I652 { }
    class S652 : I652 { public S652(I651 s) { } }
    interface I653 { }
    class S653 : I653 { public S653(I652 s) { } }
    interface I654 { }
    class S654 : I654 { public S654(I653 s) { } }
    interface I655 { }
    class S655 : I655 { public S655(I654 s) { } }
    interface I656 { }
    class S656 : I656 { public S656(I655 s) { } }
    interface I657 { }
    class S657 : I657 { public S657(I656 s) { } }
    interface I658 { }
    class S658 : I658 { public S658(I657 s) { } }
    interface I659 { }
    class S659 : I659 { public S659(I658 s) { } }
    interface I660 { }
    class S660 : I660 { public S660(I659 s) { } }
    interface I661 { }
    class S661 : I661 { public S661(I660 s) { } }
    interface I662 { }
    class S662 : I662 { public S662(I661 s) { } }
    interface I663 { }
    class S663 : I663 { public S663(I662 s) { } }
    interface I664 { }
    class S664 : I664 { public S664(I663 s) { } }
    interface I665 { }
    class S665 : I665 { public S665(I664 s) { } }
    interface I666 { }
    class S666 : I666 { public S666(I665 s) { } }
    interface I667 { }
    class S667 : I667 { public S667(I666 s) { } }
    interface I668 { }
    class S668 : I668 { public S668(I667 s) { } }
    interface I669 { }
    class S669 : I669 { public S669(I668 s) { } }
    interface I670 { }
    class S670 : I670 { public S670(I669 s) { } }
    interface I671 { }
    class S671 : I671 { public S671(I670 s) { } }
    interface I672 { }
    class S672 : I672 { public S672(I671 s) { } }
    interface I673 { }
    class S673 : I673 { public S673(I672 s) { } }
    interface I674 { }
    class S674 : I674 { public S674(I673 s) { } }
    interface I675 { }
    class S675 : I675 { public S675(I674 s) { } }
    interface I676 { }
    class S676 : I676 { public S676(I675 s) { } }
    interface I677 { }
    class S677 : I677 { public S677(I676 s) { } }
    interface I678 { }
    class S678 : I678 { public S678(I677 s) { } }
    interface I679 { }
    class S679 : I679 { public S679(I678 s) { } }
    interface I680 { }
    class S680 : I680 { public S680(I679 s) { } }
    interface I681 { }
    class S681 : I681 { public S681(I680 s) { } }
    interface I682 { }
    class S682 : I682 { public S682(I681 s) { } }
    interface I683 { }
    class S683 : I683 { public S683(I682 s) { } }
    interface I684 { }
    class S684 : I684 { public S684(I683 s) { } }
    interface I685 { }
    class S685 : I685 { public S685(I684 s) { } }
    interface I686 { }
    class S686 : I686 { public S686(I685 s) { } }
    interface I687 { }
    class S687 : I687 { public S687(I686 s) { } }
    interface I688 { }
    class S688 : I688 { public S688(I687 s) { } }
    interface I689 { }
    class S689 : I689 { public S689(I688 s) { } }
    interface I690 { }
    class S690 : I690 { public S690(I689 s) { } }
    interface I691 { }
    class S691 : I691 { public S691(I690 s) { } }
    interface I692 { }
    class S692 : I692 { public S692(I691 s) { } }
    interface I693 { }
    class S693 : I693 { public S693(I692 s) { } }
    interface I694 { }
    class S694 : I694 { public S694(I693 s) { } }
    interface I695 { }
    class S695 : I695 { public S695(I694 s) { } }
    interface I696 { }
    class S696 : I696 { public S696(I695 s) { } }
    interface I697 { }
    class S697 : I697 { public S697(I696 s) { } }
    interface I698 { }
    class S698 : I698 { public S698(I697 s) { } }
    interface I699 { }
    class S699 : I699 { public S699(I698 s) { } }
    interface I700 { }
    class S700 : I700 { public S700(I699 s) { } }
    interface I701 { }
    class S701 : I701 { public S701(I700 s) { } }
    interface I702 { }
    class S702 : I702 { public S702(I701 s) { } }
    interface I703 { }
    class S703 : I703 { public S703(I702 s) { } }
    interface I704 { }
    class S704 : I704 { public S704(I703 s) { } }
    interface I705 { }
    class S705 : I705 { public S705(I704 s) { } }
    interface I706 { }
    class S706 : I706 { public S706(I705 s) { } }
    interface I707 { }
    class S707 : I707 { public S707(I706 s) { } }
    interface I708 { }
    class S708 : I708 { public S708(I707 s) { } }
    interface I709 { }
    class S709 : I709 { public S709(I708 s) { } }
    interface I710 { }
    class S710 : I710 { public S710(I709 s) { } }
    interface I711 { }
    class S711 : I711 { public S711(I710 s) { } }
    interface I712 { }
    class S712 : I712 { public S712(I711 s) { } }
    interface I713 { }
    class S713 : I713 { public S713(I712 s) { } }
    interface I714 { }
    class S714 : I714 { public S714(I713 s) { } }
    interface I715 { }
    class S715 : I715 { public S715(I714 s) { } }
    interface I716 { }
    class S716 : I716 { public S716(I715 s) { } }
    interface I717 { }
    class S717 : I717 { public S717(I716 s) { } }
    interface I718 { }
    class S718 : I718 { public S718(I717 s) { } }
    interface I719 { }
    class S719 : I719 { public S719(I718 s) { } }
    interface I720 { }
    class S720 : I720 { public S720(I719 s) { } }
    interface I721 { }
    class S721 : I721 { public S721(I720 s) { } }
    interface I722 { }
    class S722 : I722 { public S722(I721 s) { } }
    interface I723 { }
    class S723 : I723 { public S723(I722 s) { } }
    interface I724 { }
    class S724 : I724 { public S724(I723 s) { } }
    interface I725 { }
    class S725 : I725 { public S725(I724 s) { } }
    interface I726 { }
    class S726 : I726 { public S726(I725 s) { } }
    interface I727 { }
    class S727 : I727 { public S727(I726 s) { } }
    interface I728 { }
    class S728 : I728 { public S728(I727 s) { } }
    interface I729 { }
    class S729 : I729 { public S729(I728 s) { } }
    interface I730 { }
    class S730 : I730 { public S730(I729 s) { } }
    interface I731 { }
    class S731 : I731 { public S731(I730 s) { } }
    interface I732 { }
    class S732 : I732 { public S732(I731 s) { } }
    interface I733 { }
    class S733 : I733 { public S733(I732 s) { } }
    interface I734 { }
    class S734 : I734 { public S734(I733 s) { } }
    interface I735 { }
    class S735 : I735 { public S735(I734 s) { } }
    interface I736 { }
    class S736 : I736 { public S736(I735 s) { } }
    interface I737 { }
    class S737 : I737 { public S737(I736 s) { } }
    interface I738 { }
    class S738 : I738 { public S738(I737 s) { } }
    interface I739 { }
    class S739 : I739 { public S739(I738 s) { } }
    interface I740 { }
    class S740 : I740 { public S740(I739 s) { } }
    interface I741 { }
    class S741 : I741 { public S741(I740 s) { } }
    interface I742 { }
    class S742 : I742 { public S742(I741 s) { } }
    interface I743 { }
    class S743 : I743 { public S743(I742 s) { } }
    interface I744 { }
    class S744 : I744 { public S744(I743 s) { } }
    interface I745 { }
    class S745 : I745 { public S745(I744 s) { } }
    interface I746 { }
    class S746 : I746 { public S746(I745 s) { } }
    interface I747 { }
    class S747 : I747 { public S747(I746 s) { } }
    interface I748 { }
    class S748 : I748 { public S748(I747 s) { } }
    interface I749 { }
    class S749 : I749 { public S749(I748 s) { } }
    interface I750 { }
    class S750 : I750 { public S750(I749 s) { } }
    interface I751 { }
    class S751 : I751 { public S751(I750 s) { } }
    interface I752 { }
    class S752 : I752 { public S752(I751 s) { } }
    interface I753 { }
    class S753 : I753 { public S753(I752 s) { } }
    interface I754 { }
    class S754 : I754 { public S754(I753 s) { } }
    interface I755 { }
    class S755 : I755 { public S755(I754 s) { } }
    interface I756 { }
    class S756 : I756 { public S756(I755 s) { } }
    interface I757 { }
    class S757 : I757 { public S757(I756 s) { } }
    interface I758 { }
    class S758 : I758 { public S758(I757 s) { } }
    interface I759 { }
    class S759 : I759 { public S759(I758 s) { } }
    interface I760 { }
    class S760 : I760 { public S760(I759 s) { } }
    interface I761 { }
    class S761 : I761 { public S761(I760 s) { } }
    interface I762 { }
    class S762 : I762 { public S762(I761 s) { } }
    interface I763 { }
    class S763 : I763 { public S763(I762 s) { } }
    interface I764 { }
    class S764 : I764 { public S764(I763 s) { } }
    interface I765 { }
    class S765 : I765 { public S765(I764 s) { } }
    interface I766 { }
    class S766 : I766 { public S766(I765 s) { } }
    interface I767 { }
    class S767 : I767 { public S767(I766 s) { } }
    interface I768 { }
    class S768 : I768 { public S768(I767 s) { } }
    interface I769 { }
    class S769 : I769 { public S769(I768 s) { } }
    interface I770 { }
    class S770 : I770 { public S770(I769 s) { } }
    interface I771 { }
    class S771 : I771 { public S771(I770 s) { } }
    interface I772 { }
    class S772 : I772 { public S772(I771 s) { } }
    interface I773 { }
    class S773 : I773 { public S773(I772 s) { } }
    interface I774 { }
    class S774 : I774 { public S774(I773 s) { } }
    interface I775 { }
    class S775 : I775 { public S775(I774 s) { } }
    interface I776 { }
    class S776 : I776 { public S776(I775 s) { } }
    interface I777 { }
    class S777 : I777 { public S777(I776 s) { } }
    interface I778 { }
    class S778 : I778 { public S778(I777 s) { } }
    interface I779 { }
    class S779 : I779 { public S779(I778 s) { } }
    interface I780 { }
    class S780 : I780 { public S780(I779 s) { } }
    interface I781 { }
    class S781 : I781 { public S781(I780 s) { } }
    interface I782 { }
    class S782 : I782 { public S782(I781 s) { } }
    interface I783 { }
    class S783 : I783 { public S783(I782 s) { } }
    interface I784 { }
    class S784 : I784 { public S784(I783 s) { } }
    interface I785 { }
    class S785 : I785 { public S785(I784 s) { } }
    interface I786 { }
    class S786 : I786 { public S786(I785 s) { } }
    interface I787 { }
    class S787 : I787 { public S787(I786 s) { } }
    interface I788 { }
    class S788 : I788 { public S788(I787 s) { } }
    interface I789 { }
    class S789 : I789 { public S789(I788 s) { } }
    interface I790 { }
    class S790 : I790 { public S790(I789 s) { } }
    interface I791 { }
    class S791 : I791 { public S791(I790 s) { } }
    interface I792 { }
    class S792 : I792 { public S792(I791 s) { } }
    interface I793 { }
    class S793 : I793 { public S793(I792 s) { } }
    interface I794 { }
    class S794 : I794 { public S794(I793 s) { } }
    interface I795 { }
    class S795 : I795 { public S795(I794 s) { } }
    interface I796 { }
    class S796 : I796 { public S796(I795 s) { } }
    interface I797 { }
    class S797 : I797 { public S797(I796 s) { } }
    interface I798 { }
    class S798 : I798 { public S798(I797 s) { } }
    interface I799 { }
    class S799 : I799 { public S799(I798 s) { } }
    interface I800 { }
    class S800 : I800 { public S800(I799 s) { } }
    interface I801 { }
    class S801 : I801 { public S801(I800 s) { } }
    interface I802 { }
    class S802 : I802 { public S802(I801 s) { } }
    interface I803 { }
    class S803 : I803 { public S803(I802 s) { } }
    interface I804 { }
    class S804 : I804 { public S804(I803 s) { } }
    interface I805 { }
    class S805 : I805 { public S805(I804 s) { } }
    interface I806 { }
    class S806 : I806 { public S806(I805 s) { } }
    interface I807 { }
    class S807 : I807 { public S807(I806 s) { } }
    interface I808 { }
    class S808 : I808 { public S808(I807 s) { } }
    interface I809 { }
    class S809 : I809 { public S809(I808 s) { } }
    interface I810 { }
    class S810 : I810 { public S810(I809 s) { } }
    interface I811 { }
    class S811 : I811 { public S811(I810 s) { } }
    interface I812 { }
    class S812 : I812 { public S812(I811 s) { } }
    interface I813 { }
    class S813 : I813 { public S813(I812 s) { } }
    interface I814 { }
    class S814 : I814 { public S814(I813 s) { } }
    interface I815 { }
    class S815 : I815 { public S815(I814 s) { } }
    interface I816 { }
    class S816 : I816 { public S816(I815 s) { } }
    interface I817 { }
    class S817 : I817 { public S817(I816 s) { } }
    interface I818 { }
    class S818 : I818 { public S818(I817 s) { } }
    interface I819 { }
    class S819 : I819 { public S819(I818 s) { } }
    interface I820 { }
    class S820 : I820 { public S820(I819 s) { } }
    interface I821 { }
    class S821 : I821 { public S821(I820 s) { } }
    interface I822 { }
    class S822 : I822 { public S822(I821 s) { } }
    interface I823 { }
    class S823 : I823 { public S823(I822 s) { } }
    interface I824 { }
    class S824 : I824 { public S824(I823 s) { } }
    interface I825 { }
    class S825 : I825 { public S825(I824 s) { } }
    interface I826 { }
    class S826 : I826 { public S826(I825 s) { } }
    interface I827 { }
    class S827 : I827 { public S827(I826 s) { } }
    interface I828 { }
    class S828 : I828 { public S828(I827 s) { } }
    interface I829 { }
    class S829 : I829 { public S829(I828 s) { } }
    interface I830 { }
    class S830 : I830 { public S830(I829 s) { } }
    interface I831 { }
    class S831 : I831 { public S831(I830 s) { } }
    interface I832 { }
    class S832 : I832 { public S832(I831 s) { } }
    interface I833 { }
    class S833 : I833 { public S833(I832 s) { } }
    interface I834 { }
    class S834 : I834 { public S834(I833 s) { } }
    interface I835 { }
    class S835 : I835 { public S835(I834 s) { } }
    interface I836 { }
    class S836 : I836 { public S836(I835 s) { } }
    interface I837 { }
    class S837 : I837 { public S837(I836 s) { } }
    interface I838 { }
    class S838 : I838 { public S838(I837 s) { } }
    interface I839 { }
    class S839 : I839 { public S839(I838 s) { } }
    interface I840 { }
    class S840 : I840 { public S840(I839 s) { } }
    interface I841 { }
    class S841 : I841 { public S841(I840 s) { } }
    interface I842 { }
    class S842 : I842 { public S842(I841 s) { } }
    interface I843 { }
    class S843 : I843 { public S843(I842 s) { } }
    interface I844 { }
    class S844 : I844 { public S844(I843 s) { } }
    interface I845 { }
    class S845 : I845 { public S845(I844 s) { } }
    interface I846 { }
    class S846 : I846 { public S846(I845 s) { } }
    interface I847 { }
    class S847 : I847 { public S847(I846 s) { } }
    interface I848 { }
    class S848 : I848 { public S848(I847 s) { } }
    interface I849 { }
    class S849 : I849 { public S849(I848 s) { } }
    interface I850 { }
    class S850 : I850 { public S850(I849 s) { } }
    interface I851 { }
    class S851 : I851 { public S851(I850 s) { } }
    interface I852 { }
    class S852 : I852 { public S852(I851 s) { } }
    interface I853 { }
    class S853 : I853 { public S853(I852 s) { } }
    interface I854 { }
    class S854 : I854 { public S854(I853 s) { } }
    interface I855 { }
    class S855 : I855 { public S855(I854 s) { } }
    interface I856 { }
    class S856 : I856 { public S856(I855 s) { } }
    interface I857 { }
    class S857 : I857 { public S857(I856 s) { } }
    interface I858 { }
    class S858 : I858 { public S858(I857 s) { } }
    interface I859 { }
    class S859 : I859 { public S859(I858 s) { } }
    interface I860 { }
    class S860 : I860 { public S860(I859 s) { } }
    interface I861 { }
    class S861 : I861 { public S861(I860 s) { } }
    interface I862 { }
    class S862 : I862 { public S862(I861 s) { } }
    interface I863 { }
    class S863 : I863 { public S863(I862 s) { } }
    interface I864 { }
    class S864 : I864 { public S864(I863 s) { } }
    interface I865 { }
    class S865 : I865 { public S865(I864 s) { } }
    interface I866 { }
    class S866 : I866 { public S866(I865 s) { } }
    interface I867 { }
    class S867 : I867 { public S867(I866 s) { } }
    interface I868 { }
    class S868 : I868 { public S868(I867 s) { } }
    interface I869 { }
    class S869 : I869 { public S869(I868 s) { } }
    interface I870 { }
    class S870 : I870 { public S870(I869 s) { } }
    interface I871 { }
    class S871 : I871 { public S871(I870 s) { } }
    interface I872 { }
    class S872 : I872 { public S872(I871 s) { } }
    interface I873 { }
    class S873 : I873 { public S873(I872 s) { } }
    interface I874 { }
    class S874 : I874 { public S874(I873 s) { } }
    interface I875 { }
    class S875 : I875 { public S875(I874 s) { } }
    interface I876 { }
    class S876 : I876 { public S876(I875 s) { } }
    interface I877 { }
    class S877 : I877 { public S877(I876 s) { } }
    interface I878 { }
    class S878 : I878 { public S878(I877 s) { } }
    interface I879 { }
    class S879 : I879 { public S879(I878 s) { } }
    interface I880 { }
    class S880 : I880 { public S880(I879 s) { } }
    interface I881 { }
    class S881 : I881 { public S881(I880 s) { } }
    interface I882 { }
    class S882 : I882 { public S882(I881 s) { } }
    interface I883 { }
    class S883 : I883 { public S883(I882 s) { } }
    interface I884 { }
    class S884 : I884 { public S884(I883 s) { } }
    interface I885 { }
    class S885 : I885 { public S885(I884 s) { } }
    interface I886 { }
    class S886 : I886 { public S886(I885 s) { } }
    interface I887 { }
    class S887 : I887 { public S887(I886 s) { } }
    interface I888 { }
    class S888 : I888 { public S888(I887 s) { } }
    interface I889 { }
    class S889 : I889 { public S889(I888 s) { } }
    interface I890 { }
    class S890 : I890 { public S890(I889 s) { } }
    interface I891 { }
    class S891 : I891 { public S891(I890 s) { } }
    interface I892 { }
    class S892 : I892 { public S892(I891 s) { } }
    interface I893 { }
    class S893 : I893 { public S893(I892 s) { } }
    interface I894 { }
    class S894 : I894 { public S894(I893 s) { } }
    interface I895 { }
    class S895 : I895 { public S895(I894 s) { } }
    interface I896 { }
    class S896 : I896 { public S896(I895 s) { } }
    interface I897 { }
    class S897 : I897 { public S897(I896 s) { } }
    interface I898 { }
    class S898 : I898 { public S898(I897 s) { } }
    interface I899 { }
    class S899 : I899 { public S899(I898 s) { } }
    interface I900 { }
    class S900 : I900 { public S900(I899 s) { } }
    interface I901 { }
    class S901 : I901 { public S901(I900 s) { } }
    interface I902 { }
    class S902 : I902 { public S902(I901 s) { } }
    interface I903 { }
    class S903 : I903 { public S903(I902 s) { } }
    interface I904 { }
    class S904 : I904 { public S904(I903 s) { } }
    interface I905 { }
    class S905 : I905 { public S905(I904 s) { } }
    interface I906 { }
    class S906 : I906 { public S906(I905 s) { } }
    interface I907 { }
    class S907 : I907 { public S907(I906 s) { } }
    interface I908 { }
    class S908 : I908 { public S908(I907 s) { } }
    interface I909 { }
    class S909 : I909 { public S909(I908 s) { } }
    interface I910 { }
    class S910 : I910 { public S910(I909 s) { } }
    interface I911 { }
    class S911 : I911 { public S911(I910 s) { } }
    interface I912 { }
    class S912 : I912 { public S912(I911 s) { } }
    interface I913 { }
    class S913 : I913 { public S913(I912 s) { } }
    interface I914 { }
    class S914 : I914 { public S914(I913 s) { } }
    interface I915 { }
    class S915 : I915 { public S915(I914 s) { } }
    interface I916 { }
    class S916 : I916 { public S916(I915 s) { } }
    interface I917 { }
    class S917 : I917 { public S917(I916 s) { } }
    interface I918 { }
    class S918 : I918 { public S918(I917 s) { } }
    interface I919 { }
    class S919 : I919 { public S919(I918 s) { } }
    interface I920 { }
    class S920 : I920 { public S920(I919 s) { } }
    interface I921 { }
    class S921 : I921 { public S921(I920 s) { } }
    interface I922 { }
    class S922 : I922 { public S922(I921 s) { } }
    interface I923 { }
    class S923 : I923 { public S923(I922 s) { } }
    interface I924 { }
    class S924 : I924 { public S924(I923 s) { } }
    interface I925 { }
    class S925 : I925 { public S925(I924 s) { } }
    interface I926 { }
    class S926 : I926 { public S926(I925 s) { } }
    interface I927 { }
    class S927 : I927 { public S927(I926 s) { } }
    interface I928 { }
    class S928 : I928 { public S928(I927 s) { } }
    interface I929 { }
    class S929 : I929 { public S929(I928 s) { } }
    interface I930 { }
    class S930 : I930 { public S930(I929 s) { } }
    interface I931 { }
    class S931 : I931 { public S931(I930 s) { } }
    interface I932 { }
    class S932 : I932 { public S932(I931 s) { } }
    interface I933 { }
    class S933 : I933 { public S933(I932 s) { } }
    interface I934 { }
    class S934 : I934 { public S934(I933 s) { } }
    interface I935 { }
    class S935 : I935 { public S935(I934 s) { } }
    interface I936 { }
    class S936 : I936 { public S936(I935 s) { } }
    interface I937 { }
    class S937 : I937 { public S937(I936 s) { } }
    interface I938 { }
    class S938 : I938 { public S938(I937 s) { } }
    interface I939 { }
    class S939 : I939 { public S939(I938 s) { } }
    interface I940 { }
    class S940 : I940 { public S940(I939 s) { } }
    interface I941 { }
    class S941 : I941 { public S941(I940 s) { } }
    interface I942 { }
    class S942 : I942 { public S942(I941 s) { } }
    interface I943 { }
    class S943 : I943 { public S943(I942 s) { } }
    interface I944 { }
    class S944 : I944 { public S944(I943 s) { } }
    interface I945 { }
    class S945 : I945 { public S945(I944 s) { } }
    interface I946 { }
    class S946 : I946 { public S946(I945 s) { } }
    interface I947 { }
    class S947 : I947 { public S947(I946 s) { } }
    interface I948 { }
    class S948 : I948 { public S948(I947 s) { } }
    interface I949 { }
    class S949 : I949 { public S949(I948 s) { } }
    interface I950 { }
    class S950 : I950 { public S950(I949 s) { } }
    interface I951 { }
    class S951 : I951 { public S951(I950 s) { } }
    interface I952 { }
    class S952 : I952 { public S952(I951 s) { } }
    interface I953 { }
    class S953 : I953 { public S953(I952 s) { } }
    interface I954 { }
    class S954 : I954 { public S954(I953 s) { } }
    interface I955 { }
    class S955 : I955 { public S955(I954 s) { } }
    interface I956 { }
    class S956 : I956 { public S956(I955 s) { } }
    interface I957 { }
    class S957 : I957 { public S957(I956 s) { } }
    interface I958 { }
    class S958 : I958 { public S958(I957 s) { } }
    interface I959 { }
    class S959 : I959 { public S959(I958 s) { } }
    interface I960 { }
    class S960 : I960 { public S960(I959 s) { } }
    interface I961 { }
    class S961 : I961 { public S961(I960 s) { } }
    interface I962 { }
    class S962 : I962 { public S962(I961 s) { } }
    interface I963 { }
    class S963 : I963 { public S963(I962 s) { } }
    interface I964 { }
    class S964 : I964 { public S964(I963 s) { } }
    interface I965 { }
    class S965 : I965 { public S965(I964 s) { } }
    interface I966 { }
    class S966 : I966 { public S966(I965 s) { } }
    interface I967 { }
    class S967 : I967 { public S967(I966 s) { } }
    interface I968 { }
    class S968 : I968 { public S968(I967 s) { } }
    interface I969 { }
    class S969 : I969 { public S969(I968 s) { } }
    interface I970 { }
    class S970 : I970 { public S970(I969 s) { } }
    interface I971 { }
    class S971 : I971 { public S971(I970 s) { } }
    interface I972 { }
    class S972 : I972 { public S972(I971 s) { } }
    interface I973 { }
    class S973 : I973 { public S973(I972 s) { } }
    interface I974 { }
    class S974 : I974 { public S974(I973 s) { } }
    interface I975 { }
    class S975 : I975 { public S975(I974 s) { } }
    interface I976 { }
    class S976 : I976 { public S976(I975 s) { } }
    interface I977 { }
    class S977 : I977 { public S977(I976 s) { } }
    interface I978 { }
    class S978 : I978 { public S978(I977 s) { } }
    interface I979 { }
    class S979 : I979 { public S979(I978 s) { } }
    interface I980 { }
    class S980 : I980 { public S980(I979 s) { } }
    interface I981 { }
    class S981 : I981 { public S981(I980 s) { } }
    interface I982 { }
    class S982 : I982 { public S982(I981 s) { } }
    interface I983 { }
    class S983 : I983 { public S983(I982 s) { } }
    interface I984 { }
    class S984 : I984 { public S984(I983 s) { } }
    interface I985 { }
    class S985 : I985 { public S985(I984 s) { } }
    interface I986 { }
    class S986 : I986 { public S986(I985 s) { } }
    interface I987 { }
    class S987 : I987 { public S987(I986 s) { } }
    interface I988 { }
    class S988 : I988 { public S988(I987 s) { } }
    interface I989 { }
    class S989 : I989 { public S989(I988 s) { } }
    interface I990 { }
    class S990 : I990 { public S990(I989 s) { } }
    interface I991 { }
    class S991 : I991 { public S991(I990 s) { } }
    interface I992 { }
    class S992 : I992 { public S992(I991 s) { } }
    interface I993 { }
    class S993 : I993 { public S993(I992 s) { } }
    interface I994 { }
    class S994 : I994 { public S994(I993 s) { } }
    interface I995 { }
    class S995 : I995 { public S995(I994 s) { } }
    interface I996 { }
    class S996 : I996 { public S996(I995 s) { } }
    interface I997 { }
    class S997 : I997 { public S997(I996 s) { } }
    interface I998 { }
    class S998 : I998 { public S998(I997 s) { } }
    interface I999 { }
    class S999 : I999 { public S999(I998 s) { } }
    public static class CompilationTestDataProvider
    {
        public static void Register(IServiceCollection p)
        {
            p.AddScoped<I0, S0>();
            p.AddTransient<I0, S0>();
            p.AddTransient<I1, S1>();
            p.AddTransient<I2, S2>();
            p.AddTransient<I3, S3>();
            p.AddTransient<I4, S4>();
            p.AddTransient<I5, S5>();
            p.AddTransient<I6, S6>();
            p.AddTransient<I7, S7>();
            p.AddTransient<I8, S8>();
            p.AddTransient<I9, S9>();
            p.AddTransient<I10, S10>();
            p.AddTransient<I11, S11>();
            p.AddTransient<I12, S12>();
            p.AddTransient<I13, S13>();
            p.AddTransient<I14, S14>();
            p.AddTransient<I15, S15>();
            p.AddTransient<I16, S16>();
            p.AddTransient<I17, S17>();
            p.AddTransient<I18, S18>();
            p.AddTransient<I19, S19>();
            p.AddTransient<I20, S20>();
            p.AddTransient<I21, S21>();
            p.AddTransient<I22, S22>();
            p.AddTransient<I23, S23>();
            p.AddTransient<I24, S24>();
            p.AddTransient<I25, S25>();
            p.AddTransient<I26, S26>();
            p.AddTransient<I27, S27>();
            p.AddTransient<I28, S28>();
            p.AddTransient<I29, S29>();
            p.AddTransient<I30, S30>();
            p.AddTransient<I31, S31>();
            p.AddTransient<I32, S32>();
            p.AddTransient<I33, S33>();
            p.AddTransient<I34, S34>();
            p.AddTransient<I35, S35>();
            p.AddTransient<I36, S36>();
            p.AddTransient<I37, S37>();
            p.AddTransient<I38, S38>();
            p.AddTransient<I39, S39>();
            p.AddTransient<I40, S40>();
            p.AddTransient<I41, S41>();
            p.AddTransient<I42, S42>();
            p.AddTransient<I43, S43>();
            p.AddTransient<I44, S44>();
            p.AddTransient<I45, S45>();
            p.AddTransient<I46, S46>();
            p.AddTransient<I47, S47>();
            p.AddTransient<I48, S48>();
            p.AddTransient<I49, S49>();
            p.AddTransient<I50, S50>();
            p.AddTransient<I51, S51>();
            p.AddTransient<I52, S52>();
            p.AddTransient<I53, S53>();
            p.AddTransient<I54, S54>();
            p.AddTransient<I55, S55>();
            p.AddTransient<I56, S56>();
            p.AddTransient<I57, S57>();
            p.AddTransient<I58, S58>();
            p.AddTransient<I59, S59>();
            p.AddTransient<I60, S60>();
            p.AddTransient<I61, S61>();
            p.AddTransient<I62, S62>();
            p.AddTransient<I63, S63>();
            p.AddTransient<I64, S64>();
            p.AddTransient<I65, S65>();
            p.AddTransient<I66, S66>();
            p.AddTransient<I67, S67>();
            p.AddTransient<I68, S68>();
            p.AddTransient<I69, S69>();
            p.AddTransient<I70, S70>();
            p.AddTransient<I71, S71>();
            p.AddTransient<I72, S72>();
            p.AddTransient<I73, S73>();
            p.AddTransient<I74, S74>();
            p.AddTransient<I75, S75>();
            p.AddTransient<I76, S76>();
            p.AddTransient<I77, S77>();
            p.AddTransient<I78, S78>();
            p.AddTransient<I79, S79>();
            p.AddTransient<I80, S80>();
            p.AddTransient<I81, S81>();
            p.AddTransient<I82, S82>();
            p.AddTransient<I83, S83>();
            p.AddTransient<I84, S84>();
            p.AddTransient<I85, S85>();
            p.AddTransient<I86, S86>();
            p.AddTransient<I87, S87>();
            p.AddTransient<I88, S88>();
            p.AddTransient<I89, S89>();
            p.AddTransient<I90, S90>();
            p.AddTransient<I91, S91>();
            p.AddTransient<I92, S92>();
            p.AddTransient<I93, S93>();
            p.AddTransient<I94, S94>();
            p.AddTransient<I95, S95>();
            p.AddTransient<I96, S96>();
            p.AddTransient<I97, S97>();
            p.AddTransient<I98, S98>();
            p.AddTransient<I99, S99>();
            p.AddTransient<I100, S100>();
            p.AddTransient<I101, S101>();
            p.AddTransient<I102, S102>();
            p.AddTransient<I103, S103>();
            p.AddTransient<I104, S104>();
            p.AddTransient<I105, S105>();
            p.AddTransient<I106, S106>();
            p.AddTransient<I107, S107>();
            p.AddTransient<I108, S108>();
            p.AddTransient<I109, S109>();
            p.AddTransient<I110, S110>();
            p.AddTransient<I111, S111>();
            p.AddTransient<I112, S112>();
            p.AddTransient<I113, S113>();
            p.AddTransient<I114, S114>();
            p.AddTransient<I115, S115>();
            p.AddTransient<I116, S116>();
            p.AddTransient<I117, S117>();
            p.AddTransient<I118, S118>();
            p.AddTransient<I119, S119>();
            p.AddTransient<I120, S120>();
            p.AddTransient<I121, S121>();
            p.AddTransient<I122, S122>();
            p.AddTransient<I123, S123>();
            p.AddTransient<I124, S124>();
            p.AddTransient<I125, S125>();
            p.AddTransient<I126, S126>();
            p.AddTransient<I127, S127>();
            p.AddTransient<I128, S128>();
            p.AddTransient<I129, S129>();
            p.AddTransient<I130, S130>();
            p.AddTransient<I131, S131>();
            p.AddTransient<I132, S132>();
            p.AddTransient<I133, S133>();
            p.AddTransient<I134, S134>();
            p.AddTransient<I135, S135>();
            p.AddTransient<I136, S136>();
            p.AddTransient<I137, S137>();
            p.AddTransient<I138, S138>();
            p.AddTransient<I139, S139>();
            p.AddTransient<I140, S140>();
            p.AddTransient<I141, S141>();
            p.AddTransient<I142, S142>();
            p.AddTransient<I143, S143>();
            p.AddTransient<I144, S144>();
            p.AddTransient<I145, S145>();
            p.AddTransient<I146, S146>();
            p.AddTransient<I147, S147>();
            p.AddTransient<I148, S148>();
            p.AddTransient<I149, S149>();
            p.AddTransient<I150, S150>();
            p.AddTransient<I151, S151>();
            p.AddTransient<I152, S152>();
            p.AddTransient<I153, S153>();
            p.AddTransient<I154, S154>();
            p.AddTransient<I155, S155>();
            p.AddTransient<I156, S156>();
            p.AddTransient<I157, S157>();
            p.AddTransient<I158, S158>();
            p.AddTransient<I159, S159>();
            p.AddTransient<I160, S160>();
            p.AddTransient<I161, S161>();
            p.AddTransient<I162, S162>();
            p.AddTransient<I163, S163>();
            p.AddTransient<I164, S164>();
            p.AddTransient<I165, S165>();
            p.AddTransient<I166, S166>();
            p.AddTransient<I167, S167>();
            p.AddTransient<I168, S168>();
            p.AddTransient<I169, S169>();
            p.AddTransient<I170, S170>();
            p.AddTransient<I171, S171>();
            p.AddTransient<I172, S172>();
            p.AddTransient<I173, S173>();
            p.AddTransient<I174, S174>();
            p.AddTransient<I175, S175>();
            p.AddTransient<I176, S176>();
            p.AddTransient<I177, S177>();
            p.AddTransient<I178, S178>();
            p.AddTransient<I179, S179>();
            p.AddTransient<I180, S180>();
            p.AddTransient<I181, S181>();
            p.AddTransient<I182, S182>();
            p.AddTransient<I183, S183>();
            p.AddTransient<I184, S184>();
            p.AddTransient<I185, S185>();
            p.AddTransient<I186, S186>();
            p.AddTransient<I187, S187>();
            p.AddTransient<I188, S188>();
            p.AddTransient<I189, S189>();
            p.AddTransient<I190, S190>();
            p.AddTransient<I191, S191>();
            p.AddTransient<I192, S192>();
            p.AddTransient<I193, S193>();
            p.AddTransient<I194, S194>();
            p.AddTransient<I195, S195>();
            p.AddTransient<I196, S196>();
            p.AddTransient<I197, S197>();
            p.AddTransient<I198, S198>();
            p.AddTransient<I199, S199>();
            p.AddTransient<I200, S200>();
            p.AddTransient<I201, S201>();
            p.AddTransient<I202, S202>();
            p.AddTransient<I203, S203>();
            p.AddTransient<I204, S204>();
            p.AddTransient<I205, S205>();
            p.AddTransient<I206, S206>();
            p.AddTransient<I207, S207>();
            p.AddTransient<I208, S208>();
            p.AddTransient<I209, S209>();
            p.AddTransient<I210, S210>();
            p.AddTransient<I211, S211>();
            p.AddTransient<I212, S212>();
            p.AddTransient<I213, S213>();
            p.AddTransient<I214, S214>();
            p.AddTransient<I215, S215>();
            p.AddTransient<I216, S216>();
            p.AddTransient<I217, S217>();
            p.AddTransient<I218, S218>();
            p.AddTransient<I219, S219>();
            p.AddTransient<I220, S220>();
            p.AddTransient<I221, S221>();
            p.AddTransient<I222, S222>();
            p.AddTransient<I223, S223>();
            p.AddTransient<I224, S224>();
            p.AddTransient<I225, S225>();
            p.AddTransient<I226, S226>();
            p.AddTransient<I227, S227>();
            p.AddTransient<I228, S228>();
            p.AddTransient<I229, S229>();
            p.AddTransient<I230, S230>();
            p.AddTransient<I231, S231>();
            p.AddTransient<I232, S232>();
            p.AddTransient<I233, S233>();
            p.AddTransient<I234, S234>();
            p.AddTransient<I235, S235>();
            p.AddTransient<I236, S236>();
            p.AddTransient<I237, S237>();
            p.AddTransient<I238, S238>();
            p.AddTransient<I239, S239>();
            p.AddTransient<I240, S240>();
            p.AddTransient<I241, S241>();
            p.AddTransient<I242, S242>();
            p.AddTransient<I243, S243>();
            p.AddTransient<I244, S244>();
            p.AddTransient<I245, S245>();
            p.AddTransient<I246, S246>();
            p.AddTransient<I247, S247>();
            p.AddTransient<I248, S248>();
            p.AddTransient<I249, S249>();
            p.AddTransient<I250, S250>();
            p.AddTransient<I251, S251>();
            p.AddTransient<I252, S252>();
            p.AddTransient<I253, S253>();
            p.AddTransient<I254, S254>();
            p.AddTransient<I255, S255>();
            p.AddTransient<I256, S256>();
            p.AddTransient<I257, S257>();
            p.AddTransient<I258, S258>();
            p.AddTransient<I259, S259>();
            p.AddTransient<I260, S260>();
            p.AddTransient<I261, S261>();
            p.AddTransient<I262, S262>();
            p.AddTransient<I263, S263>();
            p.AddTransient<I264, S264>();
            p.AddTransient<I265, S265>();
            p.AddTransient<I266, S266>();
            p.AddTransient<I267, S267>();
            p.AddTransient<I268, S268>();
            p.AddTransient<I269, S269>();
            p.AddTransient<I270, S270>();
            p.AddTransient<I271, S271>();
            p.AddTransient<I272, S272>();
            p.AddTransient<I273, S273>();
            p.AddTransient<I274, S274>();
            p.AddTransient<I275, S275>();
            p.AddTransient<I276, S276>();
            p.AddTransient<I277, S277>();
            p.AddTransient<I278, S278>();
            p.AddTransient<I279, S279>();
            p.AddTransient<I280, S280>();
            p.AddTransient<I281, S281>();
            p.AddTransient<I282, S282>();
            p.AddTransient<I283, S283>();
            p.AddTransient<I284, S284>();
            p.AddTransient<I285, S285>();
            p.AddTransient<I286, S286>();
            p.AddTransient<I287, S287>();
            p.AddTransient<I288, S288>();
            p.AddTransient<I289, S289>();
            p.AddTransient<I290, S290>();
            p.AddTransient<I291, S291>();
            p.AddTransient<I292, S292>();
            p.AddTransient<I293, S293>();
            p.AddTransient<I294, S294>();
            p.AddTransient<I295, S295>();
            p.AddTransient<I296, S296>();
            p.AddTransient<I297, S297>();
            p.AddTransient<I298, S298>();
            p.AddTransient<I299, S299>();
            p.AddTransient<I300, S300>();
            p.AddTransient<I301, S301>();
            p.AddTransient<I302, S302>();
            p.AddTransient<I303, S303>();
            p.AddTransient<I304, S304>();
            p.AddTransient<I305, S305>();
            p.AddTransient<I306, S306>();
            p.AddTransient<I307, S307>();
            p.AddTransient<I308, S308>();
            p.AddTransient<I309, S309>();
            p.AddTransient<I310, S310>();
            p.AddTransient<I311, S311>();
            p.AddTransient<I312, S312>();
            p.AddTransient<I313, S313>();
            p.AddTransient<I314, S314>();
            p.AddTransient<I315, S315>();
            p.AddTransient<I316, S316>();
            p.AddTransient<I317, S317>();
            p.AddTransient<I318, S318>();
            p.AddTransient<I319, S319>();
            p.AddTransient<I320, S320>();
            p.AddTransient<I321, S321>();
            p.AddTransient<I322, S322>();
            p.AddTransient<I323, S323>();
            p.AddTransient<I324, S324>();
            p.AddTransient<I325, S325>();
            p.AddTransient<I326, S326>();
            p.AddTransient<I327, S327>();
            p.AddTransient<I328, S328>();
            p.AddTransient<I329, S329>();
            p.AddTransient<I330, S330>();
            p.AddTransient<I331, S331>();
            p.AddTransient<I332, S332>();
            p.AddScoped<I333, S333>();
            p.AddScoped<I334, S334>();
            p.AddScoped<I335, S335>();
            p.AddScoped<I336, S336>();
            p.AddScoped<I337, S337>();
            p.AddScoped<I338, S338>();
            p.AddScoped<I339, S339>();
            p.AddScoped<I340, S340>();
            p.AddScoped<I341, S341>();
            p.AddScoped<I342, S342>();
            p.AddScoped<I343, S343>();
            p.AddScoped<I344, S344>();
            p.AddScoped<I345, S345>();
            p.AddScoped<I346, S346>();
            p.AddScoped<I347, S347>();
            p.AddScoped<I348, S348>();
            p.AddScoped<I349, S349>();
            p.AddScoped<I350, S350>();
            p.AddScoped<I351, S351>();
            p.AddScoped<I352, S352>();
            p.AddScoped<I353, S353>();
            p.AddScoped<I354, S354>();
            p.AddScoped<I355, S355>();
            p.AddScoped<I356, S356>();
            p.AddScoped<I357, S357>();
            p.AddScoped<I358, S358>();
            p.AddScoped<I359, S359>();
            p.AddScoped<I360, S360>();
            p.AddScoped<I361, S361>();
            p.AddScoped<I362, S362>();
            p.AddScoped<I363, S363>();
            p.AddScoped<I364, S364>();
            p.AddScoped<I365, S365>();
            p.AddScoped<I366, S366>();
            p.AddScoped<I367, S367>();
            p.AddScoped<I368, S368>();
            p.AddScoped<I369, S369>();
            p.AddScoped<I370, S370>();
            p.AddScoped<I371, S371>();
            p.AddScoped<I372, S372>();
            p.AddScoped<I373, S373>();
            p.AddScoped<I374, S374>();
            p.AddScoped<I375, S375>();
            p.AddScoped<I376, S376>();
            p.AddScoped<I377, S377>();
            p.AddScoped<I378, S378>();
            p.AddScoped<I379, S379>();
            p.AddScoped<I380, S380>();
            p.AddScoped<I381, S381>();
            p.AddScoped<I382, S382>();
            p.AddScoped<I383, S383>();
            p.AddScoped<I384, S384>();
            p.AddScoped<I385, S385>();
            p.AddScoped<I386, S386>();
            p.AddScoped<I387, S387>();
            p.AddScoped<I388, S388>();
            p.AddScoped<I389, S389>();
            p.AddScoped<I390, S390>();
            p.AddScoped<I391, S391>();
            p.AddScoped<I392, S392>();
            p.AddScoped<I393, S393>();
            p.AddScoped<I394, S394>();
            p.AddScoped<I395, S395>();
            p.AddScoped<I396, S396>();
            p.AddScoped<I397, S397>();
            p.AddScoped<I398, S398>();
            p.AddScoped<I399, S399>();
            p.AddScoped<I400, S400>();
            p.AddScoped<I401, S401>();
            p.AddScoped<I402, S402>();
            p.AddScoped<I403, S403>();
            p.AddScoped<I404, S404>();
            p.AddScoped<I405, S405>();
            p.AddScoped<I406, S406>();
            p.AddScoped<I407, S407>();
            p.AddScoped<I408, S408>();
            p.AddScoped<I409, S409>();
            p.AddScoped<I410, S410>();
            p.AddScoped<I411, S411>();
            p.AddScoped<I412, S412>();
            p.AddScoped<I413, S413>();
            p.AddScoped<I414, S414>();
            p.AddScoped<I415, S415>();
            p.AddScoped<I416, S416>();
            p.AddScoped<I417, S417>();
            p.AddScoped<I418, S418>();
            p.AddScoped<I419, S419>();
            p.AddScoped<I420, S420>();
            p.AddScoped<I421, S421>();
            p.AddScoped<I422, S422>();
            p.AddScoped<I423, S423>();
            p.AddScoped<I424, S424>();
            p.AddScoped<I425, S425>();
            p.AddScoped<I426, S426>();
            p.AddScoped<I427, S427>();
            p.AddScoped<I428, S428>();
            p.AddScoped<I429, S429>();
            p.AddScoped<I430, S430>();
            p.AddScoped<I431, S431>();
            p.AddScoped<I432, S432>();
            p.AddScoped<I433, S433>();
            p.AddScoped<I434, S434>();
            p.AddScoped<I435, S435>();
            p.AddScoped<I436, S436>();
            p.AddScoped<I437, S437>();
            p.AddScoped<I438, S438>();
            p.AddScoped<I439, S439>();
            p.AddScoped<I440, S440>();
            p.AddScoped<I441, S441>();
            p.AddScoped<I442, S442>();
            p.AddScoped<I443, S443>();
            p.AddScoped<I444, S444>();
            p.AddScoped<I445, S445>();
            p.AddScoped<I446, S446>();
            p.AddScoped<I447, S447>();
            p.AddScoped<I448, S448>();
            p.AddScoped<I449, S449>();
            p.AddScoped<I450, S450>();
            p.AddScoped<I451, S451>();
            p.AddScoped<I452, S452>();
            p.AddScoped<I453, S453>();
            p.AddScoped<I454, S454>();
            p.AddScoped<I455, S455>();
            p.AddScoped<I456, S456>();
            p.AddScoped<I457, S457>();
            p.AddScoped<I458, S458>();
            p.AddScoped<I459, S459>();
            p.AddScoped<I460, S460>();
            p.AddScoped<I461, S461>();
            p.AddScoped<I462, S462>();
            p.AddScoped<I463, S463>();
            p.AddScoped<I464, S464>();
            p.AddScoped<I465, S465>();
            p.AddScoped<I466, S466>();
            p.AddScoped<I467, S467>();
            p.AddScoped<I468, S468>();
            p.AddScoped<I469, S469>();
            p.AddScoped<I470, S470>();
            p.AddScoped<I471, S471>();
            p.AddScoped<I472, S472>();
            p.AddScoped<I473, S473>();
            p.AddScoped<I474, S474>();
            p.AddScoped<I475, S475>();
            p.AddScoped<I476, S476>();
            p.AddScoped<I477, S477>();
            p.AddScoped<I478, S478>();
            p.AddScoped<I479, S479>();
            p.AddScoped<I480, S480>();
            p.AddScoped<I481, S481>();
            p.AddScoped<I482, S482>();
            p.AddScoped<I483, S483>();
            p.AddScoped<I484, S484>();
            p.AddScoped<I485, S485>();
            p.AddScoped<I486, S486>();
            p.AddScoped<I487, S487>();
            p.AddScoped<I488, S488>();
            p.AddScoped<I489, S489>();
            p.AddScoped<I490, S490>();
            p.AddScoped<I491, S491>();
            p.AddScoped<I492, S492>();
            p.AddScoped<I493, S493>();
            p.AddScoped<I494, S494>();
            p.AddScoped<I495, S495>();
            p.AddScoped<I496, S496>();
            p.AddScoped<I497, S497>();
            p.AddScoped<I498, S498>();
            p.AddScoped<I499, S499>();
            p.AddScoped<I500, S500>();
            p.AddScoped<I501, S501>();
            p.AddScoped<I502, S502>();
            p.AddScoped<I503, S503>();
            p.AddScoped<I504, S504>();
            p.AddScoped<I505, S505>();
            p.AddScoped<I506, S506>();
            p.AddScoped<I507, S507>();
            p.AddScoped<I508, S508>();
            p.AddScoped<I509, S509>();
            p.AddScoped<I510, S510>();
            p.AddScoped<I511, S511>();
            p.AddScoped<I512, S512>();
            p.AddScoped<I513, S513>();
            p.AddScoped<I514, S514>();
            p.AddScoped<I515, S515>();
            p.AddScoped<I516, S516>();
            p.AddScoped<I517, S517>();
            p.AddScoped<I518, S518>();
            p.AddScoped<I519, S519>();
            p.AddScoped<I520, S520>();
            p.AddScoped<I521, S521>();
            p.AddScoped<I522, S522>();
            p.AddScoped<I523, S523>();
            p.AddScoped<I524, S524>();
            p.AddScoped<I525, S525>();
            p.AddScoped<I526, S526>();
            p.AddScoped<I527, S527>();
            p.AddScoped<I528, S528>();
            p.AddScoped<I529, S529>();
            p.AddScoped<I530, S530>();
            p.AddScoped<I531, S531>();
            p.AddScoped<I532, S532>();
            p.AddScoped<I533, S533>();
            p.AddScoped<I534, S534>();
            p.AddScoped<I535, S535>();
            p.AddScoped<I536, S536>();
            p.AddScoped<I537, S537>();
            p.AddScoped<I538, S538>();
            p.AddScoped<I539, S539>();
            p.AddScoped<I540, S540>();
            p.AddScoped<I541, S541>();
            p.AddScoped<I542, S542>();
            p.AddScoped<I543, S543>();
            p.AddScoped<I544, S544>();
            p.AddScoped<I545, S545>();
            p.AddScoped<I546, S546>();
            p.AddScoped<I547, S547>();
            p.AddScoped<I548, S548>();
            p.AddScoped<I549, S549>();
            p.AddScoped<I550, S550>();
            p.AddScoped<I551, S551>();
            p.AddScoped<I552, S552>();
            p.AddScoped<I553, S553>();
            p.AddScoped<I554, S554>();
            p.AddScoped<I555, S555>();
            p.AddScoped<I556, S556>();
            p.AddScoped<I557, S557>();
            p.AddScoped<I558, S558>();
            p.AddScoped<I559, S559>();
            p.AddScoped<I560, S560>();
            p.AddScoped<I561, S561>();
            p.AddScoped<I562, S562>();
            p.AddScoped<I563, S563>();
            p.AddScoped<I564, S564>();
            p.AddScoped<I565, S565>();
            p.AddScoped<I566, S566>();
            p.AddScoped<I567, S567>();
            p.AddScoped<I568, S568>();
            p.AddScoped<I569, S569>();
            p.AddScoped<I570, S570>();
            p.AddScoped<I571, S571>();
            p.AddScoped<I572, S572>();
            p.AddScoped<I573, S573>();
            p.AddScoped<I574, S574>();
            p.AddScoped<I575, S575>();
            p.AddScoped<I576, S576>();
            p.AddScoped<I577, S577>();
            p.AddScoped<I578, S578>();
            p.AddScoped<I579, S579>();
            p.AddScoped<I580, S580>();
            p.AddScoped<I581, S581>();
            p.AddScoped<I582, S582>();
            p.AddScoped<I583, S583>();
            p.AddScoped<I584, S584>();
            p.AddScoped<I585, S585>();
            p.AddScoped<I586, S586>();
            p.AddScoped<I587, S587>();
            p.AddScoped<I588, S588>();
            p.AddScoped<I589, S589>();
            p.AddScoped<I590, S590>();
            p.AddScoped<I591, S591>();
            p.AddScoped<I592, S592>();
            p.AddScoped<I593, S593>();
            p.AddScoped<I594, S594>();
            p.AddScoped<I595, S595>();
            p.AddScoped<I596, S596>();
            p.AddScoped<I597, S597>();
            p.AddScoped<I598, S598>();
            p.AddScoped<I599, S599>();
            p.AddScoped<I600, S600>();
            p.AddScoped<I601, S601>();
            p.AddScoped<I602, S602>();
            p.AddScoped<I603, S603>();
            p.AddScoped<I604, S604>();
            p.AddScoped<I605, S605>();
            p.AddScoped<I606, S606>();
            p.AddScoped<I607, S607>();
            p.AddScoped<I608, S608>();
            p.AddScoped<I609, S609>();
            p.AddScoped<I610, S610>();
            p.AddScoped<I611, S611>();
            p.AddScoped<I612, S612>();
            p.AddScoped<I613, S613>();
            p.AddScoped<I614, S614>();
            p.AddScoped<I615, S615>();
            p.AddScoped<I616, S616>();
            p.AddScoped<I617, S617>();
            p.AddScoped<I618, S618>();
            p.AddScoped<I619, S619>();
            p.AddScoped<I620, S620>();
            p.AddScoped<I621, S621>();
            p.AddScoped<I622, S622>();
            p.AddScoped<I623, S623>();
            p.AddScoped<I624, S624>();
            p.AddScoped<I625, S625>();
            p.AddScoped<I626, S626>();
            p.AddScoped<I627, S627>();
            p.AddScoped<I628, S628>();
            p.AddScoped<I629, S629>();
            p.AddScoped<I630, S630>();
            p.AddScoped<I631, S631>();
            p.AddScoped<I632, S632>();
            p.AddScoped<I633, S633>();
            p.AddScoped<I634, S634>();
            p.AddScoped<I635, S635>();
            p.AddScoped<I636, S636>();
            p.AddScoped<I637, S637>();
            p.AddScoped<I638, S638>();
            p.AddScoped<I639, S639>();
            p.AddScoped<I640, S640>();
            p.AddScoped<I641, S641>();
            p.AddScoped<I642, S642>();
            p.AddScoped<I643, S643>();
            p.AddScoped<I644, S644>();
            p.AddScoped<I645, S645>();
            p.AddScoped<I646, S646>();
            p.AddScoped<I647, S647>();
            p.AddScoped<I648, S648>();
            p.AddScoped<I649, S649>();
            p.AddScoped<I650, S650>();
            p.AddScoped<I651, S651>();
            p.AddScoped<I652, S652>();
            p.AddScoped<I653, S653>();
            p.AddScoped<I654, S654>();
            p.AddScoped<I655, S655>();
            p.AddScoped<I656, S656>();
            p.AddScoped<I657, S657>();
            p.AddScoped<I658, S658>();
            p.AddScoped<I659, S659>();
            p.AddScoped<I660, S660>();
            p.AddScoped<I661, S661>();
            p.AddScoped<I662, S662>();
            p.AddScoped<I663, S663>();
            p.AddScoped<I664, S664>();
            p.AddSingleton<I665, S665>();
            p.AddSingleton<I666, S666>();
            p.AddSingleton<I667, S667>();
            p.AddSingleton<I668, S668>();
            p.AddSingleton<I669, S669>();
            p.AddSingleton<I670, S670>();
            p.AddSingleton<I671, S671>();
            p.AddSingleton<I672, S672>();
            p.AddSingleton<I673, S673>();
            p.AddSingleton<I674, S674>();
            p.AddSingleton<I675, S675>();
            p.AddSingleton<I676, S676>();
            p.AddSingleton<I677, S677>();
            p.AddSingleton<I678, S678>();
            p.AddSingleton<I679, S679>();
            p.AddSingleton<I680, S680>();
            p.AddSingleton<I681, S681>();
            p.AddSingleton<I682, S682>();
            p.AddSingleton<I683, S683>();
            p.AddSingleton<I684, S684>();
            p.AddSingleton<I685, S685>();
            p.AddSingleton<I686, S686>();
            p.AddSingleton<I687, S687>();
            p.AddSingleton<I688, S688>();
            p.AddSingleton<I689, S689>();
            p.AddSingleton<I690, S690>();
            p.AddSingleton<I691, S691>();
            p.AddSingleton<I692, S692>();
            p.AddSingleton<I693, S693>();
            p.AddSingleton<I694, S694>();
            p.AddSingleton<I695, S695>();
            p.AddSingleton<I696, S696>();
            p.AddSingleton<I697, S697>();
            p.AddSingleton<I698, S698>();
            p.AddSingleton<I699, S699>();
            p.AddSingleton<I700, S700>();
            p.AddSingleton<I701, S701>();
            p.AddSingleton<I702, S702>();
            p.AddSingleton<I703, S703>();
            p.AddSingleton<I704, S704>();
            p.AddSingleton<I705, S705>();
            p.AddSingleton<I706, S706>();
            p.AddSingleton<I707, S707>();
            p.AddSingleton<I708, S708>();
            p.AddSingleton<I709, S709>();
            p.AddSingleton<I710, S710>();
            p.AddSingleton<I711, S711>();
            p.AddSingleton<I712, S712>();
            p.AddSingleton<I713, S713>();
            p.AddSingleton<I714, S714>();
            p.AddSingleton<I715, S715>();
            p.AddSingleton<I716, S716>();
            p.AddSingleton<I717, S717>();
            p.AddSingleton<I718, S718>();
            p.AddSingleton<I719, S719>();
            p.AddSingleton<I720, S720>();
            p.AddSingleton<I721, S721>();
            p.AddSingleton<I722, S722>();
            p.AddSingleton<I723, S723>();
            p.AddSingleton<I724, S724>();
            p.AddSingleton<I725, S725>();
            p.AddSingleton<I726, S726>();
            p.AddSingleton<I727, S727>();
            p.AddSingleton<I728, S728>();
            p.AddSingleton<I729, S729>();
            p.AddSingleton<I730, S730>();
            p.AddSingleton<I731, S731>();
            p.AddSingleton<I732, S732>();
            p.AddSingleton<I733, S733>();
            p.AddSingleton<I734, S734>();
            p.AddSingleton<I735, S735>();
            p.AddSingleton<I736, S736>();
            p.AddSingleton<I737, S737>();
            p.AddSingleton<I738, S738>();
            p.AddSingleton<I739, S739>();
            p.AddSingleton<I740, S740>();
            p.AddSingleton<I741, S741>();
            p.AddSingleton<I742, S742>();
            p.AddSingleton<I743, S743>();
            p.AddSingleton<I744, S744>();
            p.AddSingleton<I745, S745>();
            p.AddSingleton<I746, S746>();
            p.AddSingleton<I747, S747>();
            p.AddSingleton<I748, S748>();
            p.AddSingleton<I749, S749>();
            p.AddSingleton<I750, S750>();
            p.AddSingleton<I751, S751>();
            p.AddSingleton<I752, S752>();
            p.AddSingleton<I753, S753>();
            p.AddSingleton<I754, S754>();
            p.AddSingleton<I755, S755>();
            p.AddSingleton<I756, S756>();
            p.AddSingleton<I757, S757>();
            p.AddSingleton<I758, S758>();
            p.AddSingleton<I759, S759>();
            p.AddSingleton<I760, S760>();
            p.AddSingleton<I761, S761>();
            p.AddSingleton<I762, S762>();
            p.AddSingleton<I763, S763>();
            p.AddSingleton<I764, S764>();
            p.AddSingleton<I765, S765>();
            p.AddSingleton<I766, S766>();
            p.AddSingleton<I767, S767>();
            p.AddSingleton<I768, S768>();
            p.AddSingleton<I769, S769>();
            p.AddSingleton<I770, S770>();
            p.AddSingleton<I771, S771>();
            p.AddSingleton<I772, S772>();
            p.AddSingleton<I773, S773>();
            p.AddSingleton<I774, S774>();
            p.AddSingleton<I775, S775>();
            p.AddSingleton<I776, S776>();
            p.AddSingleton<I777, S777>();
            p.AddSingleton<I778, S778>();
            p.AddSingleton<I779, S779>();
            p.AddSingleton<I780, S780>();
            p.AddSingleton<I781, S781>();
            p.AddSingleton<I782, S782>();
            p.AddSingleton<I783, S783>();
            p.AddSingleton<I784, S784>();
            p.AddSingleton<I785, S785>();
            p.AddSingleton<I786, S786>();
            p.AddSingleton<I787, S787>();
            p.AddSingleton<I788, S788>();
            p.AddSingleton<I789, S789>();
            p.AddSingleton<I790, S790>();
            p.AddSingleton<I791, S791>();
            p.AddSingleton<I792, S792>();
            p.AddSingleton<I793, S793>();
            p.AddSingleton<I794, S794>();
            p.AddSingleton<I795, S795>();
            p.AddSingleton<I796, S796>();
            p.AddSingleton<I797, S797>();
            p.AddSingleton<I798, S798>();
            p.AddSingleton<I799, S799>();
            p.AddSingleton<I800, S800>();
            p.AddSingleton<I801, S801>();
            p.AddSingleton<I802, S802>();
            p.AddSingleton<I803, S803>();
            p.AddSingleton<I804, S804>();
            p.AddSingleton<I805, S805>();
            p.AddSingleton<I806, S806>();
            p.AddSingleton<I807, S807>();
            p.AddSingleton<I808, S808>();
            p.AddSingleton<I809, S809>();
            p.AddSingleton<I810, S810>();
            p.AddSingleton<I811, S811>();
            p.AddSingleton<I812, S812>();
            p.AddSingleton<I813, S813>();
            p.AddSingleton<I814, S814>();
            p.AddSingleton<I815, S815>();
            p.AddSingleton<I816, S816>();
            p.AddSingleton<I817, S817>();
            p.AddSingleton<I818, S818>();
            p.AddSingleton<I819, S819>();
            p.AddSingleton<I820, S820>();
            p.AddSingleton<I821, S821>();
            p.AddSingleton<I822, S822>();
            p.AddSingleton<I823, S823>();
            p.AddSingleton<I824, S824>();
            p.AddSingleton<I825, S825>();
            p.AddSingleton<I826, S826>();
            p.AddSingleton<I827, S827>();
            p.AddSingleton<I828, S828>();
            p.AddSingleton<I829, S829>();
            p.AddSingleton<I830, S830>();
            p.AddSingleton<I831, S831>();
            p.AddSingleton<I832, S832>();
            p.AddSingleton<I833, S833>();
            p.AddSingleton<I834, S834>();
            p.AddSingleton<I835, S835>();
            p.AddSingleton<I836, S836>();
            p.AddSingleton<I837, S837>();
            p.AddSingleton<I838, S838>();
            p.AddSingleton<I839, S839>();
            p.AddSingleton<I840, S840>();
            p.AddSingleton<I841, S841>();
            p.AddSingleton<I842, S842>();
            p.AddSingleton<I843, S843>();
            p.AddSingleton<I844, S844>();
            p.AddSingleton<I845, S845>();
            p.AddSingleton<I846, S846>();
            p.AddSingleton<I847, S847>();
            p.AddSingleton<I848, S848>();
            p.AddSingleton<I849, S849>();
            p.AddSingleton<I850, S850>();
            p.AddSingleton<I851, S851>();
            p.AddSingleton<I852, S852>();
            p.AddSingleton<I853, S853>();
            p.AddSingleton<I854, S854>();
            p.AddSingleton<I855, S855>();
            p.AddSingleton<I856, S856>();
            p.AddSingleton<I857, S857>();
            p.AddSingleton<I858, S858>();
            p.AddSingleton<I859, S859>();
            p.AddSingleton<I860, S860>();
            p.AddSingleton<I861, S861>();
            p.AddSingleton<I862, S862>();
            p.AddSingleton<I863, S863>();
            p.AddSingleton<I864, S864>();
            p.AddSingleton<I865, S865>();
            p.AddSingleton<I866, S866>();
            p.AddSingleton<I867, S867>();
            p.AddSingleton<I868, S868>();
            p.AddSingleton<I869, S869>();
            p.AddSingleton<I870, S870>();
            p.AddSingleton<I871, S871>();
            p.AddSingleton<I872, S872>();
            p.AddSingleton<I873, S873>();
            p.AddSingleton<I874, S874>();
            p.AddSingleton<I875, S875>();
            p.AddSingleton<I876, S876>();
            p.AddSingleton<I877, S877>();
            p.AddSingleton<I878, S878>();
            p.AddSingleton<I879, S879>();
            p.AddSingleton<I880, S880>();
            p.AddSingleton<I881, S881>();
            p.AddSingleton<I882, S882>();
            p.AddSingleton<I883, S883>();
            p.AddSingleton<I884, S884>();
            p.AddSingleton<I885, S885>();
            p.AddSingleton<I886, S886>();
            p.AddSingleton<I887, S887>();
            p.AddSingleton<I888, S888>();
            p.AddSingleton<I889, S889>();
            p.AddSingleton<I890, S890>();
            p.AddSingleton<I891, S891>();
            p.AddSingleton<I892, S892>();
            p.AddSingleton<I893, S893>();
            p.AddSingleton<I894, S894>();
            p.AddSingleton<I895, S895>();
            p.AddSingleton<I896, S896>();
            p.AddSingleton<I897, S897>();
            p.AddSingleton<I898, S898>();
            p.AddSingleton<I899, S899>();
            p.AddSingleton<I900, S900>();
            p.AddSingleton<I901, S901>();
            p.AddSingleton<I902, S902>();
            p.AddSingleton<I903, S903>();
            p.AddSingleton<I904, S904>();
            p.AddSingleton<I905, S905>();
            p.AddSingleton<I906, S906>();
            p.AddSingleton<I907, S907>();
            p.AddSingleton<I908, S908>();
            p.AddSingleton<I909, S909>();
            p.AddSingleton<I910, S910>();
            p.AddSingleton<I911, S911>();
            p.AddSingleton<I912, S912>();
            p.AddSingleton<I913, S913>();
            p.AddSingleton<I914, S914>();
            p.AddSingleton<I915, S915>();
            p.AddSingleton<I916, S916>();
            p.AddSingleton<I917, S917>();
            p.AddSingleton<I918, S918>();
            p.AddSingleton<I919, S919>();
            p.AddSingleton<I920, S920>();
            p.AddSingleton<I921, S921>();
            p.AddSingleton<I922, S922>();
            p.AddSingleton<I923, S923>();
            p.AddSingleton<I924, S924>();
            p.AddSingleton<I925, S925>();
            p.AddSingleton<I926, S926>();
            p.AddSingleton<I927, S927>();
            p.AddSingleton<I928, S928>();
            p.AddSingleton<I929, S929>();
            p.AddSingleton<I930, S930>();
            p.AddSingleton<I931, S931>();
            p.AddSingleton<I932, S932>();
            p.AddSingleton<I933, S933>();
            p.AddSingleton<I934, S934>();
            p.AddSingleton<I935, S935>();
            p.AddSingleton<I936, S936>();
            p.AddSingleton<I937, S937>();
            p.AddSingleton<I938, S938>();
            p.AddSingleton<I939, S939>();
            p.AddSingleton<I940, S940>();
            p.AddSingleton<I941, S941>();
            p.AddSingleton<I942, S942>();
            p.AddSingleton<I943, S943>();
            p.AddSingleton<I944, S944>();
            p.AddSingleton<I945, S945>();
            p.AddSingleton<I946, S946>();
            p.AddSingleton<I947, S947>();
            p.AddSingleton<I948, S948>();
            p.AddSingleton<I949, S949>();
            p.AddSingleton<I950, S950>();
            p.AddSingleton<I951, S951>();
            p.AddSingleton<I952, S952>();
            p.AddSingleton<I953, S953>();
            p.AddSingleton<I954, S954>();
            p.AddSingleton<I955, S955>();
            p.AddSingleton<I956, S956>();
            p.AddSingleton<I957, S957>();
            p.AddSingleton<I958, S958>();
            p.AddSingleton<I959, S959>();
            p.AddSingleton<I960, S960>();
            p.AddSingleton<I961, S961>();
            p.AddSingleton<I962, S962>();
            p.AddSingleton<I963, S963>();
            p.AddSingleton<I964, S964>();
            p.AddSingleton<I965, S965>();
            p.AddSingleton<I966, S966>();
            p.AddSingleton<I967, S967>();
            p.AddSingleton<I968, S968>();
            p.AddSingleton<I969, S969>();
            p.AddSingleton<I970, S970>();
            p.AddSingleton<I971, S971>();
            p.AddSingleton<I972, S972>();
            p.AddSingleton<I973, S973>();
            p.AddSingleton<I974, S974>();
            p.AddSingleton<I975, S975>();
            p.AddSingleton<I976, S976>();
            p.AddSingleton<I977, S977>();
            p.AddSingleton<I978, S978>();
            p.AddSingleton<I979, S979>();
            p.AddSingleton<I980, S980>();
            p.AddSingleton<I981, S981>();
            p.AddSingleton<I982, S982>();
            p.AddSingleton<I983, S983>();
            p.AddSingleton<I984, S984>();
            p.AddSingleton<I985, S985>();
            p.AddSingleton<I986, S986>();
            p.AddSingleton<I987, S987>();
            p.AddSingleton<I988, S988>();
            p.AddSingleton<I989, S989>();
            p.AddSingleton<I990, S990>();
            p.AddSingleton<I991, S991>();
            p.AddSingleton<I992, S992>();
            p.AddSingleton<I993, S993>();
            p.AddSingleton<I994, S994>();
            p.AddSingleton<I995, S995>();
            p.AddSingleton<I996, S996>();
            p.AddSingleton<I997, S997>();
            p.AddSingleton<I998, S998>();
            p.AddSingleton<I999, S999>();
        }
    }
}
