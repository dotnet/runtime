// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

struct HasherFNV1a {

    private var hash: UInt = 14_695_981_039_346_656_037
    private let prime: UInt = 1_099_511_628_211

    mutating func combine<T>(_ val: T) {
        for byte in withUnsafeBytes(of: val, Array.init) {
            hash ^= UInt(byte)
            hash = hash &* prime
        }
    }

    func finalize() -> Int {
        Int(truncatingIfNeeded: hash)
    }
}

@frozen
public struct F0_S0
{
    public let f0 : Double;
    public let f1 : UInt32;
    public let f2 : UInt16;
}

@frozen
public struct F0_S1
{
    public let f0 : UInt64;
}

@frozen
public struct F0_S2
{
    public let f0 : Float;
}

public func swiftFunc0(a0: Int16, a1: Int32, a2: UInt64, a3: UInt16, a4: F0_S0, a5: F0_S1, a6: UInt8, a7: F0_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    return hasher.finalize()
}

@frozen
public struct F1_S0
{
    public let f0 : Int64;
    public let f1 : Double;
    public let f2 : Int8;
    public let f3 : Int32;
    public let f4 : UInt16;
}

@frozen
public struct F1_S1
{
    public let f0 : UInt8;
}

@frozen
public struct F1_S2
{
    public let f0 : Int16;
}

public func swiftFunc1(a0: F1_S0, a1: UInt8, a2: F1_S1, a3: F1_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a0.f4);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3.f0);
    return hasher.finalize()
}

@frozen
public struct F2_S0
{
    public let f0 : Int;
    public let f1 : UInt;
}

@frozen
public struct F2_S1
{
    public let f0 : Int64;
    public let f1 : Int32;
    public let f2 : Int16;
    public let f3 : Int64;
    public let f4 : UInt16;
}

@frozen
public struct F2_S2_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F2_S2_S0
{
    public let f0 : F2_S2_S0_S0;
}

@frozen
public struct F2_S2
{
    public let f0 : F2_S2_S0;
}

@frozen
public struct F2_S3
{
    public let f0 : UInt8;
}

@frozen
public struct F2_S4
{
    public let f0 : Int32;
    public let f1 : UInt;
}

@frozen
public struct F2_S5
{
    public let f0 : Float;
}

public func swiftFunc2(a0: Int64, a1: Int16, a2: Int32, a3: F2_S0, a4: UInt8, a5: Int32, a6: F2_S1, a7: F2_S2, a8: UInt16, a9: Float, a10: F2_S3, a11: F2_S4, a12: F2_S5, a13: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a6.f4);
    hasher.combine(a7.f0.f0.f0);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a11.f0);
    hasher.combine(a11.f1);
    hasher.combine(a12.f0);
    hasher.combine(a13);
    return hasher.finalize()
}

@frozen
public struct F3_S0_S0
{
    public let f0 : Int;
    public let f1 : UInt32;
}

@frozen
public struct F3_S0
{
    public let f0 : Int8;
    public let f1 : F3_S0_S0;
    public let f2 : UInt32;
}

@frozen
public struct F3_S1
{
    public let f0 : Int64;
    public let f1 : Float;
}

@frozen
public struct F3_S2
{
    public let f0 : Float;
}

@frozen
public struct F3_S3
{
    public let f0 : UInt8;
    public let f1 : Int;
}

@frozen
public struct F3_S4
{
    public let f0 : UInt;
    public let f1 : Float;
    public let f2 : UInt16;
}

@frozen
public struct F3_S5
{
    public let f0 : UInt32;
    public let f1 : Int64;
}

@frozen
public struct F3_S6_S0
{
    public let f0 : Int16;
    public let f1 : UInt8;
}

@frozen
public struct F3_S6
{
    public let f0 : F3_S6_S0;
    public let f1 : Int8;
    public let f2 : UInt8;
}

@frozen
public struct F3_S7
{
    public let f0 : UInt64;
}

public func swiftFunc3(a0: Int, a1: F3_S0, a2: F3_S1, a3: Double, a4: Int, a5: F3_S2, a6: F3_S3, a7: F3_S4, a8: F3_S5, a9: UInt16, a10: Int32, a11: F3_S6, a12: Int, a13: F3_S7) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1.f0);
    hasher.combine(a1.f1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11.f0.f0);
    hasher.combine(a11.f0.f1);
    hasher.combine(a11.f1);
    hasher.combine(a11.f2);
    hasher.combine(a12);
    hasher.combine(a13.f0);
    return hasher.finalize()
}

@frozen
public struct F4_S0
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : Int16;
}

@frozen
public struct F4_S1_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F4_S1
{
    public let f0 : F4_S1_S0;
    public let f1 : Float;
}

@frozen
public struct F4_S2_S0
{
    public let f0 : Int;
}

@frozen
public struct F4_S2
{
    public let f0 : F4_S2_S0;
    public let f1 : Int;
}

@frozen
public struct F4_S3
{
    public let f0 : UInt64;
    public let f1 : UInt64;
    public let f2 : Int64;
}

public func swiftFunc4(a0: Int, a1: F4_S0, a2: UInt, a3: UInt64, a4: Int8, a5: Double, a6: F4_S1, a7: UInt8, a8: Int32, a9: UInt32, a10: UInt64, a11: F4_S2, a12: Int16, a13: Int, a14: F4_S3, a15: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0.f0);
    hasher.combine(a6.f1);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11.f0.f0);
    hasher.combine(a11.f1);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14.f0);
    hasher.combine(a14.f1);
    hasher.combine(a14.f2);
    hasher.combine(a15);
    return hasher.finalize()
}

@frozen
public struct F5_S0
{
    public let f0 : UInt;
}

public func swiftFunc5(a0: UInt, a1: UInt64, a2: UInt8, a3: F5_S0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    return hasher.finalize()
}

@frozen
public struct F6_S0
{
    public let f0 : Int32;
    public let f1 : Int;
    public let f2 : UInt8;
}

@frozen
public struct F6_S1
{
    public let f0 : Int;
    public let f1 : Float;
}

@frozen
public struct F6_S2_S0
{
    public let f0 : Double;
}

@frozen
public struct F6_S2
{
    public let f0 : F6_S2_S0;
    public let f1 : UInt16;
}

@frozen
public struct F6_S3
{
    public let f0 : Double;
    public let f1 : Double;
    public let f2 : UInt64;
}

@frozen
public struct F6_S4
{
    public let f0 : Int8;
}

@frozen
public struct F6_S5
{
    public let f0 : Int16;
}

public func swiftFunc6(a0: Int64, a1: F6_S0, a2: F6_S1, a3: UInt, a4: UInt8, a5: Int32, a6: F6_S2, a7: Float, a8: Int16, a9: F6_S3, a10: UInt16, a11: Double, a12: UInt32, a13: F6_S4, a14: F6_S5) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0.f0);
    hasher.combine(a6.f1);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a9.f1);
    hasher.combine(a9.f2);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13.f0);
    hasher.combine(a14.f0);
    return hasher.finalize()
}

@frozen
public struct F7_S0
{
    public let f0 : Int16;
    public let f1 : Int;
}

@frozen
public struct F7_S1
{
    public let f0 : UInt8;
}

public func swiftFunc7(a0: Int64, a1: Int, a2: UInt8, a3: F7_S0, a4: F7_S1, a5: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F8_S0
{
    public let f0 : Int32;
}

public func swiftFunc8(a0: UInt16, a1: UInt, a2: UInt16, a3: UInt64, a4: F8_S0, a5: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F9_S0
{
    public let f0 : Double;
}

@frozen
public struct F9_S1
{
    public let f0 : Int32;
}

public func swiftFunc9(a0: Int64, a1: Float, a2: F9_S0, a3: UInt16, a4: F9_S1, a5: UInt16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F10_S0
{
    public let f0 : Int64;
    public let f1 : UInt32;
}

@frozen
public struct F10_S1
{
    public let f0 : Float;
    public let f1 : UInt8;
    public let f2 : UInt;
}

@frozen
public struct F10_S2
{
    public let f0 : UInt;
    public let f1 : UInt64;
}

@frozen
public struct F10_S3
{
    public let f0 : Float;
}

@frozen
public struct F10_S4
{
    public let f0 : Int64;
}

public func swiftFunc10(a0: UInt16, a1: UInt16, a2: F10_S0, a3: UInt64, a4: Float, a5: Int8, a6: Int64, a7: UInt64, a8: Int64, a9: Float, a10: Int32, a11: Int32, a12: Int64, a13: UInt64, a14: F10_S1, a15: Int64, a16: F10_S2, a17: F10_S3, a18: F10_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14.f0);
    hasher.combine(a14.f1);
    hasher.combine(a14.f2);
    hasher.combine(a15);
    hasher.combine(a16.f0);
    hasher.combine(a16.f1);
    hasher.combine(a17.f0);
    hasher.combine(a18.f0);
    return hasher.finalize()
}

@frozen
public struct F11_S0
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : UInt64;
    public let f3 : Int16;
}

@frozen
public struct F11_S1
{
    public let f0 : UInt;
}

@frozen
public struct F11_S2
{
    public let f0 : Int16;
}

@frozen
public struct F11_S3_S0
{
    public let f0 : Float;
}

@frozen
public struct F11_S3
{
    public let f0 : F11_S3_S0;
}

public func swiftFunc11(a0: Int, a1: UInt64, a2: UInt8, a3: Int16, a4: F11_S0, a5: F11_S1, a6: UInt16, a7: Double, a8: Int, a9: UInt32, a10: F11_S2, a11: F11_S3, a12: Int8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a4.f3);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a11.f0.f0);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F12_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F12_S1
{
    public let f0 : UInt8;
}

