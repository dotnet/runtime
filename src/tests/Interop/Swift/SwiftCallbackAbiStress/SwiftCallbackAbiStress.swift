// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

@frozen
public struct F0_Ret
{
    public let f0 : UInt16;
    public let f1 : Float;
    public let f2 : Int32;
    public let f3 : UInt64;
}

public func swiftCallbackFunc0(f: (Int16, Int32, UInt64, UInt16, Int64, Double, UInt32, UInt16, Int, UInt64) -> F0_Ret) -> F0_Ret {
    return f(-17813, 318006528, 1195162122024233590, 60467, 4587464142261794085, 2686980744237725, 331986645, 56299, 6785053689615432643, 6358078381523084952)
}

public func swiftCallbackFunc1(f: (Double, Int8, Int32, UInt16, UInt8, Double, UInt8, UInt64, Int16, Float, Float, UInt64, Int8) -> UInt) -> UInt {
    return f(3867437130564654, -64, 31081182, 20316, 73, 3543740592144911, 250, 6680393408153342744, 23758, 7189013, 5438196, 3310322731568932038, 3)
}

@frozen
public struct F2_Ret_S0
{
    public let f0 : Int64;
    public let f1 : Int32;
}

@frozen
public struct F2_Ret
{
    public let f0 : F2_Ret_S0;
    public let f1 : Int16;
}

public func swiftCallbackFunc2(f: (UInt, UInt8) -> F2_Ret) -> F2_Ret {
    return f(2153637757371267722, 150)
}

@frozen
public struct F3_Ret_S0
{
    public let f0 : Int16;
    public let f1 : Int32;
    public let f2 : UInt16;
}

@frozen
public struct F3_Ret
{
    public let f0 : Int;
    public let f1 : F3_Ret_S0;
    public let f2 : UInt;
    public let f3 : Int8;
}

public func swiftCallbackFunc3(f: (UInt16, UInt, UInt, Int, Float, UInt16) -> F3_Ret) -> F3_Ret {
    return f(45065, 8506742096411295359, 8619375465417625458, 5288917394772427257, 5678138, 33467)
}

@frozen
public struct F4_Ret
{
    public let f0 : UInt64;
    public let f1 : UInt32;
    public let f2 : UInt64;
}

public func swiftCallbackFunc4(f: (Int64, UInt16, Int32, UInt16, Int, Double, UInt16, Float, Int32, UInt16, Int8, Float, UInt64, Int16, Double, Int8, Int8, Int32, Int, Int32, Int64, Int64) -> F4_Ret) -> F4_Ret {
    return f(8771527078890676837, 18667, 224631333, 13819, 8888237425788084647, 2677321682649925, 50276, 2703201, 545337834, 11190, 112, 4053251, 7107857019164433129, -3092, 2176685406663423, 57, -61, 866840318, 5927291145767969522, 1818333546, 6272248211765159948, 6555966806846053216)
}

@frozen
public struct F5_Ret
{
    public let f0 : UInt64;
    public let f1 : Int32;
    public let f2 : Int;
    public let f3 : Float;
    public let f4 : Int16;
    public let f5 : UInt64;
}

public func swiftCallbackFunc5(f: (Int32, UInt16, UInt16, Int16, UInt8, Int8, UInt8, Int, UInt64, UInt64, Int64, Int16, Int16, Int64, UInt16, UInt8, UInt16) -> F5_Ret) -> F5_Ret {
    return f(359602150, 51495, 37765, 29410, 95, -104, 32, 8530952551906271255, 706266487837805024, 707905209555595641, 8386588676727568762, -8624, 26113, 8389143657021522019, 13337, 229, 51876)
}

public func swiftCallbackFunc6(f: (Int32, UInt32, UInt64, Int32, Int8, Int, Int, Int16, Int, UInt32, UInt64, UInt64, Int64, UInt32) -> UInt16) -> UInt16 {
    return f(743741783, 850236948, 5908745692727636656, 2106839818, 77, 291907785975160065, 3560129042279209151, -30568, 5730241035812482149, 18625011, 242340713355417257, 6962175160124965670, 2935089705514798822, 2051956645)
}

@frozen
public struct F7_Ret_S0
{
    public let f0 : Int;
}

@frozen
public struct F7_Ret
{
    public let f0 : Int8;
    public let f1 : Int8;
    public let f2 : UInt8;
    public let f3 : F7_Ret_S0;
    public let f4 : UInt32;
}

public func swiftCallbackFunc7(f: (UInt64, UInt8, Int16, UInt) -> F7_Ret) -> F7_Ret {
    return f(7625368278886567558, 70, 26780, 7739343395912136630)
}

public func swiftCallbackFunc8(f: (Float, UInt) -> UInt8) -> UInt8 {
    return f(6278007, 1620979945874429615)
}

@frozen
public struct F9_Ret
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : UInt64;
    public let f3 : UInt16;
}

public func swiftCallbackFunc9(f: (Int8, Int, Int16, Int64, Double, Double, Int, UInt16, UInt16, Float, Float, UInt16, UInt32, Int16, Int32, Int32, UInt64, Int16, Int64, Int, UInt8, UInt16, Int16, Int, Int16) -> F9_Ret) -> F9_Ret {
    return f(17, 4720638462358523954, 30631, 8206569929240962953, 1359667226908383, 3776001892555053, 747160900180286726, 12700, 53813, 7860389, 1879743, 61400, 1962814337, 17992, 677814589, 1019483263, 6326265259403184370, -14633, 4127072498763789519, 4008108205305320386, 128, 21189, 32104, 384827814282870543, 20647)
}

