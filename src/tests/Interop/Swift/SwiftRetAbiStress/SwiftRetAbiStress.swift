// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

@frozen
public struct S0
{
    public let f0 : Int16;
    public let f1 : Int32;
    public let f2 : UInt64;
}

public func swiftRetFunc0() -> S0 {
    return S0(f0: -17813, f1: 318006528, f2: 1195162122024233590)
}

@frozen
public struct S1
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : Int64;
    public let f3 : UInt32;
}

public func swiftRetFunc1() -> S1 {
    return S1(f0: -29793, f1: 7351779, f2: 133491708229548754, f3: 665726990)
}

@frozen
public struct S2_S0
{
    public let f0 : UInt64;
}

@frozen
public struct S2
{
    public let f0 : S2_S0;
    public let f1 : UInt8;
    public let f2 : UInt16;
    public let f3 : Float;
    public let f4 : Int32;
}

public func swiftRetFunc2() -> S2 {
    return S2(f0: S2_S0(f0: 2153637757371267722), f1: 150, f2: 48920, f3: 3564327, f4: 1310569731)
}

@frozen
public struct S3
{
    public let f0 : Int64;
    public let f1 : Double;
    public let f2 : Int8;
    public let f3 : Int32;
    public let f4 : UInt16;
    public let f5 : UInt8;
    public let f6 : Double;
}

public func swiftRetFunc3() -> S3 {
    return S3(f0: 5610153900386943274, f1: 2431035148834736, f2: 111, f3: 772269424, f4: 19240, f5: 146, f6: 821805530740405)
}

@frozen
public struct S4
{
    public let f0 : Int8;
    public let f1 : UInt32;
    public let f2 : UInt64;
    public let f3 : Int64;
}

public func swiftRetFunc4() -> S4 {
    return S4(f0: 125, f1: 377073381, f2: 964784376430620335, f3: 5588038704850976624)
}

@frozen
public struct S5_S0
{
    public let f0 : UInt32;
    public let f1 : Double;
}

@frozen
public struct S5
{
    public let f0 : UInt64;
    public let f1 : Int8;
    public let f2 : UInt;
    public let f3 : S5_S0;
    public let f4 : Int;
    public let f5 : UInt8;
}

public func swiftRetFunc5() -> S5 {
    return S5(f0: 5315019731968023493, f1: 114, f2: 1154655179105889397, f3: S5_S0(f0: 1468030771, f1: 3066473182924818), f4: 6252650621827449809, f5: 129)
}

@frozen
public struct S6
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : Int64;
    public let f3 : UInt16;
}

public func swiftRetFunc6() -> S6 {
    return S6(f0: 743741783, f1: -6821, f2: 5908745692727636656, f3: 64295)
}

@frozen
public struct S7_S0
{
    public let f0 : Int;
}

@frozen
public struct S7
{
    public let f0 : S7_S0;
}

public func swiftRetFunc7() -> S7 {
    return S7(f0: S7_S0(f0: 7625368278886567558))
}

@frozen
public struct S8
{
    public let f0 : Int;
}

public func swiftRetFunc8() -> S8 {
    return S8(f0: 775279004683334365)
}

@frozen
public struct S9_S0
{
    public let f0 : Int16;
    public let f1 : Int32;
}

@frozen
public struct S9
{
    public let f0 : UInt32;
    public let f1 : Int;
    public let f2 : S9_S0;
    public let f3 : UInt16;
}

public func swiftRetFunc9() -> S9 {
    return S9(f0: 1223030410, f1: 4720638462358523954, f2: S9_S0(f0: 30631, f1: 1033774469), f3: 64474)
}

@frozen
public struct S10
{
    public let f0 : Float;
    public let f1 : Float;
}

public func swiftRetFunc10() -> S10 {
    return S10(f0: 3276917, f1: 6694615)
}

@frozen
public struct S11
{
    public let f0 : Double;
    public let f1 : Int;
    public let f2 : UInt32;
    public let f3 : Int8;
}

public func swiftRetFunc11() -> S11 {
    return S11(f0: 938206348036312, f1: 6559514243876905696, f2: 1357772248, f3: 59)
}

@frozen
public struct S12
{
    public let f0 : Double;
}

public func swiftRetFunc12() -> S12 {
    return S12(f0: 1580503485222363)
}

@frozen
public struct S13
{
    public let f0 : UInt32;
}

public func swiftRetFunc13() -> S13 {
    return S13(f0: 1381551558)
}

@frozen
public struct S14_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct S14_S0
{
    public let f0 : S14_S0_S0;
}

@frozen
public struct S14
{
    public let f0 : Int32;
    public let f1 : UInt16;
    public let f2 : Int8;
    public let f3 : Float;
    public let f4 : UInt64;
    public let f5 : S14_S0;
    public let f6 : Int8;
}