@frozen
public struct F12_S2
{
    public let f0 : UInt;
}

public func swiftFunc12(a0: UInt8, a1: Int32, a2: F12_S0, a3: Int8, a4: F12_S1, a5: F12_S2, a6: UInt32, a7: Int16, a8: Int8, a9: Int8, a10: UInt32, a11: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    return hasher.finalize()
}

@frozen
public struct F13_S0_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F13_S0_S0
{
    public let f0 : F13_S0_S0_S0;
}

@frozen
public struct F13_S0
{
    public let f0 : Int8;
    public let f1 : F13_S0_S0;
}

@frozen
public struct F13_S1_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F13_S1
{
    public let f0 : F13_S1_S0;
}

public func swiftFunc13(a0: Int8, a1: Double, a2: F13_S0, a3: F13_S1, a4: Int8, a5: Double) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1.f0.f0);
    hasher.combine(a3.f0.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F14_S0
{
    public let f0 : Int;
}

public func swiftFunc14(a0: Int8, a1: Int, a2: F14_S0, a3: Float, a4: UInt) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4);
    return hasher.finalize()
}

@frozen
public struct F15_S0
{
    public let f0 : Float;
    public let f1 : Int16;
    public let f2 : UInt8;
    public let f3 : Int64;
    public let f4 : Double;
}

@frozen
public struct F15_S1_S0
{
    public let f0 : Int8;
}

@frozen
public struct F15_S1
{
    public let f0 : UInt32;
    public let f1 : F15_S1_S0;
    public let f2 : UInt;
    public let f3 : Int32;
}

public func swiftFunc15(a0: F15_S0, a1: UInt64, a2: UInt32, a3: UInt, a4: UInt64, a5: Int16, a6: F15_S1, a7: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a0.f4);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1.f0);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F16_S0_S0
{
    public let f0 : Double;
}

@frozen
public struct F16_S0
{
    public let f0 : Int;
    public let f1 : Int;
    public let f2 : F16_S0_S0;
}

@frozen
public struct F16_S1
{
    public let f0 : Int16;
    public let f1 : UInt64;
    public let f2 : UInt32;
}

@frozen
public struct F16_S2
{
    public let f0 : UInt8;
    public let f1 : UInt64;
    public let f2 : Float;
}

@frozen
public struct F16_S3
{
    public let f0 : Int32;
}

public func swiftFunc16(a0: UInt64, a1: F16_S0, a2: F16_S1, a3: UInt16, a4: Int16, a5: F16_S2, a6: F16_S3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2.f0);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2);
    hasher.combine(a6.f0);
    return hasher.finalize()
}

@frozen
public struct F17_S0
{
    public let f0 : Int16;
}

@frozen
public struct F17_S1
{
    public let f0 : Int64;
    public let f1 : UInt;
    public let f2 : UInt64;
}

@frozen
public struct F17_S2
{
    public let f0 : Int8;
}

@frozen
public struct F17_S3
{
    public let f0 : Int8;
    public let f1 : UInt32;
}

@frozen
public struct F17_S4
{
    public let f0 : UInt64;
}

@frozen
public struct F17_S5
{
    public let f0 : Int64;
}

public func swiftFunc17(a0: F17_S0, a1: Int8, a2: F17_S1, a3: Int8, a4: UInt, a5: F17_S2, a6: Int64, a7: F17_S3, a8: F17_S4, a9: F17_S5) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a8.f0);
    hasher.combine(a9.f0);
    return hasher.finalize()
}

@frozen
public struct F18_S0_S0
{
    public let f0 : UInt16;
    public let f1 : Int16;
}

@frozen
public struct F18_S0
{
    public let f0 : UInt32;
    public let f1 : F18_S0_S0;
    public let f2 : UInt16;
}

@frozen
public struct F18_S1
{
    public let f0 : Int;
    public let f1 : Int;
}

@frozen
public struct F18_S2_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F18_S2
{
    public let f0 : UInt64;
    public let f1 : Int64;
    public let f2 : UInt8;
    public let f3 : F18_S2_S0;
}

public func swiftFunc18(a0: UInt8, a1: Double, a2: F18_S0, a3: F18_S1, a4: UInt16, a5: Int64, a6: UInt64, a7: F18_S2, a8: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1.f0);
    hasher.combine(a2.f1.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3.f0);
    hasher.combine(a8);
    return hasher.finalize()
}

@frozen
public struct F19_S0
{
    public let f0 : Int;
    public let f1 : Double;
    public let f2 : UInt16;
}

public func swiftFunc19(a0: UInt, a1: F19_S0, a2: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    return hasher.finalize()
}

@frozen
public struct F20_S0
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : UInt64;
    public let f3 : UInt32;
    public let f4 : UInt64;
}

@frozen
public struct F20_S1
{
    public let f0 : Int64;
}

public func swiftFunc20(a0: Int8, a1: F20_S0, a2: UInt64, a3: Int, a4: F20_S1, a5: UInt8, a6: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a1.f4);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    hasher.combine(a6);
    return hasher.finalize()
}

@frozen
public struct F21_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F21_S1
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : UInt8;
    public let f3 : Int16;
}

@frozen
public struct F21_S2
{
    public let f0 : Int8;
    public let f1 : UInt64;
    public let f2 : Int64;
    public let f3 : UInt8;
}

@frozen
public struct F21_S3
{
    public let f0 : Double;
    public let f1 : Int;
}

public func swiftFunc21(a0: UInt64, a1: Int8, a2: UInt, a3: Double, a4: Float, a5: Int, a6: F21_S0, a7: F21_S1, a8: UInt16, a9: F21_S2, a10: UInt8, a11: F21_S3, a12: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a9.f1);
    hasher.combine(a9.f2);
    hasher.combine(a9.f3);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    hasher.combine(a11.f1);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F22_S0
{
    public let f0 : UInt16;
    public let f1 : UInt32;
    public let f2 : Int16;
    public let f3 : Float;
}

@frozen
public struct F22_S1
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : UInt8;
    public let f3 : Int;
    public let f4 : Int;
}

@frozen
public struct F22_S2_S0
{
    public let f0 : Int8;
}

@frozen
public struct F22_S2
{
    public let f0 : Int32;
    public let f1 : Int32;
    public let f2 : UInt32;
    public let f3 : UInt8;
    public let f4 : F22_S2_S0;
}

@frozen
public struct F22_S3
{
    public let f0 : Int16;
    public let f1 : Double;
    public let f2 : Double;
    public let f3 : Int32;
}

public func swiftFunc22(a0: Int8, a1: Int32, a2: F22_S0, a3: F22_S1, a4: F22_S2, a5: UInt64, a6: F22_S3, a7: UInt) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a3.f3);
    hasher.combine(a3.f4);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a4.f3);
    hasher.combine(a4.f4.f0);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F23_S0
{
    public let f0 : UInt32;
    public let f1 : Int16;
}

@frozen
public struct F23_S1
{
    public let f0 : UInt;
    public let f1 : UInt32;
}

@frozen
public struct F23_S2
{
    public let f0 : Double;
    public let f1 : UInt32;
    public let f2 : Int32;
    public let f3 : UInt8;
}

public func swiftFunc23(a0: F23_S0, a1: F23_S1, a2: F23_S2, a3: Double, a4: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a3);
    hasher.combine(a4);
    return hasher.finalize()
}

@frozen
public struct F24_S0
{
    public let f0 : Int8;
    public let f1 : Int32;
}

@frozen
public struct F24_S1
{
    public let f0 : Int8;
}

@frozen
public struct F24_S2
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : Double;
    public let f3 : UInt;
}

@frozen
public struct F24_S3
{
    public let f0 : Int;
}

public func swiftFunc24(a0: F24_S0, a1: F24_S1, a2: F24_S2, a3: F24_S3, a4: UInt, a5: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a3.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F25_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct F25_S0
{
    public let f0 : Float;
    public let f1 : F25_S0_S0;
    public let f2 : UInt32;
}

@frozen
public struct F25_S1
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : Float;
}

@frozen
public struct F25_S2
{
    public let f0 : Int64;
    public let f1 : UInt16;
}

@frozen
public struct F25_S3
{
    public let f0 : UInt64;
}

@frozen
public struct F25_S4
{
    public let f0 : UInt16;
}

