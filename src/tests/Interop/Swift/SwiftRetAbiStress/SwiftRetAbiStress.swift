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