public func swiftRetFunc14() -> S14 {
    return S14(f0: 1765691191, f1: 56629, f2: 25, f3: 2944946, f4: 951929105049584033, f5: S14_S0(f0: S14_S0_S0(f0: -30)), f6: 66)
}

@frozen
public struct S15_S0
{
    public let f0 : UInt;
    public let f1 : Float;
}

@frozen
public struct S15
{
    public let f0 : Int;
    public let f1 : S15_S0;
    public let f2 : UInt16;
    public let f3 : Int32;
}

public func swiftRetFunc15() -> S15 {
    return S15(f0: 2090703541638269172, f1: S15_S0(f0: 6408314016925514463, f1: 6534515), f2: 30438, f3: 1745811802)
}

@frozen
public struct S16
{
    public let f0 : UInt32;
    public let f1 : UInt64;
    public let f2 : UInt8;
    public let f3 : Int32;
    public let f4 : UInt;
    public let f5 : Int8;
}

public func swiftRetFunc16() -> S16 {
    return S16(f0: 585220635, f1: 4034210936973794153, f2: 48, f3: 1155081155, f4: 806384837403045657, f5: 54)
}

@frozen
public struct S17
{
    public let f0 : UInt8;
    public let f1 : Int8;
    public let f2 : UInt8;
}

public func swiftRetFunc17() -> S17 {
    return S17(f0: 23, f1: 112, f2: 15)
}

@frozen
public struct S18_S0
{
    public let f0 : UInt32;
    public let f1 : Float;
}

@frozen
public struct S18
{
    public let f0 : S18_S0;
    public let f1 : Int;
    public let f2 : Int32;
    public let f3 : UInt16;
    public let f4 : Int16;
}

public func swiftRetFunc18() -> S18 {
    return S18(f0: S18_S0(f0: 1964425016, f1: 2767295), f1: 6016563774923595868, f2: 1648562735, f3: 378, f4: -20536)
}

@frozen
public struct S19
{
    public let f0 : UInt8;
    public let f1 : UInt16;
    public let f2 : Float;
    public let f3 : UInt64;
    public let f4 : Int32;
}

public func swiftRetFunc19() -> S19 {
    return S19(f0: 188, f1: 47167, f2: 6781297, f3: 8140268502944465472, f4: 708690468)
}

@frozen
public struct S20_S0
{
    public let f0 : UInt32;
    public let f1 : Float;
}

@frozen
public struct S20
{
    public let f0 : S20_S0;
    public let f1 : UInt8;
}

public func swiftRetFunc20() -> S20 {
    return S20(f0: S20_S0(f0: 2019361333, f1: 938975), f1: 192)
}

@frozen
public struct S21_S0_S0
{
    public let f0 : UInt16;
}

@frozen
public struct S21_S0
{
    public let f0 : S21_S0_S0;
}

@frozen
public struct S21
{
    public let f0 : Double;
    public let f1 : Double;
    public let f2 : UInt;
    public let f3 : Int;
    public let f4 : UInt64;
    public let f5 : S21_S0;
}

public func swiftRetFunc21() -> S21 {
    return S21(f0: 1693878073402490, f1: 3392111340517811, f2: 3584917502172813732, f3: 665495086154608745, f4: 2918107814961929578, f5: S21_S0(f0: S21_S0_S0(f0: 4634)))
}

@frozen
public struct S22
{
    public let f0 : UInt32;
}

public func swiftRetFunc22() -> S22 {
    return S22(f0: 640156952)
}

@frozen
public struct S23
{
    public let f0 : UInt8;
    public let f1 : Int16;
    public let f2 : UInt64;
    public let f3 : UInt;
    public let f4 : UInt;
    public let f5 : UInt64;
    public let f6 : UInt8;
}

public func swiftRetFunc23() -> S23 {
    return S23(f0: 122, f1: 28995, f2: 25673626033589541, f3: 828363978755325884, f4: 3065573182429720699, f5: 1484484917001276079, f6: 209)
}

@frozen
public struct S24
{
    public let f0 : UInt64;
    public let f1 : UInt64;
}

public func swiftRetFunc24() -> S24 {
    return S24(f0: 2621245238416080387, f1: 6541787564638363256)
}

@frozen
public struct S25_S0
{
    public let f0 : Int;
}

@frozen
public struct S25
{
    public let f0 : Int8;
    public let f1 : Int8;
    public let f2 : UInt8;
    public let f3 : S25_S0;
    public let f4 : UInt32;
}

public func swiftRetFunc25() -> S25 {
    return S25(f0: 30, f1: -8, f2: 168, f3: S25_S0(f0: 7601538494489501573), f4: 814523741)
}