public func swiftFunc25(a0: Float, a1: F25_S0, a2: Int64, a3: UInt8, a4: F25_S1, a5: Int, a6: F25_S2, a7: Int32, a8: Int32, a9: UInt, a10: UInt64, a11: F25_S3, a12: F25_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1.f0);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    hasher.combine(a12.f0);
    return hasher.finalize()
}

@frozen
public struct F26_S0
{
    public let f0 : Double;
}

public func swiftFunc26(a0: UInt16, a1: Double, a2: Int64, a3: F26_S0, a4: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a4);
    return hasher.finalize()
}

@frozen
public struct F27_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F27_S0
{
    public let f0 : UInt16;
    public let f1 : F27_S0_S0;
    public let f2 : Double;
}

@frozen
public struct F27_S1
{
    public let f0 : Int;
    public let f1 : Int8;
    public let f2 : Int16;
    public let f3 : UInt8;
}

@frozen
public struct F27_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F27_S3
{
    public let f0 : UInt64;
    public let f1 : UInt32;
}

@frozen
public struct F27_S4
{
    public let f0 : UInt8;
}

public func swiftFunc27(a0: F27_S0, a1: Double, a2: Double, a3: Int8, a4: Int8, a5: F27_S1, a6: Int16, a7: F27_S2, a8: Int8, a9: UInt16, a10: F27_S3, a11: F27_S4, a12: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2);
    hasher.combine(a5.f3);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a10.f1);
    hasher.combine(a11.f0);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F28_S0
{
    public let f0 : Double;
    public let f1 : Int16;
    public let f2 : Double;
    public let f3 : UInt64;
}

@frozen
public struct F28_S1
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : UInt64;
    public let f3 : Float;
}

@frozen
public struct F28_S2
{
    public let f0 : Double;
    public let f1 : UInt64;
}

@frozen
public struct F28_S3
{
    public let f0 : Int16;
    public let f1 : UInt64;
    public let f2 : Double;
    public let f3 : Int32;
}

@frozen
public struct F28_S4
{
    public let f0 : Int;
}

public func swiftFunc28(a0: UInt8, a1: UInt16, a2: F28_S0, a3: F28_S1, a4: F28_S2, a5: UInt64, a6: Int32, a7: Int64, a8: Double, a9: UInt16, a10: F28_S3, a11: F28_S4, a12: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a3.f3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a10.f1);
    hasher.combine(a10.f2);
    hasher.combine(a10.f3);
    hasher.combine(a11.f0);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F29_S0
{
    public let f0 : Int32;
    public let f1 : Float;
    public let f2 : Int16;
}

@frozen
public struct F29_S1
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : UInt;
}

@frozen
public struct F29_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F29_S3
{
    public let f0 : Int64;
    public let f1 : Int64;
}

public func swiftFunc29(a0: Int8, a1: F29_S0, a2: Int32, a3: UInt, a4: F29_S1, a5: UInt64, a6: F29_S2, a7: Int16, a8: Int64, a9: UInt32, a10: UInt64, a11: Int, a12: F29_S3, a13: UInt8, a14: Int8, a15: Double) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12.f0);
    hasher.combine(a12.f1);
    hasher.combine(a13);
    hasher.combine(a14);
    hasher.combine(a15);
    return hasher.finalize()
}

@frozen
public struct F30_S0
{
    public let f0 : UInt;
    public let f1 : Float;
}

@frozen
public struct F30_S1
{
    public let f0 : UInt64;
    public let f1 : UInt8;
    public let f2 : Double;
    public let f3 : Int;
}

@frozen
public struct F30_S2_S0
{
    public let f0 : Int16;
    public let f1 : Int16;
}

@frozen
public struct F30_S2_S1
{
    public let f0 : Int64;
}

@frozen
public struct F30_S2
{
    public let f0 : F30_S2_S0;
    public let f1 : F30_S2_S1;
}

@frozen
public struct F30_S3
{
    public let f0 : Int8;
    public let f1 : UInt8;
    public let f2 : UInt64;
    public let f3 : UInt32;
}

@frozen
public struct F30_S4
{
    public let f0 : UInt16;
}

public func swiftFunc30(a0: UInt16, a1: Int16, a2: UInt16, a3: F30_S0, a4: F30_S1, a5: F30_S2, a6: UInt64, a7: Int32, a8: UInt, a9: F30_S3, a10: UInt16, a11: F30_S4, a12: Int8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a4.f3);
    hasher.combine(a5.f0.f0);
    hasher.combine(a5.f0.f1);
    hasher.combine(a5.f1.f0);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a9.f1);
    hasher.combine(a9.f2);
    hasher.combine(a9.f3);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F31_S0
{
    public let f0 : Int;
    public let f1 : Float;
    public let f2 : UInt32;
    public let f3 : Int;
}

public func swiftFunc31(a0: Int64, a1: F31_S0, a2: UInt32, a3: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a2);
    hasher.combine(a3);
    return hasher.finalize()
}

@frozen
public struct F32_S0
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : Int64;
}

@frozen
public struct F32_S1_S0
{
    public let f0 : UInt;
}

@frozen
public struct F32_S1
{
    public let f0 : UInt8;
    public let f1 : F32_S1_S0;
}

@frozen
public struct F32_S2
{
    public let f0 : UInt32;
    public let f1 : UInt8;
    public let f2 : UInt;
}

@frozen
public struct F32_S3_S0
{
    public let f0 : UInt;
}

@frozen
public struct F32_S3
{
    public let f0 : UInt64;
    public let f1 : F32_S3_S0;
    public let f2 : UInt64;
}

@frozen
public struct F32_S4
{
    public let f0 : Double;
    public let f1 : Int64;
    public let f2 : Int64;
    public let f3 : Float;
}

public func swiftFunc32(a0: UInt64, a1: F32_S0, a2: Double, a3: F32_S1, a4: F32_S2, a5: UInt64, a6: Float, a7: F32_S3, a8: F32_S4, a9: UInt32, a10: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1.f0);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1.f0);
    hasher.combine(a7.f2);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a8.f2);
    hasher.combine(a8.f3);
    hasher.combine(a9);
    hasher.combine(a10);
    return hasher.finalize()
}

@frozen
public struct F33_S0
{
    public let f0 : Int8;
    public let f1 : UInt8;
}

@frozen
public struct F33_S1
{
    public let f0 : UInt16;
    public let f1 : UInt8;
    public let f2 : Int64;
}

@frozen
public struct F33_S2_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F33_S2
{
    public let f0 : F33_S2_S0;
    public let f1 : UInt;
    public let f2 : Float;
    public let f3 : Double;
    public let f4 : UInt16;
}

@frozen
public struct F33_S3
{
    public let f0 : UInt;
}

public func swiftFunc33(a0: Float, a1: F33_S0, a2: UInt64, a3: Int64, a4: F33_S1, a5: UInt16, a6: UInt, a7: UInt16, a8: F33_S2, a9: F33_S3, a10: Int) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8.f0.f0);
    hasher.combine(a8.f1);
    hasher.combine(a8.f2);
    hasher.combine(a8.f3);
    hasher.combine(a8.f4);
    hasher.combine(a9.f0);
    hasher.combine(a10);
    return hasher.finalize()
}

@frozen
public struct F34_S0
{
    public let f0 : UInt8;
}

public func swiftFunc34(a0: Int64, a1: F34_S0, a2: UInt, a3: UInt, a4: UInt8, a5: Double) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F35_S0
{
    public let f0 : Int16;
}

@frozen
public struct F35_S1_S0
{
    public let f0 : UInt16;
    public let f1 : Int8;
}

@frozen
public struct F35_S1
{
    public let f0 : Int64;
    public let f1 : F35_S1_S0;
    public let f2 : Float;
}

@frozen
public struct F35_S2
{
    public let f0 : UInt64;
    public let f1 : Int8;
    public let f2 : UInt32;
    public let f3 : Int64;
}

@frozen
public struct F35_S3_S0_S0
{
    public let f0 : UInt32;
    public let f1 : UInt8;
}

@frozen
public struct F35_S3_S0
{
    public let f0 : UInt16;
    public let f1 : F35_S3_S0_S0;
    public let f2 : Double;
}

@frozen
public struct F35_S3
{
    public let f0 : F35_S3_S0;
    public let f1 : UInt32;
}

@frozen
public struct F35_S4
{
    public let f0 : Float;
}

public func swiftFunc35(a0: UInt8, a1: F35_S0, a2: UInt8, a3: UInt8, a4: F35_S1, a5: Int32, a6: F35_S2, a7: Int, a8: UInt32, a9: F35_S3, a10: F35_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1.f0);
    hasher.combine(a4.f1.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9.f0.f0);
    hasher.combine(a9.f0.f1.f0);
    hasher.combine(a9.f0.f1.f1);
    hasher.combine(a9.f0.f2);
    hasher.combine(a9.f1);
    hasher.combine(a10.f0);
    return hasher.finalize()
}

@frozen
public struct F36_S0
{
    public let f0 : UInt64;
    public let f1 : Int8;
}

@frozen
public struct F36_S1
{
    public let f0 : Int64;
    public let f1 : UInt;
    public let f2 : Int;
    public let f3 : Int32;
}

@frozen
public struct F36_S2
{
    public let f0 : Int;
}

@frozen
public struct F36_S3_S0
{
    public let f0 : Float;
}

@frozen
public struct F36_S3
{
    public let f0 : Int64;
    public let f1 : Int8;
    public let f2 : F36_S3_S0;
}

@frozen
public struct F36_S4
{
    public let f0 : UInt;
    public let f1 : Int64;
    public let f2 : Double;
    public let f3 : Double;
}

@frozen
public struct F36_S5
{
    public let f0 : UInt8;
    public let f1 : UInt8;
}

@frozen
public struct F36_S6
{
    public let f0 : UInt16;
}

public func swiftFunc36(a0: F36_S0, a1: Double, a2: UInt64, a3: F36_S1, a4: F36_S2, a5: F36_S3, a6: F36_S4, a7: Float, a8: F36_S5, a9: UInt8, a10: Double, a11: F36_S6) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a3.f3);
    hasher.combine(a4.f0);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2.f0);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a7);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    return hasher.finalize()
}

@frozen
public struct F37_S0
{
    public let f0 : Int32;
}

@frozen
public struct F37_S1
{
    public let f0 : UInt32;
    public let f1 : UInt32;
    public let f2 : Float;
}

@frozen
public struct F37_S2
{
    public let f0 : Int32;
    public let f1 : UInt32;
    public let f2 : Double;
    public let f3 : UInt;
}

@frozen
public struct F37_S3_S0
{
    public let f0 : Int;
}

@frozen
public struct F37_S3
{
    public let f0 : F37_S3_S0;
}

public func swiftFunc37(a0: Int, a1: UInt64, a2: UInt32, a3: Int32, a4: Int8, a5: UInt8, a6: UInt64, a7: F37_S0, a8: F37_S1, a9: Int16, a10: F37_S2, a11: UInt, a12: F37_S3, a13: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a8.f2);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a10.f1);
    hasher.combine(a10.f2);
    hasher.combine(a10.f3);
    hasher.combine(a11);
    hasher.combine(a12.f0.f0);
    hasher.combine(a13);
    return hasher.finalize()
}

@frozen
public struct F38_S0
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : Int16;
}

@frozen
public struct F38_S1
{
    public let f0 : Int32;
}

@frozen
public struct F38_S2
{
    public let f0 : UInt;
}

public func swiftFunc38(a0: UInt32, a1: Int32, a2: F38_S0, a3: F38_S1, a4: F38_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3.f0);
    hasher.combine(a4.f0);
    return hasher.finalize()
}

@frozen
public struct F39_S0_S0
{
    public let f0 : UInt;
}

@frozen
public struct F39_S0_S1
{
    public let f0 : Int32;
}

@frozen
public struct F39_S0
{
    public let f0 : Int;
    public let f1 : Int64;
    public let f2 : UInt32;
    public let f3 : F39_S0_S0;
    public let f4 : F39_S0_S1;
}

@frozen
public struct F39_S1
{
    public let f0 : UInt;
    public let f1 : Double;
}

public func swiftFunc39(a0: UInt, a1: UInt, a2: F39_S0, a3: F39_S1, a4: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3.f0);
    hasher.combine(a2.f4.f0);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4);
    return hasher.finalize()
}

public func swiftFunc40(a0: Int32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    return hasher.finalize()
}

@frozen
public struct F41_S0
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : UInt16;
}

@frozen
public struct F41_S1
{
    public let f0 : UInt16;
    public let f1 : UInt64;
    public let f2 : Int8;
    public let f3 : Float;
    public let f4 : UInt64;
}

@frozen
public struct F41_S2_S0_S0
{
    public let f0 : Int16;
}

@frozen
public struct F41_S2_S0
{
    public let f0 : F41_S2_S0_S0;
}

@frozen
public struct F41_S2
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : UInt64;
    public let f3 : Float;
    public let f4 : F41_S2_S0;
}

public func swiftFunc41(a0: Float, a1: F41_S0, a2: F41_S1, a3: F41_S2, a4: UInt32, a5: UInt, a6: UInt32, a7: Int, a8: Int8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a2.f4);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a3.f3);
    hasher.combine(a3.f4.f0.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    return hasher.finalize()
}

@frozen
public struct F42_S0
{
    public let f0 : UInt32;
    public let f1 : UInt64;
    public let f2 : UInt64;
}

@frozen
public struct F42_S1
{
    public let f0 : Double;
    public let f1 : Double;
}

@frozen
public struct F42_S2_S0
{
    public let f0 : Int;
}

@frozen
public struct F42_S2
{
    public let f0 : UInt8;
    public let f1 : Int64;
    public let f2 : F42_S2_S0;
    public let f3 : Int;
}

@frozen
public struct F42_S3_S0
{
    public let f0 : Int16;
}

@frozen
public struct F42_S3
{
    public let f0 : Float;
    public let f1 : F42_S3_S0;
}

@frozen
public struct F42_S4
{
    public let f0 : UInt32;
}

@frozen
public struct F42_S5_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F42_S5
{
    public let f0 : F42_S5_S0;
}

@frozen
public struct F42_S6
{
    public let f0 : UInt;
}

public func swiftFunc42(a0: F42_S0, a1: F42_S1, a2: UInt16, a3: F42_S2, a4: F42_S3, a5: F42_S4, a6: F42_S5, a7: F42_S6, a8: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2.f0);
    hasher.combine(a3.f3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1.f0);
    hasher.combine(a5.f0);
    hasher.combine(a6.f0.f0);
    hasher.combine(a7.f0);
    hasher.combine(a8);
    return hasher.finalize()
}

@frozen
public struct F43_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F43_S0
{
    public let f0 : F43_S0_S0;
}

public func swiftFunc43(a0: Int64, a1: UInt8, a2: Int8, a3: Float, a4: Int64, a5: Int, a6: F43_S0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0.f0);
    return hasher.finalize()
}

@frozen
public struct F44_S0
{
    public let f0 : UInt64;
}

public func swiftFunc44(a0: F44_S0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    return hasher.finalize()
}

@frozen
public struct F45_S0
{
    public let f0 : Double;
    public let f1 : Int;
}

@frozen
public struct F45_S1_S0
{
    public let f0 : Double;
}

@frozen
public struct F45_S1_S1
{
    public let f0 : Float;
}

@frozen
public struct F45_S1
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : F45_S1_S0;
    public let f3 : F45_S1_S1;
}

@frozen
public struct F45_S2
{
    public let f0 : UInt64;
    public let f1 : Float;
    public let f2 : UInt16;
}

public func swiftFunc45(a0: F45_S0, a1: F45_S1, a2: F45_S2, a3: Int) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2.f0);
    hasher.combine(a1.f3.f0);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    return hasher.finalize()
}

@frozen
public struct F46_S0
{
    public let f0 : Int64;
    public let f1 : UInt8;
    public let f2 : UInt;
    public let f3 : Int8;
}

@frozen
public struct F46_S1
{
    public let f0 : UInt8;
}

@frozen
public struct F46_S2
{
    public let f0 : Int;
}

@frozen
public struct F46_S3
{
    public let f0 : UInt64;
    public let f1 : Int64;
}

@frozen
public struct F46_S4
{
    public let f0 : Int16;
    public let f1 : Int32;
    public let f2 : UInt32;
}

@frozen
public struct F46_S5
{
    public let f0 : UInt64;
}

public func swiftFunc46(a0: F46_S0, a1: F46_S1, a2: Int8, a3: Float, a4: F46_S2, a5: Int16, a6: F46_S3, a7: Int16, a8: Float, a9: F46_S4, a10: UInt16, a11: Float, a12: Int8, a13: F46_S5) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a9.f1);
    hasher.combine(a9.f2);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13.f0);
    return hasher.finalize()
}

@frozen
public struct F47_S0_S0
{
    public let f0 : UInt16;
    public let f1 : Int8;
}

@frozen
public struct F47_S0
{
    public let f0 : F47_S0_S0;
    public let f1 : UInt16;
    public let f2 : UInt;
    public let f3 : Int64;
}

@frozen
public struct F47_S1
{
    public let f0 : Int64;
    public let f1 : UInt8;
}

public func swiftFunc47(a0: Int, a1: F47_S0, a2: F47_S1, a3: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0.f0);
    hasher.combine(a1.f0.f1);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3);
    return hasher.finalize()
}

public func swiftFunc48(a0: Int8, a1: UInt32, a2: Int16, a3: Float, a4: Int, a5: Float, a6: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    return hasher.finalize()
}

@frozen
public struct F49_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F49_S1_S0
{
    public let f0 : Int16;
}

@frozen
public struct F49_S1_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F49_S1
{
    public let f0 : F49_S1_S0;
    public let f1 : Int32;
    public let f2 : F49_S1_S1;
    public let f3 : UInt;
}

@frozen
public struct F49_S2
{
    public let f0 : UInt16;
    public let f1 : UInt8;
    public let f2 : Float;
    public let f3 : Int64;
}