@frozen
public struct S26
{
    public let f0 : Float;
}

public func swiftRetFunc26() -> S26 {
    return S26(f0: 3681545)
}

@frozen
public struct S27
{
    public let f0 : Int64;
    public let f1 : Double;
    public let f2 : Int8;
    public let f3 : Int;
    public let f4 : Int16;
    public let f5 : Int64;
}

public func swiftRetFunc27() -> S27 {
    return S27(f0: 4847421047018330189, f1: 3655171692392280, f2: 46, f3: 4476120319602257660, f4: -6106, f5: 5756567968111212829)
}

@frozen
public struct S28_S0
{
    public let f0 : Double;
}

@frozen
public struct S28
{
    public let f0 : Float;
    public let f1 : Int16;
    public let f2 : S28_S0;
    public let f3 : Double;
    public let f4 : UInt64;
}

public func swiftRetFunc28() -> S28 {
    return S28(f0: 3491512, f1: 5249, f2: S28_S0(f0: 1107064327388314), f3: 2170381648425673, f4: 5138313315157580943)
}

@frozen
public struct S29
{
    public let f0 : UInt16;
    public let f1 : UInt32;
    public let f2 : Int16;
    public let f3 : Int32;
    public let f4 : Int32;
    public let f5 : UInt64;
    public let f6 : Int16;
}

public func swiftRetFunc29() -> S29 {
    return S29(f0: 39000, f1: 408611655, f2: 18090, f3: 351857085, f4: 1103441843, f5: 5162040247631126074, f6: -27930)
}

@frozen
public struct S30_S0
{
    public let f0 : Int8;
    public let f1 : Int8;
    public let f2 : Int32;
}

@frozen
public struct S30_S1
{
    public let f0 : Float;
}

@frozen
public struct S30
{
    public let f0 : Float;
    public let f1 : S30_S0;
    public let f2 : S30_S1;
    public let f3 : Int64;
}

public func swiftRetFunc30() -> S30 {
    return S30(f0: 6492602, f1: S30_S0(f0: 76, f1: -26, f2: 1777644423), f2: S30_S1(f0: 6558571), f3: 5879147675377398012)
}

@frozen
public struct S31
{
    public let f0 : Int64;
    public let f1 : UInt64;
    public let f2 : UInt16;
    public let f3 : UInt16;
    public let f4 : Int8;
}

public func swiftRetFunc31() -> S31 {
    return S31(f0: 4699402628739628277, f1: 7062790893852687562, f2: 28087, f3: 11088, f4: 69)
}

@frozen
public struct S32
{
    public let f0 : Int32;
    public let f1 : UInt64;
    public let f2 : UInt64;
    public let f3 : UInt32;
    public let f4 : Int16;
    public let f5 : UInt16;
}

public func swiftRetFunc32() -> S32 {
    return S32(f0: 688805466, f1: 8860655326984381661, f2: 6943423675662271404, f3: 196368476, f4: 14229, f5: 34635)
}

@frozen
public struct S33
{
    public let f0 : UInt16;
    public let f1 : UInt32;
    public let f2 : Int32;
    public let f3 : UInt16;
    public let f4 : Float;
    public let f5 : UInt64;
    public let f6 : Int;
}

public func swiftRetFunc33() -> S33 {
    return S33(f0: 9297, f1: 7963252, f2: 556244690, f3: 19447, f4: 6930550, f5: 126294981263481729, f6: 2540579257616511618)
}

@frozen
public struct S34
{
    public let f0 : Int64;
    public let f1 : UInt32;
    public let f2 : UInt64;
}

public func swiftRetFunc34() -> S34 {
    return S34(f0: 5845561428743737556, f1: 1358941228, f2: 3701080255861218446)
}

@frozen
public struct S35
{
    public let f0 : Float;
    public let f1 : Float;
    public let f2 : Int64;
    public let f3 : UInt8;
    public let f4 : Double;
    public let f5 : UInt16;
}

public func swiftRetFunc35() -> S35 {
    return S35(f0: 5982956, f1: 3675164, f2: 229451138397478297, f3: 163, f4: 2925293762193390, f5: 5018)
}

@frozen
public struct S36
{
    public let f0 : Int32;
    public let f1 : Int64;
    public let f2 : UInt64;
}

public func swiftRetFunc36() -> S36 {
    return S36(f0: 1915776502, f1: 2197655909333830531, f2: 6072941592567177049)
}

@frozen
public struct S37
{
    public let f0 : UInt8;
    public let f1 : Double;
}

public func swiftRetFunc37() -> S37 {
    return S37(f0: 18, f1: 4063164371882658)
}