@frozen
public struct F49_S3
{
    public let f0 : Int32;
    public let f1 : Float;
}

@frozen
public struct F49_S4
{
    public let f0 : UInt32;
    public let f1 : Int;
    public let f2 : Int;
}

public func swiftFunc49(a0: UInt64, a1: UInt8, a2: F49_S0, a3: F49_S1, a4: UInt, a5: UInt32, a6: Double, a7: F49_S2, a8: F49_S3, a9: Int8, a10: F49_S4, a11: Int32, a12: UInt64, a13: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3.f0.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2.f0);
    hasher.combine(a3.f3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a10.f1);
    hasher.combine(a10.f2);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13);
    return hasher.finalize()
}

@frozen
public struct F50_S0
{
    public let f0 : Int8;
    public let f1 : Int16;
    public let f2 : Int32;
    public let f3 : UInt32;
}

@frozen
public struct F50_S1
{
    public let f0 : Int32;
}

public func swiftFunc50(a0: F50_S0, a1: UInt8, a2: F50_S1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    return hasher.finalize()
}

public func swiftFunc51(a0: UInt16, a1: Int8, a2: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    return hasher.finalize()
}

public func swiftFunc52(a0: UInt8, a1: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    return hasher.finalize()
}

@frozen
public struct F53_S0_S0
{
    public let f0 : Int64;
    public let f1 : UInt;
}

@frozen
public struct F53_S0
{
    public let f0 : UInt64;
    public let f1 : F53_S0_S0;
    public let f2 : Int16;
    public let f3 : UInt8;
}

@frozen
public struct F53_S1_S0
{
    public let f0 : Int64;
}

@frozen
public struct F53_S1
{
    public let f0 : F53_S1_S0;
}

@frozen
public struct F53_S2
{
    public let f0 : UInt8;
    public let f1 : UInt64;
    public let f2 : Double;
}

public func swiftFunc53(a0: F53_S0, a1: UInt, a2: UInt64, a3: Float, a4: UInt32, a5: F53_S1, a6: F53_S2, a7: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a0.f1.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0.f0);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F54_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F54_S0
{
    public let f0 : F54_S0_S0;
}

@frozen
public struct F54_S1
{
    public let f0 : UInt32;
}

public func swiftFunc54(a0: Int8, a1: Int32, a2: UInt32, a3: F54_S0, a4: Float, a5: UInt8, a6: F54_S1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    return hasher.finalize()
}

@frozen
public struct F55_S0
{
    public let f0 : Double;
}

public func swiftFunc55(a0: F55_S0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    return hasher.finalize()
}

@frozen
public struct F56_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F56_S0
{
    public let f0 : Float;
    public let f1 : F56_S0_S0;
}

@frozen
public struct F56_S1_S0
{
    public let f0 : Int16;
}

@frozen
public struct F56_S1
{
    public let f0 : F56_S1_S0;
    public let f1 : Double;
    public let f2 : UInt;
    public let f3 : UInt32;
}

@frozen
public struct F56_S2
{
    public let f0 : Int16;
    public let f1 : Int16;
}

@frozen
public struct F56_S3
{
    public let f0 : UInt16;
}

@frozen
public struct F56_S4
{
    public let f0 : UInt;
}

public func swiftFunc56(a0: F56_S0, a1: F56_S1, a2: F56_S2, a3: F56_S3, a4: F56_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a1.f0.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3.f0);
    hasher.combine(a4.f0);
    return hasher.finalize()
}

@frozen
public struct F57_S0
{
    public let f0 : Int8;
    public let f1 : UInt32;
}

@frozen
public struct F57_S1_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F57_S1_S1
{
    public let f0 : UInt;
}

@frozen
public struct F57_S1
{
    public let f0 : F57_S1_S0;
    public let f1 : F57_S1_S1;
    public let f2 : Int16;
}

@frozen
public struct F57_S2
{
    public let f0 : UInt;
}

public func swiftFunc57(a0: UInt32, a1: F57_S0, a2: F57_S1, a3: UInt, a4: F57_S2, a5: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2.f0.f0);
    hasher.combine(a2.f1.f0);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F58_S0
{
    public let f0 : Int64;
}

@frozen
public struct F58_S1
{
    public let f0 : UInt;
    public let f1 : Int;
    public let f2 : UInt;
    public let f3 : UInt16;
}

public func swiftFunc58(a0: UInt8, a1: UInt8, a2: Int, a3: F58_S0, a4: Float, a5: UInt64, a6: Int8, a7: F58_S1, a8: UInt16, a9: Int64, a10: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    return hasher.finalize()
}

@frozen
public struct F59_S0
{
    public let f0 : UInt;
    public let f1 : UInt8;
    public let f2 : Float;
    public let f3 : Int;
}

@frozen
public struct F59_S1
{
    public let f0 : UInt8;
    public let f1 : Int32;
}

@frozen
public struct F59_S2
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : Int8;
}

@frozen
public struct F59_S3
{
    public let f0 : Int8;
    public let f1 : Float;
    public let f2 : Int32;
}

@frozen
public struct F59_S4_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F59_S4
{
    public let f0 : F59_S4_S0;
}

public func swiftFunc59(a0: F59_S0, a1: Float, a2: UInt32, a3: F59_S1, a4: F59_S2, a5: UInt16, a6: Float, a7: Int, a8: Int, a9: UInt, a10: UInt, a11: Int16, a12: F59_S3, a13: F59_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12.f0);
    hasher.combine(a12.f1);
    hasher.combine(a12.f2);
    hasher.combine(a13.f0.f0);
    return hasher.finalize()
}

@frozen
public struct F60_S0
{
    public let f0 : Int64;
}

@frozen
public struct F60_S1
{
    public let f0 : UInt32;
}

public func swiftFunc60(a0: Int32, a1: Int8, a2: Int32, a3: UInt16, a4: Float, a5: F60_S0, a6: F60_S1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a6.f0);
    return hasher.finalize()
}

@frozen
public struct F61_S0
{
    public let f0 : UInt16;
    public let f1 : Int32;
    public let f2 : Int8;
}

@frozen
public struct F61_S1
{
    public let f0 : Double;
    public let f1 : Int;
}

@frozen
public struct F61_S2
{
    public let f0 : Int;
    public let f1 : Int8;
    public let f2 : Float;
    public let f3 : UInt16;
    public let f4 : Float;
}

@frozen
public struct F61_S3
{
    public let f0 : UInt32;
    public let f1 : UInt64;
    public let f2 : UInt;
    public let f3 : UInt;
}

@frozen
public struct F61_S4_S0
{
    public let f0 : UInt8;
    public let f1 : UInt64;
}

@frozen
public struct F61_S4
{
    public let f0 : F61_S4_S0;
    public let f1 : Int64;
}

public func swiftFunc61(a0: F61_S0, a1: UInt8, a2: Float, a3: F61_S1, a4: Int8, a5: Int64, a6: F61_S2, a7: F61_S3, a8: F61_S4, a9: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a6.f4);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3);
    hasher.combine(a8.f0.f0);
    hasher.combine(a8.f0.f1);
    hasher.combine(a8.f1);
    hasher.combine(a9);
    return hasher.finalize()
}

@frozen
public struct F62_S0
{
    public let f0 : Int64;
}

@frozen
public struct F62_S1
{
    public let f0 : Float;
}

public func swiftFunc62(a0: F62_S0, a1: Int16, a2: Int32, a3: F62_S1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    return hasher.finalize()
}

@frozen
public struct F63_S0
{
    public let f0 : Int;
}

public func swiftFunc63(a0: F63_S0) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    return hasher.finalize()
}

@frozen
public struct F64_S0
{
    public let f0 : Double;
    public let f1 : UInt16;
    public let f2 : Int32;
    public let f3 : Int;
    public let f4 : Double;
}

@frozen
public struct F64_S1
{
    public let f0 : Int32;
    public let f1 : Float;
    public let f2 : UInt32;
}

public func swiftFunc64(a0: Double, a1: F64_S0, a2: UInt8, a3: F64_S1, a4: Int32, a5: UInt64, a6: Int8, a7: Int8, a8: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a1.f4);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    return hasher.finalize()
}

public func swiftFunc65(a0: Float, a1: Float, a2: UInt, a3: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    return hasher.finalize()
}

@frozen
public struct F66_S0
{
    public let f0 : Int64;
}

@frozen
public struct F66_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F66_S1
{
    public let f0 : F66_S1_S0;
    public let f1 : Float;
}

@frozen
public struct F66_S2
{
    public let f0 : Double;
    public let f1 : UInt8;
}

@frozen
public struct F66_S3
{
    public let f0 : UInt;
}

public func swiftFunc66(a0: F66_S0, a1: F66_S1, a2: F66_S2, a3: F66_S3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a1.f0.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a3.f0);
    return hasher.finalize()
}

@frozen
public struct F67_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F67_S1_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F67_S1_S0
{
    public let f0 : F67_S1_S0_S0;
}