@frozen
public struct S38
{
    public let f0 : UInt;
    public let f1 : Int64;
    public let f2 : UInt8;
    public let f3 : UInt;
}

public func swiftRetFunc38() -> S38 {
    return S38(f0: 7389960750529773276, f1: 2725802169582362061, f2: 2, f3: 3659261019360356514)
}

@frozen
public struct S39
{
    public let f0 : Int32;
    public let f1 : Int32;
    public let f2 : Int;
    public let f3 : Int16;
    public let f4 : UInt16;
}

public func swiftRetFunc39() -> S39 {
    return S39(f0: 50995691, f1: 1623216479, f2: 2906650346451599789, f3: 28648, f4: 8278)
}

@frozen
public struct S40_S0
{
    public let f0 : Float;
    public let f1 : UInt8;
    public let f2 : Int8;
    public let f3 : UInt;
    public let f4 : Double;
}

@frozen
public struct S40
{
    public let f0 : S40_S0;
    public let f1 : Int16;
    public let f2 : Int16;
}

public func swiftRetFunc40() -> S40 {
    return S40(f0: S40_S0(f0: 7087264, f1: 37, f2: -5, f3: 479915249821490487, f4: 144033730096589), f1: 28654, f2: 16398)
}

@frozen
public struct S41
{
    public let f0 : UInt;
    public let f1 : UInt;
}

public func swiftRetFunc41() -> S41 {
    return S41(f0: 7923718819069382599, f1: 1539666179674725957)
}

@frozen
public struct S42_S0
{
    public let f0 : Int32;
}

@frozen
public struct S42
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : S42_S0;
    public let f3 : UInt;
}

public func swiftRetFunc42() -> S42 {
    return S42(f0: 1046060439, f1: 8249831314190867613, f2: S42_S0(f0: 1097582349), f3: 2864677262092469436)
}

@frozen
public struct S43_S0_S0
{
    public let f0 : Float;
}

@frozen
public struct S43_S0
{
    public let f0 : S43_S0_S0;
}

@frozen
public struct S43
{
    public let f0 : S43_S0;
    public let f1 : Int8;
}

public func swiftRetFunc43() -> S43 {
    return S43(f0: S43_S0(f0: S43_S0_S0(f0: 1586338)), f1: 104)
}

@frozen
public struct S44
{
    public let f0 : UInt8;
    public let f1 : Int32;
    public let f2 : Int;
    public let f3 : UInt32;
}

public func swiftRetFunc44() -> S44 {
    return S44(f0: 94, f1: 1109076022, f2: 3135595850598607828, f3: 760084013)
}

@frozen
public struct S45_S0
{
    public let f0 : Int64;
}

@frozen
public struct S45
{
    public let f0 : Int16;
    public let f1 : UInt64;
    public let f2 : Int;
    public let f3 : S45_S0;
}

public func swiftRetFunc45() -> S45 {
    return S45(f0: 3071, f1: 5908138438609341766, f2: 5870206722419946629, f3: S45_S0(f0: 8128455876189744801))
}

@frozen
public struct S46
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : Int8;
    public let f3 : UInt32;
    public let f4 : UInt8;
    public let f5 : Int32;
}

public func swiftRetFunc46() -> S46 {
    return S46(f0: 14794, f1: 60, f2: -77, f3: 653898879, f4: 224, f5: 266602433)
}

@frozen
public struct S47_S0
{
    public let f0 : Int8;
}

@frozen
public struct S47
{
    public let f0 : Double;
    public let f1 : S47_S0;
}

public func swiftRetFunc47() -> S47 {
    return S47(f0: 3195976594911793, f1: S47_S0(f0: -91))
}

@frozen
public struct S48
{
    public let f0 : Int;
}

public func swiftRetFunc48() -> S48 {
    return S48(f0: 778504172538154682)
}

@frozen
public struct S49_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct S49_S0
{
    public let f0 : S49_S0_S0;
}

@frozen
public struct S49
{
    public let f0 : UInt64;
    public let f1 : S49_S0;
    public let f2 : Int8;
    public let f3 : Double;
    public let f4 : UInt32;
    public let f5 : UInt32;
}

public func swiftRetFunc49() -> S49 {
    return S49(f0: 4235011519458710874, f1: S49_S0(f0: S49_S0_S0(f0: 3120420438742285733)), f2: -8, f3: 1077419570643725, f4: 1985303212, f5: 264580506)
}

@frozen
public struct S50
{
    public let f0 : Int32;
}

public func swiftRetFunc50() -> S50 {
    return S50(f0: 1043912405)
}

@frozen
public struct S51_S0_S0_S0
{
    public let f0 : Float;
}

@frozen
public struct S51_S0_S0
{
    public let f0 : S51_S0_S0_S0;
    public let f1 : Int16;
}

@frozen
public struct S51_S0
{
    public let f0 : Double;
    public let f1 : S51_S0_S0;
    public let f2 : UInt8;
    public let f3 : Int64;
}

@frozen
public struct S51
{
    public let f0 : S51_S0;
    public let f1 : Double;
}

public func swiftRetFunc51() -> S51 {
    return S51(f0: S51_S0(f0: 3266680719186600, f1: S51_S0_S0(f0: S51_S0_S0_S0(f0: 428247), f1: -24968), f2: 76, f3: 183022772513065490), f1: 2661928101793033)
}

@frozen
public struct S52
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : UInt32;
    public let f3 : UInt64;
    public let f4 : Int;
    public let f5 : Int8;
}

public func swiftRetFunc52() -> S52 {
    return S52(f0: 1812191671, f1: 6594574760089190928, f2: 831147243, f3: 3301835731003365248, f4: 5382332538247340743, f5: -77)
}

@frozen
public struct S53_S0
{
    public let f0 : Int8;
    public let f1 : UInt;
}

@frozen
public struct S53
{
    public let f0 : S53_S0;
    public let f1 : Int32;
    public let f2 : Int64;
    public let f3 : Float;
    public let f4 : Int8;
}

public func swiftRetFunc53() -> S53 {
    return S53(f0: S53_S0(f0: -123, f1: 3494916243607193741), f1: 1406699798, f2: 4018943158751734338, f3: 1084415, f4: -8)
}

@frozen
public struct S54_S0
{
    public let f0 : Double;
}

@frozen
public struct S54
{
    public let f0 : Int;
    public let f1 : Int;
    public let f2 : S54_S0;
    public let f3 : Int64;
}

public func swiftRetFunc54() -> S54 {
    return S54(f0: 8623517456704997133, f1: 1521939500434086364, f2: S54_S0(f0: 3472783299414218), f3: 4761507229870258916)
}

@frozen
public struct S55
{
    public let f0 : Int16;
    public let f1 : UInt32;
    public let f2 : Int64;
    public let f3 : UInt32;
    public let f4 : Int8;
    public let f5 : UInt8;
}

public func swiftRetFunc55() -> S55 {
    return S55(f0: -28051, f1: 1759912152, f2: 2038322238348454200, f3: 601094102, f4: 5, f5: 75)
}

@frozen
public struct S56
{
    public let f0 : UInt64;
    public let f1 : Float;
    public let f2 : Int8;
    public let f3 : Int32;
}

public func swiftRetFunc56() -> S56 {
    return S56(f0: 6313168909786453069, f1: 6254558, f2: 115, f3: 847834891)
}

@frozen
public struct S57
{
    public let f0 : UInt;
    public let f1 : Int16;
    public let f2 : Int8;
    public let f3 : Int32;
}

public func swiftRetFunc57() -> S57 {
    return S57(f0: 546304219852233452, f1: -27416, f2: 47, f3: 1094575684)
}

@frozen
public struct S58
{
    public let f0 : UInt64;
    public let f1 : UInt64;
}

public func swiftRetFunc58() -> S58 {
    return S58(f0: 4612004722568513699, f1: 2222525519606580195)
}

@frozen
public struct S59
{
    public let f0 : Int8;
    public let f1 : UInt;
    public let f2 : Int;
    public let f3 : Int8;
    public let f4 : Int64;
    public let f5 : UInt8;
}

public func swiftRetFunc59() -> S59 {
    return S59(f0: -92, f1: 7281011081566942937, f2: 8203439771560005792, f3: 103, f4: 1003386607251132236, f5: 6)
}

@frozen
public struct S60
{
    public let f0 : UInt64;
    public let f1 : Int;
}

public func swiftRetFunc60() -> S60 {
    return S60(f0: 6922353269487057763, f1: 103032455997325768)
}

@frozen
public struct S61_S0
{
    public let f0 : Int64;
    public let f1 : Int64;
    public let f2 : Float;
}

@frozen
public struct S61
{
    public let f0 : UInt64;
    public let f1 : S61_S0;
    public let f2 : Int16;
    public let f3 : Int32;
}

public func swiftRetFunc61() -> S61 {
    return S61(f0: 3465845922566501572, f1: S61_S0(f0: 8266662359091888314, f1: 7511705648638703076, f2: 535470), f2: -5945, f3: 523043523)
}

@frozen
public struct S62_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct S62_S0
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : UInt16;
    public let f3 : S62_S0_S0;
}

@frozen
public struct S62
{
    public let f0 : S62_S0;
    public let f1 : Int;
    public let f2 : UInt16;
}

public func swiftRetFunc62() -> S62 {
    return S62(f0: S62_S0(f0: 50789, f1: 30245, f2: 35063, f3: S62_S0_S0(f0: 3102684963408623932)), f1: 792877586576090769, f2: 24697)
}