@frozen
public struct F67_S1
{
    public let f0 : F67_S1_S0;
    public let f1 : UInt32;
    public let f2 : Int16;
}

public func swiftFunc67(a0: UInt64, a1: UInt32, a2: UInt16, a3: Int8, a4: F67_S0, a5: UInt64, a6: F67_S1, a7: UInt, a8: UInt64, a9: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5);
    hasher.combine(a6.f0.f0.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    return hasher.finalize()
}

@frozen
public struct F68_S0_S0_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F68_S0_S0
{
    public let f0 : F68_S0_S0_S0;
}

@frozen
public struct F68_S0
{
    public let f0 : F68_S0_S0;
}

@frozen
public struct F68_S1
{
    public let f0 : UInt64;
    public let f1 : UInt16;
}

@frozen
public struct F68_S2
{
    public let f0 : UInt;
    public let f1 : Int;
    public let f2 : UInt64;
    public let f3 : Double;
}

@frozen
public struct F68_S3
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : UInt32;
    public let f3 : UInt;
}

@frozen
public struct F68_S4
{
    public let f0 : Int32;
}

public func swiftFunc68(a0: UInt16, a1: Int64, a2: Int16, a3: UInt64, a4: Int8, a5: Int32, a6: UInt8, a7: F68_S0, a8: UInt8, a9: F68_S1, a10: Int16, a11: F68_S2, a12: Int16, a13: Int16, a14: F68_S3, a15: F68_S4) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0.f0.f0);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a9.f1);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    hasher.combine(a11.f1);
    hasher.combine(a11.f2);
    hasher.combine(a11.f3);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14.f0);
    hasher.combine(a14.f1);
    hasher.combine(a14.f2);
    hasher.combine(a14.f3);
    hasher.combine(a15.f0);
    return hasher.finalize()
}

@frozen
public struct F69_S0
{
    public let f0 : UInt32;
    public let f1 : UInt;
}

@frozen
public struct F69_S1_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F69_S1_S0
{
    public let f0 : F69_S1_S0_S0;
    public let f1 : Int8;
}

@frozen
public struct F69_S1
{
    public let f0 : F69_S1_S0;
    public let f1 : UInt;
    public let f2 : Int;
}

@frozen
public struct F69_S2
{
    public let f0 : Float;
    public let f1 : UInt32;
    public let f2 : UInt16;
    public let f3 : Int8;
}

@frozen
public struct F69_S3
{
    public let f0 : UInt8;
    public let f1 : Double;
}

@frozen
public struct F69_S4
{
    public let f0 : Double;
}

@frozen
public struct F69_S5
{
    public let f0 : UInt64;
}

public func swiftFunc69(a0: F69_S0, a1: F69_S1, a2: Int, a3: Int, a4: UInt16, a5: Int16, a6: Double, a7: F69_S2, a8: F69_S3, a9: F69_S4, a10: Int, a11: Int32, a12: F69_S5, a13: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0.f0.f0);
    hasher.combine(a1.f0.f1);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a7.f3);
    hasher.combine(a8.f0);
    hasher.combine(a8.f1);
    hasher.combine(a9.f0);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12.f0);
    hasher.combine(a13);
    return hasher.finalize()
}

@frozen
public struct F70_S0
{
    public let f0 : Float;
    public let f1 : Int64;
}

@frozen
public struct F70_S1
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : Int16;
}

@frozen
public struct F70_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F70_S3
{
    public let f0 : UInt16;
}

public func swiftFunc70(a0: UInt64, a1: F70_S0, a2: UInt16, a3: Int8, a4: Float, a5: F70_S1, a6: Int, a7: F70_S2, a8: F70_S3, a9: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a8.f0);
    hasher.combine(a9);
    return hasher.finalize()
}

@frozen
public struct F71_S0
{
    public let f0 : Int;
}

@frozen
public struct F71_S1
{
    public let f0 : UInt64;
}

public func swiftFunc71(a0: Int64, a1: F71_S0, a2: Int8, a3: F71_S1, a4: Float, a5: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F72_S0_S0
{
    public let f0 : Int;
    public let f1 : Double;
}

@frozen
public struct F72_S0
{
    public let f0 : F72_S0_S0;
    public let f1 : UInt32;
}

@frozen
public struct F72_S1
{
    public let f0 : Int;
}

@frozen
public struct F72_S2
{
    public let f0 : Double;
}

public func swiftFunc72(a0: F72_S0, a1: F72_S1, a2: F72_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0.f0);
    hasher.combine(a0.f0.f1);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a2.f0);
    return hasher.finalize()
}

public func swiftFunc73(a0: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    return hasher.finalize()
}

@frozen
public struct F74_S0
{
    public let f0 : UInt8;
    public let f1 : UInt8;
    public let f2 : Double;
    public let f3 : UInt8;
}

@frozen
public struct F74_S1
{
    public let f0 : Int16;
    public let f1 : UInt16;
    public let f2 : Int64;
    public let f3 : UInt;
}

@frozen
public struct F74_S2
{
    public let f0 : Int16;
    public let f1 : Double;
    public let f2 : Float;
}

@frozen
public struct F74_S3
{
    public let f0 : Int16;
}

public func swiftFunc74(a0: F74_S0, a1: F74_S1, a2: Int32, a3: F74_S2, a4: Int, a5: Int64, a6: Int16, a7: Int32, a8: F74_S3, a9: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8.f0);
    hasher.combine(a9);
    return hasher.finalize()
}

@frozen
public struct F75_S0_S0_S0
{
    public let f0 : Int16;
}

@frozen
public struct F75_S0_S0
{
    public let f0 : F75_S0_S0_S0;
}

@frozen
public struct F75_S0
{
    public let f0 : F75_S0_S0;
    public let f1 : Double;
    public let f2 : Int32;
}

@frozen
public struct F75_S1_S0_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F75_S1_S0
{
    public let f0 : UInt;
    public let f1 : F75_S1_S0_S0;
    public let f2 : Int64;
}

@frozen
public struct F75_S1
{
    public let f0 : F75_S1_S0;
    public let f1 : Int;
}

@frozen
public struct F75_S2
{
    public let f0 : UInt64;
}

public func swiftFunc75(a0: F75_S0, a1: Double, a2: Int, a3: UInt, a4: Int, a5: F75_S1, a6: F75_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0.f0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0.f0);
    hasher.combine(a5.f0.f1.f0);
    hasher.combine(a5.f0.f2);
    hasher.combine(a5.f1);
    hasher.combine(a6.f0);
    return hasher.finalize()
}

@frozen
public struct F76_S0
{
    public let f0 : Int;
}

@frozen
public struct F76_S1
{
    public let f0 : UInt64;
    public let f1 : Int32;
    public let f2 : Int16;
}

@frozen
public struct F76_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F76_S3
{
    public let f0 : Int;
}

public func swiftFunc76(a0: Double, a1: Int64, a2: UInt16, a3: Float, a4: Float, a5: F76_S0, a6: Int16, a7: F76_S1, a8: Int64, a9: UInt64, a10: UInt16, a11: UInt8, a12: Int8, a13: Int, a14: Int64, a15: Int8, a16: Int8, a17: Int16, a18: UInt16, a19: F76_S2, a20: F76_S3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a7.f1);
    hasher.combine(a7.f2);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14);
    hasher.combine(a15);
    hasher.combine(a16);
    hasher.combine(a17);
    hasher.combine(a18);
    hasher.combine(a19.f0);
    hasher.combine(a20.f0);
    return hasher.finalize()
}

@frozen
public struct F77_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct F77_S0
{
    public let f0 : UInt64;
    public let f1 : F77_S0_S0;
    public let f2 : Int8;
}

@frozen
public struct F77_S1
{
    public let f0 : UInt64;
    public let f1 : Int;
    public let f2 : Int32;
}

@frozen
public struct F77_S2_S0_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F77_S2_S0
{
    public let f0 : F77_S2_S0_S0;
}

@frozen
public struct F77_S2
{
    public let f0 : F77_S2_S0;
    public let f1 : Int16;
    public let f2 : Int8;
    public let f3 : UInt8;
}

@frozen
public struct F77_S3
{
    public let f0 : Int;
    public let f1 : Int;
    public let f2 : Int;
    public let f3 : Int16;
}

@frozen
public struct F77_S4
{
    public let f0 : Double;
    public let f1 : Int8;
    public let f2 : UInt32;
    public let f3 : Int16;
    public let f4 : UInt32;
}

@frozen
public struct F77_S5
{
    public let f0 : UInt;
}

public func swiftFunc77(a0: F77_S0, a1: Int16, a2: F77_S1, a3: UInt32, a4: F77_S2, a5: F77_S3, a6: F77_S4, a7: UInt64, a8: F77_S5, a9: UInt16, a10: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    hasher.combine(a4.f0.f0.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a4.f3);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2);
    hasher.combine(a5.f3);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a6.f4);
    hasher.combine(a7);
    hasher.combine(a8.f0);
    hasher.combine(a9);
    hasher.combine(a10);
    return hasher.finalize()
}

@frozen
public struct F78_S0
{
    public let f0 : UInt16;
    public let f1 : UInt;
}