@frozen
public struct S63
{
    public let f0 : Double;
    public let f1 : Int;
    public let f2 : Double;
    public let f3 : Int8;
    public let f4 : Float;
}

public func swiftRetFunc63() -> S63 {
    return S63(f0: 4097323000009314, f1: 4162427097168837193, f2: 140736061437152, f3: -59, f4: 7331757)
}

@frozen
public struct S64_S0
{
    public let f0 : UInt64;
}

@frozen
public struct S64
{
    public let f0 : S64_S0;
    public let f1 : UInt64;
    public let f2 : Int64;
    public let f3 : Int;
}

public func swiftRetFunc64() -> S64 {
    return S64(f0: S64_S0(f0: 2624461610177878495), f1: 5222178027019975511, f2: 9006949357929457355, f3: 7966680593035770540)
}

@frozen
public struct S65
{
    public let f0 : Int;
    public let f1 : Double;
    public let f2 : UInt16;
    public let f3 : Int16;
    public let f4 : UInt8;
    public let f5 : Int32;
    public let f6 : UInt64;
}

public func swiftRetFunc65() -> S65 {
    return S65(f0: 6080968957098434687, f1: 3067343828504927, f2: 56887, f3: 804, f4: 235, f5: 121742660, f6: 9218677163034827308)
}

@frozen
public struct S66
{
    public let f0 : Int8;
    public let f1 : UInt64;
    public let f2 : UInt32;
    public let f3 : UInt64;
    public let f4 : UInt64;
}

public func swiftRetFunc66() -> S66 {
    return S66(f0: -16, f1: 7967447403042597794, f2: 2029697750, f3: 4180031087394830849, f4: 5847795120921557969)
}

@frozen
public struct S67_S0
{
    public let f0 : UInt64;
}

@frozen
public struct S67
{
    public let f0 : S67_S0;
    public let f1 : UInt8;
    public let f2 : UInt16;
    public let f3 : UInt64;
    public let f4 : UInt64;
    public let f5 : Int8;
}

public func swiftRetFunc67() -> S67 {
    return S67(f0: S67_S0(f0: 4844204675254434929), f1: 135, f2: 13969, f3: 4897129719050177731, f4: 7233638107485862921, f5: -11)
}

@frozen
public struct S68_S0
{
    public let f0 : Double;
}

@frozen
public struct S68
{
    public let f0 : Int32;
    public let f1 : UInt64;
    public let f2 : UInt32;
    public let f3 : S68_S0;
    public let f4 : Int32;
    public let f5 : Int8;
}

public func swiftRetFunc68() -> S68 {
    return S68(f0: 1708606840, f1: 1768121573985581212, f2: 1033319213, f3: S68_S0(f0: 2741322436867931), f4: 955320338, f5: 12)
}

@frozen
public struct S69
{
    public let f0 : UInt32;
}

public func swiftRetFunc69() -> S69 {
    return S69(f0: 2092746473)
}

@frozen
public struct S70
{
    public let f0 : UInt8;
    public let f1 : Float;
}

public func swiftRetFunc70() -> S70 {
    return S70(f0: 76, f1: 4138467)
}

@frozen
public struct S71_S0
{
    public let f0 : Int8;
    public let f1 : UInt64;
    public let f2 : Int64;
}

@frozen
public struct S71
{
    public let f0 : S71_S0;
    public let f1 : UInt8;
    public let f2 : UInt8;
}

public func swiftRetFunc71() -> S71 {
    return S71(f0: S71_S0(f0: -98, f1: 8603744544763953916, f2: 8460721064583106347), f1: 10, f2: 88)
}

@frozen
public struct S72
{
    public let f0 : UInt32;
}

public func swiftRetFunc72() -> S72 {
    return S72(f0: 2021509367)
}

@frozen
public struct S73
{
    public let f0 : Int;
    public let f1 : Int16;
    public let f2 : UInt64;
    public let f3 : Float;
    public let f4 : Int32;
    public let f5 : UInt;
    public let f6 : UInt;
}

public func swiftRetFunc73() -> S73 {
    return S73(f0: 6222563427944465437, f1: 28721, f2: 1313300783845289148, f3: 6761, f4: 2074171265, f5: 6232209228889209160, f6: 1423931135184844265)
}

@frozen
public struct S74
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : Double;
    public let f3 : UInt16;
    public let f4 : Int8;
}

public func swiftRetFunc74() -> S74 {
    return S74(f0: 27115, f1: 1416098, f2: 4468576755457331, f3: 58864, f4: 81)
}

@frozen
public struct S75_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct S75_S0
{
    public let f0 : S75_S0_S0;
    public let f1 : UInt8;
}

@frozen
public struct S75
{
    public let f0 : UInt64;
    public let f1 : S75_S0;
    public let f2 : UInt8;
}

public func swiftRetFunc75() -> S75 {
    return S75(f0: 8532911974860912350, f1: S75_S0(f0: S75_S0_S0(f0: -60), f1: 66), f2: 200)
}

@frozen
public struct S76_S0_S0
{
    public let f0 : Int16;
}

@frozen
public struct S76_S0
{
    public let f0 : Int8;
    public let f1 : UInt64;
    public let f2 : S76_S0_S0;
    public let f3 : Double;
}

@frozen
public struct S76
{
    public let f0 : UInt8;
    public let f1 : S76_S0;
    public let f2 : Double;
}

public func swiftRetFunc76() -> S76 {
    return S76(f0: 69, f1: S76_S0(f0: -29, f1: 4872234474620951743, f2: S76_S0_S0(f0: 11036), f3: 585486652063917), f2: 2265391710186639)
}

@frozen
public struct S77
{
    public let f0 : Int32;
    public let f1 : Int32;
    public let f2 : Int32;
    public let f3 : UInt32;
    public let f4 : Int16;
}

public func swiftRetFunc77() -> S77 {
    return S77(f0: 4495211, f1: 1364377405, f2: 773989694, f3: 1121696315, f4: 7589)
}

@frozen
public struct S78
{
    public let f0 : UInt32;
    public let f1 : UInt;
}

public func swiftRetFunc78() -> S78 {
    return S78(f0: 1767839225, f1: 7917317019379224114)
}

@frozen
public struct S79_S0
{
    public let f0 : Double;
    public let f1 : UInt32;
    public let f2 : Int32;
}

@frozen
public struct S79
{
    public let f0 : S79_S0;
    public let f1 : UInt8;
    public let f2 : Double;
}

public func swiftRetFunc79() -> S79 {
    return S79(f0: S79_S0(f0: 495074072703635, f1: 417605286, f2: 171326442), f1: 203, f2: 2976663235490421)
}

@frozen
public struct S80
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : Int8;
}

public func swiftRetFunc80() -> S80 {
    return S80(f0: 999559959, f1: 19977, f2: -4)
}

@frozen
public struct S81_S0
{
    public let f0 : UInt;
}

@frozen
public struct S81
{
    public let f0 : Int32;
    public let f1 : S81_S0;
    public let f2 : Float;
    public let f3 : Int64;
    public let f4 : UInt32;
    public let f5 : UInt8;
    public let f6 : Int16;
}

public func swiftRetFunc81() -> S81 {
    return S81(f0: 452603110, f1: S81_S0(f0: 6240652733420985265), f2: 6469988, f3: 5775316279348621124, f4: 1398033592, f5: 105, f6: 21937)
}

@frozen
public struct S82
{
    public let f0 : Int;
}

public func swiftRetFunc82() -> S82 {
    return S82(f0: 6454754584537364459)
}

@frozen
public struct S83
{
    public let f0 : UInt64;
    public let f1 : UInt32;
    public let f2 : Float;
    public let f3 : UInt8;
    public let f4 : Float;
}

public func swiftRetFunc83() -> S83 {
    return S83(f0: 2998238441521688907, f1: 9623946, f2: 2577885, f3: 156, f4: 6678807)
}

@frozen
public struct S84_S0
{
    public let f0 : Int16;
}

@frozen
public struct S84
{
    public let f0 : S84_S0;
}

public func swiftRetFunc84() -> S84 {
    return S84(f0: S84_S0(f0: 16213))
}

@frozen
public struct S85_S0
{
    public let f0 : Int16;
    public let f1 : Int8;
}

@frozen
public struct S85
{
    public let f0 : Int64;
    public let f1 : UInt8;
    public let f2 : S85_S0;
    public let f3 : Float;
    public let f4 : Int;
}

public func swiftRetFunc85() -> S85 {
    return S85(f0: 8858924985061791416, f1: 200, f2: S85_S0(f0: 4504, f1: 60), f3: 5572917, f4: 6546369836182556538)
}

@frozen
public struct S86
{
    public let f0 : UInt16;
    public let f1 : Float;
    public let f2 : UInt32;
}

public func swiftRetFunc86() -> S86 {
    return S86(f0: 22762, f1: 4672435, f2: 719927700)
}

@frozen
public struct S87
{
    public let f0 : Int32;
    public let f1 : UInt;
    public let f2 : UInt64;
}

public func swiftRetFunc87() -> S87 {
    return S87(f0: 361750184, f1: 4206825694012787823, f2: 2885153391732919282)
}