public func swiftFunc78(a0: F78_S0, a1: UInt64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1);
    return hasher.finalize()
}

@frozen
public struct F79_S0
{
    public let f0 : Double;
}

public func swiftFunc79(a0: UInt32, a1: F79_S0, a2: Int16, a3: Double) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3);
    return hasher.finalize()
}

@frozen
public struct F80_S0
{
    public let f0 : UInt64;
    public let f1 : Double;
}

@frozen
public struct F80_S1_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F80_S1
{
    public let f0 : Int32;
    public let f1 : UInt16;
    public let f2 : UInt32;
    public let f3 : F80_S1_S0;
}

@frozen
public struct F80_S2
{
    public let f0 : UInt64;
    public let f1 : Int64;
    public let f2 : UInt32;
    public let f3 : UInt16;
}

@frozen
public struct F80_S3_S0_S0
{
    public let f0 : Int;
    public let f1 : Int64;
    public let f2 : UInt64;
}

@frozen
public struct F80_S3_S0
{
    public let f0 : F80_S3_S0_S0;
    public let f1 : UInt32;
}

@frozen
public struct F80_S3
{
    public let f0 : F80_S3_S0;
    public let f1 : Int32;
}

@frozen
public struct F80_S4_S0
{
    public let f0 : Float;
}

@frozen
public struct F80_S4
{
    public let f0 : F80_S4_S0;
}

public func swiftFunc80(a0: F80_S0, a1: F80_S1, a2: UInt16, a3: Int64, a4: F80_S2, a5: Double, a6: UInt64, a7: Int32, a8: F80_S3, a9: F80_S4, a10: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3.f0);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a4.f2);
    hasher.combine(a4.f3);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8.f0.f0.f0);
    hasher.combine(a8.f0.f0.f1);
    hasher.combine(a8.f0.f0.f2);
    hasher.combine(a8.f0.f1);
    hasher.combine(a8.f1);
    hasher.combine(a9.f0.f0);
    hasher.combine(a10);
    return hasher.finalize()
}

@frozen
public struct F81_S0
{
    public let f0 : Double;
    public let f1 : UInt64;
    public let f2 : UInt32;
    public let f3 : UInt8;
    public let f4 : UInt8;
}

@frozen
public struct F81_S1
{
    public let f0 : UInt32;
}

public func swiftFunc81(a0: F81_S0, a1: Int32, a2: Float, a3: F81_S1) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a0.f4);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    return hasher.finalize()
}

@frozen
public struct F82_S0
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : UInt64;
    public let f3 : Int8;
}

@frozen
public struct F82_S1_S0
{
    public let f0 : Int64;
}

@frozen
public struct F82_S1
{
    public let f0 : Int;
    public let f1 : Int32;
    public let f2 : F82_S1_S0;
}

@frozen
public struct F82_S2
{
    public let f0 : Int;
    public let f1 : Int64;
    public let f2 : UInt32;
    public let f3 : UInt16;
    public let f4 : Int64;
}

@frozen
public struct F82_S3
{
    public let f0 : UInt8;
}

public func swiftFunc82(a0: F82_S0, a1: F82_S1, a2: F82_S2, a3: UInt32, a4: Int, a5: F82_S3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2.f0);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a2.f3);
    hasher.combine(a2.f4);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    return hasher.finalize()
}

@frozen
public struct F83_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F83_S0
{
    public let f0 : F83_S0_S0;
    public let f1 : Int;
    public let f2 : Float;
}

@frozen
public struct F83_S1_S0
{
    public let f0 : Double;
}

@frozen
public struct F83_S1_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F83_S1_S1
{
    public let f0 : F83_S1_S1_S0;
}

@frozen
public struct F83_S1
{
    public let f0 : UInt32;
    public let f1 : F83_S1_S0;
    public let f2 : F83_S1_S1;
}

@frozen
public struct F83_S2
{
    public let f0 : Int;
}

public func swiftFunc83(a0: Float, a1: F83_S0, a2: F83_S1, a3: Int16, a4: Int, a5: Float, a6: F83_S2, a7: UInt16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1.f0);
    hasher.combine(a2.f2.f0.f0);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F84_S0
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : UInt16;
    public let f3 : Int64;
    public let f4 : Int16;
}

@frozen
public struct F84_S1
{
    public let f0 : Int32;
}

@frozen
public struct F84_S2_S0
{
    public let f0 : UInt8;
    public let f1 : UInt64;
}

@frozen
public struct F84_S2
{
    public let f0 : UInt;
    public let f1 : F84_S2_S0;
    public let f2 : Int8;
    public let f3 : Double;
}

@frozen
public struct F84_S3
{
    public let f0 : UInt32;
}

@frozen
public struct F84_S4
{
    public let f0 : Float;
}

public func swiftFunc84(a0: F84_S0, a1: F84_S1, a2: UInt64, a3: F84_S2, a4: UInt32, a5: F84_S3, a6: UInt, a7: F84_S4, a8: UInt64, a9: UInt64, a10: UInt16, a11: Int16, a12: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a0.f4);
    hasher.combine(a1.f0);
    hasher.combine(a2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1.f0);
    hasher.combine(a3.f1.f1);
    hasher.combine(a3.f2);
    hasher.combine(a3.f3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11);
    hasher.combine(a12);
    return hasher.finalize()
}

@frozen
public struct F85_S0_S0_S0
{
    public let f0 : Float;
}

@frozen
public struct F85_S0_S0
{
    public let f0 : Int32;
    public let f1 : F85_S0_S0_S0;
}

@frozen
public struct F85_S0
{
    public let f0 : Float;
    public let f1 : F85_S0_S0;
    public let f2 : Int;
    public let f3 : Int64;
}

@frozen
public struct F85_S1
{
    public let f0 : UInt32;
    public let f1 : Int32;
}

@frozen
public struct F85_S2
{
    public let f0 : UInt;
}

public func swiftFunc85(a0: F85_S0, a1: F85_S1, a2: F85_S2, a3: Int8, a4: UInt32, a5: Int16) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a0.f1.f1.f0);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F86_S0
{
    public let f0 : Int32;
    public let f1 : Int64;
    public let f2 : Int32;
    public let f3 : UInt16;
}

@frozen
public struct F86_S1_S0
{
    public let f0 : UInt;
}

@frozen
public struct F86_S1
{
    public let f0 : F86_S1_S0;
    public let f1 : UInt16;
}

@frozen
public struct F86_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F86_S3
{
    public let f0 : Int16;
}

@frozen
public struct F86_S4
{
    public let f0 : Int;
}

@frozen
public struct F86_S5
{
    public let f0 : Int16;
}

public func swiftFunc86(a0: F86_S0, a1: Int, a2: Int, a3: UInt, a4: F86_S1, a5: F86_S2, a6: UInt64, a7: F86_S3, a8: F86_S4, a9: F86_S5) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0.f0);
    hasher.combine(a4.f1);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    hasher.combine(a7.f0);
    hasher.combine(a8.f0);
    hasher.combine(a9.f0);
    return hasher.finalize()
}

@frozen
public struct F87_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F87_S0
{
    public let f0 : F87_S0_S0;
    public let f1 : Float;
    public let f2 : Int64;
    public let f3 : Double;
}

@frozen
public struct F87_S1
{
    public let f0 : Int32;
}

@frozen
public struct F87_S2_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F87_S2
{
    public let f0 : F87_S2_S0;
}

@frozen
public struct F87_S3
{
    public let f0 : Int32;
}

public func swiftFunc87(a0: Int64, a1: F87_S0, a2: UInt, a3: UInt8, a4: Double, a5: Int16, a6: UInt64, a7: Double, a8: Float, a9: F87_S1, a10: Int64, a11: F87_S2, a12: F87_S3, a13: Float) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9.f0);
    hasher.combine(a10);
    hasher.combine(a11.f0.f0);
    hasher.combine(a12.f0);
    hasher.combine(a13);
    return hasher.finalize()
}

@frozen
public struct F88_S0
{
    public let f0 : UInt8;
    public let f1 : Int64;
    public let f2 : UInt64;
    public let f3 : Int;
}

@frozen
public struct F88_S1
{
    public let f0 : Int64;
    public let f1 : UInt8;
    public let f2 : UInt16;
}

@frozen
public struct F88_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F88_S3_S0
{
    public let f0 : Int;
}

@frozen
public struct F88_S3
{
    public let f0 : Int32;
    public let f1 : F88_S3_S0;
    public let f2 : Int8;
    public let f3 : UInt16;
}

@frozen
public struct F88_S4_S0
{
    public let f0 : Float;
}

@frozen
public struct F88_S4
{
    public let f0 : UInt16;
    public let f1 : UInt;
    public let f2 : Int8;
    public let f3 : Int;
    public let f4 : F88_S4_S0;
}

@frozen
public struct F88_S5
{
    public let f0 : Float;
}

@frozen
public struct F88_S6
{
    public let f0 : UInt32;
}

@frozen
public struct F88_S7_S0
{
    public let f0 : Int;
}