@frozen
public struct S88
{
    public let f0 : UInt32;
    public let f1 : Int16;
    public let f2 : UInt32;
}

public func swiftRetFunc88() -> S88 {
    return S88(f0: 2125094198, f1: -10705, f2: 182007583)
}

@frozen
public struct S89
{
    public let f0 : UInt8;
    public let f1 : UInt32;
    public let f2 : Int32;
    public let f3 : Int8;
    public let f4 : Int64;
}

public func swiftRetFunc89() -> S89 {
    return S89(f0: 175, f1: 1062985476, f2: 1019006263, f3: -22, f4: 6888877252788498422)
}

@frozen
public struct S90
{
    public let f0 : UInt8;
    public let f1 : Int32;
    public let f2 : Int16;
    public let f3 : Int;
    public let f4 : UInt32;
    public let f5 : UInt32;
    public let f6 : Int64;
}

public func swiftRetFunc90() -> S90 {
    return S90(f0: 221, f1: 225825436, f2: -26231, f3: 5122880520199505508, f4: 907657092, f5: 707089277, f6: 6091814344013414920)
}

@frozen
public struct S91
{
    public let f0 : Double;
    public let f1 : Int8;
    public let f2 : Int8;
    public let f3 : UInt32;
    public let f4 : Int;
    public let f5 : Int8;
    public let f6 : Int16;
}

public func swiftRetFunc91() -> S91 {
    return S91(f0: 3265110225161261, f1: 62, f2: -38, f3: 946023589, f4: 4109819715069879890, f5: -73, f6: 20363)
}

@frozen
public struct S92_S0
{
    public let f0 : Float;
    public let f1 : Int64;
}

@frozen
public struct S92
{
    public let f0 : Int64;
    public let f1 : UInt;
    public let f2 : S92_S0;
    public let f3 : Int32;
    public let f4 : Float;
    public let f5 : Float;
}

public func swiftRetFunc92() -> S92 {
    return S92(f0: 3230438394207610137, f1: 3003396252681176136, f2: S92_S0(f0: 6494422, f1: 2971773224350614312), f3: 2063694141, f4: 3117041, f5: 1003760)
}

@frozen
public struct S93
{
    public let f0 : Int;
    public let f1 : UInt8;
    public let f2 : UInt32;
    public let f3 : UInt32;
    public let f4 : UInt64;
}

public func swiftRetFunc93() -> S93 {
    return S93(f0: 5170226481546239050, f1: 11, f2: 1120259582, f3: 1947849905, f4: 3690113387392112192)
}

@frozen
public struct S94
{
    public let f0 : UInt16;
    public let f1 : Double;
    public let f2 : Int16;
    public let f3 : Double;
    public let f4 : UInt64;
}

public func swiftRetFunc94() -> S94 {
    return S94(f0: 57111, f1: 1718940123307098, f2: -16145, f3: 1099321301986326, f4: 2972912419231960385)
}

@frozen
public struct S95_S0
{
    public let f0 : Double;
}

@frozen
public struct S95
{
    public let f0 : Int16;
    public let f1 : S95_S0;
    public let f2 : UInt64;
}

public func swiftRetFunc95() -> S95 {
    return S95(f0: 12620, f1: S95_S0(f0: 3232445258308074), f2: 97365157264460373)
}

@frozen
public struct S96
{
    public let f0 : Int8;
    public let f1 : Double;
    public let f2 : UInt64;
    public let f3 : UInt64;
    public let f4 : Int32;
    public let f5 : Int64;
}

public func swiftRetFunc96() -> S96 {
    return S96(f0: 3, f1: 242355060906873, f2: 3087879465791321798, f3: 7363229136420263380, f4: 46853328, f5: 4148307028758236491)
}

@frozen
public struct S97
{
    public let f0 : UInt16;
    public let f1 : Int32;
    public let f2 : UInt16;
    public let f3 : UInt32;
}

public func swiftRetFunc97() -> S97 {
    return S97(f0: 10651, f1: 2068379463, f2: 57307, f3: 329271020)
}

@frozen
public struct S98
{
    public let f0 : Double;
    public let f1 : Int32;
    public let f2 : Int64;
    public let f3 : Int;
    public let f4 : Float;
    public let f5 : Double;
}

public func swiftRetFunc98() -> S98 {
    return S98(f0: 2250389231883613, f1: 1755058358, f2: 6686142382639170849, f3: 6456632014163315773, f4: 2818253, f5: 1085859434505817)
}

@frozen
public struct S99_S0
{
    public let f0 : Int32;
}

@frozen
public struct S99
{
    public let f0 : S99_S0;
    public let f1 : Float;
}

public func swiftRetFunc99() -> S99 {
    return S99(f0: S99_S0(f0: 1117297545), f1: 1539294)
}