@frozen
public struct F88_S7
{
    public let f0 : F88_S7_S0;
}

public func swiftFunc88(a0: F88_S0, a1: Int8, a2: F88_S1, a3: UInt64, a4: F88_S2, a5: F88_S3, a6: F88_S4, a7: Int16, a8: F88_S5, a9: F88_S6, a10: F88_S7) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1.f0);
    hasher.combine(a5.f2);
    hasher.combine(a5.f3);
    hasher.combine(a6.f0);
    hasher.combine(a6.f1);
    hasher.combine(a6.f2);
    hasher.combine(a6.f3);
    hasher.combine(a6.f4.f0);
    hasher.combine(a7);
    hasher.combine(a8.f0);
    hasher.combine(a9.f0);
    hasher.combine(a10.f0.f0);
    return hasher.finalize()
}

@frozen
public struct F89_S0
{
    public let f0 : UInt8;
    public let f1 : Int8;
}

@frozen
public struct F89_S1
{
    public let f0 : Int32;
}

@frozen
public struct F89_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F89_S3
{
    public let f0 : Double;
    public let f1 : Double;
}

@frozen
public struct F89_S4
{
    public let f0 : UInt32;
}

public func swiftFunc89(a0: F89_S0, a1: F89_S1, a2: F89_S2, a3: UInt8, a4: F89_S3, a5: F89_S4, a6: Int32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1.f0);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a5.f0);
    hasher.combine(a6);
    return hasher.finalize()
}

@frozen
public struct F90_S0
{
    public let f0 : UInt16;
    public let f1 : Int;
}

@frozen
public struct F90_S1_S0
{
    public let f0 : Int;
}

@frozen
public struct F90_S1
{
    public let f0 : F90_S1_S0;
    public let f1 : UInt;
    public let f2 : Double;
}

@frozen
public struct F90_S2
{
    public let f0 : UInt64;
    public let f1 : Int;
    public let f2 : UInt16;
}

@frozen
public struct F90_S3_S0
{
    public let f0 : Int64;
}

@frozen
public struct F90_S3
{
    public let f0 : F90_S3_S0;
}

@frozen
public struct F90_S4
{
    public let f0 : Int64;
}

public func swiftFunc90(a0: F90_S0, a1: Int8, a2: F90_S1, a3: F90_S2, a4: F90_S3, a5: UInt32, a6: F90_S4, a7: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a1);
    hasher.combine(a2.f0.f0);
    hasher.combine(a2.f1);
    hasher.combine(a2.f2);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a3.f2);
    hasher.combine(a4.f0.f0);
    hasher.combine(a5);
    hasher.combine(a6.f0);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F91_S0_S0
{
    public let f0 : Int32;
}

@frozen
public struct F91_S0
{
    public let f0 : F91_S0_S0;
    public let f1 : UInt32;
    public let f2 : Int;
}

public func swiftFunc91(a0: F91_S0, a1: UInt8) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    return hasher.finalize()
}

@frozen
public struct F92_S0
{
    public let f0 : UInt16;
    public let f1 : UInt16;
}

@frozen
public struct F92_S1
{
    public let f0 : UInt64;
}

@frozen
public struct F92_S2
{
    public let f0 : UInt64;
    public let f1 : UInt64;
}

@frozen
public struct F92_S3
{
    public let f0 : UInt;
}

public func swiftFunc92(a0: Int16, a1: UInt64, a2: UInt, a3: Int64, a4: F92_S0, a5: Int64, a6: Double, a7: UInt8, a8: Int8, a9: UInt32, a10: Int8, a11: F92_S1, a12: UInt32, a13: Float, a14: UInt64, a15: UInt8, a16: Int32, a17: UInt32, a18: F92_S2, a19: F92_S3) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    hasher.combine(a4.f1);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10);
    hasher.combine(a11.f0);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14);
    hasher.combine(a15);
    hasher.combine(a16);
    hasher.combine(a17);
    hasher.combine(a18.f0);
    hasher.combine(a18.f1);
    hasher.combine(a19.f0);
    return hasher.finalize()
}

@frozen
public struct F93_S0
{
    public let f0 : Int32;
    public let f1 : UInt;
    public let f2 : Double;
}

@frozen
public struct F93_S1
{
    public let f0 : UInt32;
}

@frozen
public struct F93_S2
{
    public let f0 : Double;
}

public func swiftFunc93(a0: F93_S0, a1: Int, a2: F93_S1, a3: Double, a4: F93_S2) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2.f0);
    hasher.combine(a3);
    hasher.combine(a4.f0);
    return hasher.finalize()
}

public func swiftFunc94(a0: UInt64, a1: Int32, a2: UInt64, a3: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    return hasher.finalize()
}

@frozen
public struct F95_S0
{
    public let f0 : Int64;
}

public func swiftFunc95(a0: F95_S0, a1: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a1);
    return hasher.finalize()
}

@frozen
public struct F96_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F96_S0
{
    public let f0 : UInt64;
    public let f1 : Double;
    public let f2 : Double;
    public let f3 : F96_S0_S0;
}

@frozen
public struct F96_S1_S0_S0
{
    public let f0 : Double;
}

@frozen
public struct F96_S1_S0
{
    public let f0 : F96_S1_S0_S0;
}

@frozen
public struct F96_S1
{
    public let f0 : F96_S1_S0;
}

@frozen
public struct F96_S2
{
    public let f0 : UInt8;
    public let f1 : Float;
}

@frozen
public struct F96_S3
{
    public let f0 : UInt16;
}

@frozen
public struct F96_S4
{
    public let f0 : Int;
}

@frozen
public struct F96_S5_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F96_S5
{
    public let f0 : F96_S5_S0;
}

@frozen
public struct F96_S6
{
    public let f0 : UInt64;
}

public func swiftFunc96(a0: UInt16, a1: F96_S0, a2: F96_S1, a3: F96_S2, a4: UInt16, a5: UInt64, a6: Int, a7: Int32, a8: Int16, a9: UInt, a10: F96_S3, a11: Int16, a12: Int, a13: Int8, a14: Int32, a15: UInt32, a16: F96_S4, a17: F96_S5, a18: F96_S6) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1.f0);
    hasher.combine(a1.f1);
    hasher.combine(a1.f2);
    hasher.combine(a1.f3.f0);
    hasher.combine(a2.f0.f0.f0);
    hasher.combine(a3.f0);
    hasher.combine(a3.f1);
    hasher.combine(a4);
    hasher.combine(a5);
    hasher.combine(a6);
    hasher.combine(a7);
    hasher.combine(a8);
    hasher.combine(a9);
    hasher.combine(a10.f0);
    hasher.combine(a11);
    hasher.combine(a12);
    hasher.combine(a13);
    hasher.combine(a14);
    hasher.combine(a15);
    hasher.combine(a16.f0);
    hasher.combine(a17.f0.f0);
    hasher.combine(a18.f0);
    return hasher.finalize()
}

@frozen
public struct F97_S0
{
    public let f0 : Float;
    public let f1 : Float;
    public let f2 : Int;
    public let f3 : Int;
    public let f4 : Int;
}

public func swiftFunc97(a0: Int8, a1: Int32, a2: UInt8, a3: UInt32, a4: UInt8, a5: F97_S0, a6: Int8, a7: Int) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5.f0);
    hasher.combine(a5.f1);
    hasher.combine(a5.f2);
    hasher.combine(a5.f3);
    hasher.combine(a5.f4);
    hasher.combine(a6);
    hasher.combine(a7);
    return hasher.finalize()
}

@frozen
public struct F98_S0_S0_S0
{
    public let f0 : Float;
}

@frozen
public struct F98_S0_S0
{
    public let f0 : UInt;
    public let f1 : F98_S0_S0_S0;
}

@frozen
public struct F98_S0
{
    public let f0 : Int64;
    public let f1 : F98_S0_S0;
    public let f2 : UInt;
}

public func swiftFunc98(a0: F98_S0, a1: UInt16, a2: UInt16, a3: Int16, a4: Int8, a5: UInt32) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1.f0);
    hasher.combine(a0.f1.f1.f0);
    hasher.combine(a0.f2);
    hasher.combine(a1);
    hasher.combine(a2);
    hasher.combine(a3);
    hasher.combine(a4);
    hasher.combine(a5);
    return hasher.finalize()
}

@frozen
public struct F99_S0
{
    public let f0 : UInt64;
    public let f1 : UInt16;
    public let f2 : Float;
    public let f3 : UInt64;
}

@frozen
public struct F99_S1_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F99_S1
{
    public let f0 : F99_S1_S0;
}

public func swiftFunc99(a0: F99_S0, a1: Int8, a2: F99_S1, a3: Int64) -> Int {
    var hasher = HasherFNV1a()
    hasher.combine(a0.f0);
    hasher.combine(a0.f1);
    hasher.combine(a0.f2);
    hasher.combine(a0.f3);
    hasher.combine(a1);
    hasher.combine(a2.f0.f0);
    hasher.combine(a3);
    return hasher.finalize()
}

