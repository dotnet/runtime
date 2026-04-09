// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Foundation

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

public func swiftCallbackFunc0(f: (Int16, Int32, UInt64, UInt16, F0_S0, F0_S1, UInt8, F0_S2) -> Int32) -> Int32 {
    return f(-17813, 318006528, 1195162122024233590, 60467, F0_S0(f0: 2239972725713766, f1: 1404066621, f2: 29895), F0_S1(f0: 7923486769850554262), 217, F0_S2(f0: 2497655))
}

@frozen
public struct F1_S0
{
    public let f0 : UInt16;
    public let f1 : UInt8;
}

@frozen
public struct F1_S1
{
    public let f0 : UInt8;
    public let f1 : UInt64;
    public let f2 : Int16;
    public let f3 : Float;
    public let f4 : Float;
}

@frozen
public struct F1_S2_S0
{
    public let f0 : UInt32;
    public let f1 : Double;
}

@frozen
public struct F1_S2
{
    public let f0 : Int8;
    public let f1 : UInt;
    public let f2 : F1_S2_S0;
    public let f3 : Int;
}

@frozen
public struct F1_S3
{
    public let f0 : UInt16;
}

@frozen
public struct F1_S4
{
    public let f0 : Int;
}

@frozen
public struct F1_S5_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F1_S5
{
    public let f0 : F1_S5_S0;
}

public func swiftCallbackFunc1(f: (Int64, Double, Int8, F1_S0, F1_S1, F1_S2, UInt8, Int8, Int64, F1_S3, UInt, F1_S4, F1_S5, Int) -> UInt8) -> UInt8 {
    return f(7920511243396412395, 1396130721334528, -55, F1_S0(f0: 33758, f1: 103), F1_S1(f0: 201, f1: 7390774039746135757, f2: 14699, f3: 7235330, f4: 7189013), F1_S2(f0: 37, f1: 3310322731568932038, f2: F1_S2_S0(f0: 1100328218, f1: 1060779460203640), f3: 8325292022909418877), 137, 82, 1197537325837505041, F1_S3(f0: 46950), 8181828233622947597, F1_S4(f0: 1851182205030289056), F1_S5(f0: F1_S5_S0(f0: 1971014225)), 6437995407675718392)
}

@frozen
public struct F2_S0
{
    public let f0 : Int32;
    public let f1 : UInt;
    public let f2 : Float;
}

@frozen
public struct F2_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F2_S1
{
    public let f0 : Int64;
    public let f1 : UInt16;
    public let f2 : F2_S1_S0;
    public let f3 : Int;
    public let f4 : Double;
}

@frozen
public struct F2_S2
{
    public let f0 : Float;
    public let f1 : Int32;
    public let f2 : UInt16;
    public let f3 : Int8;
}

@frozen
public struct F2_S3_S0
{
    public let f0 : Int8;
}

@frozen
public struct F2_S3
{
    public let f0 : F2_S3_S0;
}

public func swiftCallbackFunc2(f: (F2_S0, F2_S1, F2_S2, Float, UInt64, F2_S3) -> Int8) -> Int8 {
    return f(F2_S0(f0: 1860840185, f1: 5407074783834178811, f2: 6261766), F2_S1(f0: 4033972792915237065, f1: 22825, f2: F2_S1_S0(f0: 44574), f3: 4536911485304731630, f4: 4282944015147385), F2_S2(f0: 2579193, f1: 586252933, f2: 47002, f3: 71), 3225929, 3599444831393612282, F2_S3(f0: F2_S3_S0(f0: 13)))
}

@frozen
public struct F3_S0_S0
{
    public let f0 : UInt;
}

@frozen
public struct F3_S0
{
    public let f0 : F3_S0_S0;
}

@frozen
public struct F3_S1
{
    public let f0 : UInt32;
    public let f1 : Int64;
}

@frozen
public struct F3_S2_S0
{
    public let f0 : Int16;
    public let f1 : UInt8;
}

@frozen
public struct F3_S2
{
    public let f0 : F3_S2_S0;
    public let f1 : Int8;
    public let f2 : UInt8;
}

@frozen
public struct F3_S3
{
    public let f0 : UInt64;
    public let f1 : Int64;
}

@frozen
public struct F3_S4
{
    public let f0 : Int16;
}

@frozen
public struct F3_Ret
{
    public let f0 : UInt16;
    public let f1 : UInt8;
    public let f2 : UInt16;
    public let f3 : Float;
}

public func swiftCallbackFunc3(f: (F3_S0, Float, UInt16, F3_S1, UInt16, Int32, F3_S2, Int, F3_S3, F3_S4) -> F3_Ret) -> F3_Ret {
    return f(F3_S0(f0: F3_S0_S0(f0: 5610153900386943274)), 7736836, 31355, F3_S1(f0: 1159208572, f1: 2707818827451590538), 37580, 1453603418, F3_S2(f0: F3_S2_S0(f0: 699, f1: 46), f1: -125, f2: 92), 94557706586779834, F3_S3(f0: 2368015527878194540, f1: 5026404532195049271), F3_S4(f0: 21807))
}

@frozen
public struct F4_S0_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F4_S0
{
    public let f0 : F4_S0_S0;
    public let f1 : Float;
}

@frozen
public struct F4_Ret_S0
{
    public let f0 : Int;
}

@frozen
public struct F4_Ret
{
    public let f0 : Int32;
    public let f1 : F4_Ret_S0;
    public let f2 : Int;
    public let f3 : Int16;
    public let f4 : Int;
    public let f5 : UInt32;
}

public func swiftCallbackFunc4(f: (Double, F4_S0, UInt8, Int32, UInt32) -> F4_Ret) -> F4_Ret {
    return f(4282972206489588, F4_S0(f0: F4_S0_S0(f0: 611688063), f1: 877466), 53, 965123506, 1301067653)
}

@frozen
public struct F5_S0
{
    public let f0 : UInt;
    public let f1 : UInt32;
}

@frozen
public struct F5_S1_S0
{
    public let f0 : Int;
    public let f1 : UInt32;
}

@frozen
public struct F5_S1_S1
{
    public let f0 : Float;
}

@frozen
public struct F5_S1
{
    public let f0 : F5_S1_S0;
    public let f1 : F5_S1_S1;
}

@frozen
public struct F5_S2
{
    public let f0 : Double;
    public let f1 : Int8;
    public let f2 : Int;
}

@frozen
public struct F5_S3
{
    public let f0 : Int64;
    public let f1 : Double;
}

@frozen
public struct F5_S4
{
    public let f0 : UInt16;
}

@frozen
public struct F5_Ret
{
    public let f0 : Int16;
    public let f1 : Int32;
    public let f2 : Int32;
    public let f3 : UInt64;
    public let f4 : Int16;
}

public func swiftCallbackFunc5(f: (UInt8, Int16, UInt64, UInt, UInt, UInt64, UInt8, F5_S0, Int8, Int8, F5_S1, F5_S2, F5_S3, Double, F5_S4, UInt16, Float, Float, UInt16) -> F5_Ret) -> F5_Ret {
    return f(42, 18727, 3436765034579128495, 6305137336506323506, 6280137078630028944, 6252650621827449809, 129, F5_S0(f0: 6879980973426111678, f1: 1952654577), -34, 102, F5_S1(f0: F5_S1_S0(f0: 8389143657021522019, f1: 437030241), f1: F5_S1_S1(f0: 7522798)), F5_S2(f0: 523364011167530, f1: 16, f2: 3823439046574037759), F5_S3(f0: 3767260839267771462, f1: 1181031208183008), 2338830539621828, F5_S4(f0: 36276), 41286, 6683955, 6399917, 767)
}

@frozen
public struct F6_S0_S0
{
    public let f0 : Float;
}

@frozen
public struct F6_S0
{
    public let f0 : Int8;
    public let f1 : Int8;
    public let f2 : Int32;
    public let f3 : F6_S0_S0;
}

@frozen
public struct F6_S1
{
    public let f0 : Int32;
    public let f1 : UInt64;
    public let f2 : UInt64;
    public let f3 : UInt32;
}

@frozen
public struct F6_S2
{
    public let f0 : Int64;
    public let f1 : Int16;
    public let f2 : Int8;
}

@frozen
public struct F6_S3
{
    public let f0 : Float;
}

@frozen
public struct F6_Ret_S0
{
    public let f0 : Int64;
    public let f1 : UInt32;
}

@frozen
public struct F6_Ret
{
    public let f0 : F6_Ret_S0;
    public let f1 : UInt64;
    public let f2 : Float;
    public let f3 : Int8;
}

public func swiftCallbackFunc6(f: (Float, F6_S0, Int64, Int8, UInt16, UInt, UInt16, UInt64, F6_S1, Int16, F6_S2, F6_S3, UInt16) -> F6_Ret) -> F6_Ret {
    return f(2905241, F6_S0(f0: -27, f1: -77, f2: 1315779092, f3: F6_S0_S0(f0: 5373970)), 7022244764256789748, -110, 2074, 3560129042279209151, 2200, 5730241035812482149, F6_S1(f0: 18625011, f1: 242340713355417257, f2: 6962175160124965670, f3: 1983617839), -28374, F6_S2(f0: 6355748563312062178, f1: -23189, f2: 81), F6_S3(f0: 4547677), 6397)
}

@frozen
public struct F7_S0
{
    public let f0 : Float;
    public let f1 : Int64;
    public let f2 : UInt;
}

@frozen
public struct F7_S1
{
    public let f0 : Int16;
    public let f1 : UInt32;
    public let f2 : UInt32;
}

public func swiftCallbackFunc7(f: (Int64, UInt8, Double, UInt16, F7_S0, UInt8, Double, UInt32, F7_S1, Int32, Int32, Int, Int16, UInt16, Int, UInt64, UInt8, Int16) -> UInt16) -> UInt16 {
    return f(7625368278886567558, 70, 2146971972122530, 54991, F7_S0(f0: 1072132, f1: 3890459003549150599, f2: 56791000421908673), 227, 3248250571953113, 1138780108, F7_S1(f0: -22670, f1: 1796712687, f2: 304251857), 1288765591, 1382721790, 6746417265635727373, -15600, 47575, 7200793040165597188, 2304985873826892392, 99, -9993)
}

@frozen
public struct F8_S0
{
    public let f0 : Int16;
    public let f1 : Int16;
    public let f2 : UInt;
}

@frozen
public struct F8_S1
{
    public let f0 : Int64;
}

@frozen
public struct F8_Ret_S0
{
    public let f0 : Int32;
    public let f1 : UInt;
    public let f2 : Int;
}

@frozen
public struct F8_Ret
{
    public let f0 : Int64;
    public let f1 : F8_Ret_S0;
    public let f2 : Int;
    public let f3 : UInt32;
}

public func swiftCallbackFunc8(f: (F8_S0, F8_S1) -> F8_Ret) -> F8_Ret {
    return f(F8_S0(f0: 16278, f1: -31563, f2: 2171308312325435543), F8_S1(f0: 8923668560896309835))
}

@frozen
public struct F9_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F9_S0
{
    public let f0 : F9_S0_S0;
    public let f1 : Int16;
}

@frozen
public struct F9_S1_S0
{
    public let f0 : Int64;
    public let f1 : Int64;
}

@frozen
public struct F9_S1
{
    public let f0 : Int;
    public let f1 : F9_S1_S0;
    public let f2 : Float;
}

@frozen
public struct F9_S2
{
    public let f0 : UInt64;
    public let f1 : Double;
    public let f2 : Int16;
    public let f3 : Int8;
}

@frozen
public struct F9_S3_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F9_S3_S0
{
    public let f0 : F9_S3_S0_S0;
}

@frozen
public struct F9_S3
{
    public let f0 : Int8;
    public let f1 : F9_S3_S0;
}

@frozen
public struct F9_S4_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F9_S4
{
    public let f0 : F9_S4_S0;
    public let f1 : Int8;
}

@frozen
public struct F9_S5_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F9_S5
{
    public let f0 : UInt32;
    public let f1 : F9_S5_S0;
}

@frozen
public struct F9_S6
{
    public let f0 : Double;
}

public func swiftCallbackFunc9(f: (Int8, UInt8, Int64, F9_S0, F9_S1, F9_S2, Double, F9_S3, F9_S4, Double, F9_S5, F9_S6) -> UInt16) -> UInt16 {
    return f(17, 104, 8922699691031703191, F9_S0(f0: F9_S0_S0(f0: 123), f1: 31706), F9_S1(f0: 1804058604961822948, f1: F9_S1_S0(f0: 8772179036715198777, f1: 3320511540592563328), f2: 679540), F9_S2(f0: 8642590829466497926, f1: 4116322155252965, f2: 17992, f3: -48), 414017537937894, F9_S3(f0: 47, f1: F9_S3_S0(f0: F9_S3_S0_S0(f0: 7576380984563129085))), F9_S4(f0: F9_S4_S0(f0: 1356827400304742803), f1: -17), 4458031413035521, F9_S5(f0: 352075098, f1: F9_S5_S0(f0: 1840980094)), F9_S6(f0: 396957263013930))
}

@frozen
public struct F10_Ret
{
    public let f0 : Int64;
    public let f1 : UInt32;
    public let f2 : UInt16;
    public let f3 : UInt32;
}

public func swiftCallbackFunc10(f: (Int16) -> F10_Ret) -> F10_Ret {
    return f(-7168)
}

@frozen
public struct F11_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct F11_S0
{
    public let f0 : UInt32;
    public let f1 : F11_S0_S0;
    public let f2 : UInt;
    public let f3 : Int32;
    public let f4 : Int64;
}

@frozen
public struct F11_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F11_S1
{
    public let f0 : F11_S1_S0;
    public let f1 : Int16;
    public let f2 : UInt32;
    public let f3 : Int16;
}

@frozen
public struct F11_S2
{
    public let f0 : UInt8;
}

@frozen
public struct F11_Ret
{
    public let f0 : Int16;
    public let f1 : Int16;
    public let f2 : UInt8;
    public let f3 : Int64;
}

public func swiftCallbackFunc11(f: (UInt32, UInt, UInt64, Int16, F11_S0, Float, Int8, UInt16, F11_S1, UInt32, Int64, UInt32, F11_S2) -> F11_Ret) -> F11_Ret {
    return f(454751144, 1696592254558667577, 5831587230944972245, 15352, F11_S0(f0: 1306601347, f1: F11_S0_S0(f0: 123), f2: 3064471520018434938, f3: 272956246, f4: 3683518307106722029), 5606122, -126, 50801, F11_S1(f0: F11_S1_S0(f0: 63467), f1: -31828, f2: 2117176776, f3: -27265), 1879606687, 4981244336430926707, 1159924856, F11_S2(f0: 29))
}

@frozen
public struct F12_S0
{
    public let f0 : UInt64;
    public let f1 : Int8;
}

@frozen
public struct F12_S1_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F12_S1_S0
{
    public let f0 : F12_S1_S0_S0;
}

@frozen
public struct F12_S1
{
    public let f0 : UInt16;
    public let f1 : UInt32;
    public let f2 : F12_S1_S0;
}

@frozen
public struct F12_Ret
{
    public let f0 : UInt64;
    public let f1 : Int;
}

public func swiftCallbackFunc12(f: (F12_S0, Int16, UInt64, F12_S1, Int8) -> F12_Ret) -> F12_Ret {
    return f(F12_S0(f0: 3236871137735400659, f1: -123), -22828, 2132557792366642035, F12_S1(f0: 42520, f1: 879349060, f2: F12_S1_S0(f0: F12_S1_S0_S0(f0: 5694370973277919380))), -75)
}

@frozen
public struct F13_S0_S0
{
    public let f0 : Int64;
    public let f1 : Int64;
}

@frozen
public struct F13_S0
{
    public let f0 : F13_S0_S0;
    public let f1 : Float;
    public let f2 : Int16;
}

@frozen
public struct F13_S1
{
    public let f0 : Int;
    public let f1 : UInt64;
}

@frozen
public struct F13_S2_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F13_S2
{
    public let f0 : F13_S2_S0;
    public let f1 : Double;
}

@frozen
public struct F13_S3
{
    public let f0 : Float;
    public let f1 : Int8;
}

@frozen
public struct F13_S4
{
    public let f0 : Int;
}

public func swiftCallbackFunc13(f: (F13_S0, Int32, Int, UInt16, UInt, F13_S1, F13_S2, Int, Double, Int8, Float, Int, F13_S3, UInt, F13_S4) -> Double) -> Double {
    return f(F13_S0(f0: F13_S0_S0(f0: 9003727031576598067, f1: 8527798284445940986), f1: 3585628, f2: -12520), 1510815104, 5883331525294982326, 60738, 5291799143932627546, F13_S1(f0: 1949276559361384602, f1: 876048527237138968), F13_S2(f0: F13_S2_S0(f0: 67), f1: 2455575228564859), 2321408806345977320, 12750323283778, 46, 6774339, 5121910967292140178, F13_S3(f0: 8254279, f1: -7), 7533347207018595125, F13_S4(f0: 6605448167191082938))
}

@frozen
public struct F14_S0
{
    public let f0 : Int8;
    public let f1 : Float;
    public let f2 : UInt16;
}

@frozen
public struct F14_S1
{
    public let f0 : UInt64;
    public let f1 : UInt64;
}

public func swiftCallbackFunc14(f: (Int64, F14_S0, Int8, UInt64, F14_S1, Int) -> Int64) -> Int64 {
    return f(5547219684656041875, F14_S0(f0: -39, f1: 5768837, f2: 53063), -102, 5745438709817040873, F14_S1(f0: 2178706453119907411, f1: 4424726479787355131), 5693881223150438553)
}

@frozen
public struct F15_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F15_S1
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : UInt8;
    public let f3 : Int16;
}

@frozen
public struct F15_S2
{
    public let f0 : Int8;
    public let f1 : UInt64;
    public let f2 : Int64;
    public let f3 : UInt8;
}

@frozen
public struct F15_S3
{
    public let f0 : Double;
}

public func swiftCallbackFunc15(f: (UInt8, UInt16, UInt64, UInt64, Int8, UInt, Double, Float, Int, F15_S0, F15_S1, UInt16, F15_S2, UInt8, F15_S3) -> Int) -> Int {
    return f(0, 31081, 8814881608835743979, 4283853687332682681, 80, 7895994601265649979, 1855521542692398, 3235683, 215122646177738904, F15_S0(f0: 2044750195), F15_S1(f0: 1772412898183620625, f1: 131256973, f2: 153, f3: 25281), 50965, F15_S2(f0: -83, f1: 7751486385861474282, f2: 3744400479301818340, f3: 150), 179, F15_S3(f0: 3108143600787174))
}

@frozen
public struct F16_S0
{
    public let f0 : Int8;
    public let f1 : Int32;
    public let f2 : UInt16;
    public let f3 : UInt16;
    public let f4 : UInt32;
}

@frozen
public struct F16_S1
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : UInt8;
    public let f3 : Int;
    public let f4 : Int;
}

@frozen
public struct F16_S2_S0
{
    public let f0 : Int8;
}

@frozen
public struct F16_S2
{
    public let f0 : Int32;
    public let f1 : Int32;
    public let f2 : UInt32;
    public let f3 : UInt8;
    public let f4 : F16_S2_S0;
}

@frozen
public struct F16_S3
{
    public let f0 : Int16;
    public let f1 : Double;
    public let f2 : Double;
    public let f3 : Int32;
}

public func swiftCallbackFunc16(f: (F16_S0, Int16, Float, F16_S1, F16_S2, UInt64, F16_S3, UInt) -> Int8) -> Int8 {
    return f(F16_S0(f0: -59, f1: 1181591186, f2: 44834, f3: 28664, f4: 404461767), 2482, 2997348, F16_S1(f0: 22423, f1: -106, f2: 182, f3: 3784074551275084420, f4: 7092934571108982079), F16_S2(f0: 1835134709, f1: 246067261, f2: 1986526591, f3: 24, f4: F16_S2_S0(f0: -112)), 1465053746911704089, F16_S3(f0: -27636, f1: 1896887612303356, f2: 4263157082840190, f3: 774653659), 3755775782607884861)
}

@frozen
public struct F17_S0
{
    public let f0 : Int32;
    public let f1 : UInt;
}

@frozen
public struct F17_S1_S0
{
    public let f0 : Double;
    public let f1 : UInt32;
}

@frozen
public struct F17_S1
{
    public let f0 : F17_S1_S0;
    public let f1 : Int32;
    public let f2 : UInt8;
}

@frozen
public struct F17_S2
{
    public let f0 : UInt32;
}

public func swiftCallbackFunc17(f: (UInt32, F17_S0, F17_S1, Double, UInt64, F17_S2) -> Double) -> Double {
    return f(201081002, F17_S0(f0: 2018751226, f1: 8488544433072104028), F17_S1(f0: F17_S1_S0(f0: 1190765430157980, f1: 70252071), f1: 1297775609, f2: 160), 4290084351352688, 4738339757002694731, F17_S2(f0: 1829312773))
}

@frozen
public struct F18_S0
{
    public let f0 : Int8;
}

@frozen
public struct F18_S1
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : Double;
    public let f3 : UInt;
}

@frozen
public struct F18_S2
{
    public let f0 : Int;
}

@frozen
public struct F18_Ret_S0
{
    public let f0 : Int16;
}

@frozen
public struct F18_Ret
{
    public let f0 : F18_Ret_S0;
}

public func swiftCallbackFunc18(f: (F18_S0, F18_S1, F18_S2, UInt, UInt32, Int64, Int16, Double) -> F18_Ret) -> F18_Ret {
    return f(F18_S0(f0: 106), F18_S1(f0: 21619, f1: -4350, f2: 3457288266203248, f3: 9020447812661292883), F18_S2(f0: 2317132584983719004), 7379425918918939512, 2055208746, 1042861174364145790, 28457, 1799004152435515)
}

@frozen
public struct F19_S0
{
    public let f0 : Int16;
    public let f1 : Int8;
    public let f2 : Float;
}

@frozen
public struct F19_S1
{
    public let f0 : Int64;
    public let f1 : UInt16;
}

@frozen
public struct F19_S2
{
    public let f0 : UInt64;
    public let f1 : Int64;
}

@frozen
public struct F19_S3
{
    public let f0 : UInt32;
    public let f1 : Int32;
}

@frozen
public struct F19_Ret_S0
{
    public let f0 : Int64;
}

@frozen
public struct F19_Ret
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : UInt16;
    public let f3 : F19_Ret_S0;
    public let f4 : Double;
    public let f5 : Double;
    public let f6 : Double;
}

public func swiftCallbackFunc19(f: (Int64, UInt8, F19_S0, Int, F19_S1, Int32, Int32, UInt, UInt64, F19_S2, UInt16, F19_S3, Int8, Int64) -> F19_Ret) -> F19_Ret {
    return f(7456120134117592143, 114, F19_S0(f0: -7583, f1: 97, f2: 2768322), 3605245176125291560, F19_S1(f0: 4445885313084714470, f1: 15810), 1179699879, 109603412, 6521628547431964799, 7687430644226018854, F19_S2(f0: 8464855230956039883, f1: 861462819289140037), 26519, F19_S3(f0: 1864602741, f1: 397176384), 81, 4909173176891211442)
}

@frozen
public struct F20_S0_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F20_S0
{
    public let f0 : Int16;
    public let f1 : UInt;
    public let f2 : F20_S0_S0;
}

@frozen
public struct F20_S1_S0
{
    public let f0 : Float;
}

@frozen
public struct F20_S1
{
    public let f0 : Int64;
    public let f1 : UInt;
    public let f2 : F20_S1_S0;
    public let f3 : Int64;
    public let f4 : Int32;
}

@frozen
public struct F20_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F20_Ret
{
    public let f0 : UInt16;
    public let f1 : UInt16;
    public let f2 : Double;
    public let f3 : Int16;
    public let f4 : Double;
}

public func swiftCallbackFunc20(f: (F20_S0, F20_S1, Float, Float, Int8, F20_S2, Float) -> F20_Ret) -> F20_Ret {
    return f(F20_S0(f0: 28858, f1: 7024100299344418039, f2: F20_S0_S0(f0: 13025)), F20_S1(f0: 7900431324553135989, f1: 8131425055682506706, f2: F20_S1_S0(f0: 3884322), f3: 605453501265278638, f4: 353756684), 622319, 1401604, -101, F20_S2(f0: 1355570413), 2912776)
}

@frozen
public struct F21_S0
{
    public let f0 : Double;
    public let f1 : UInt64;
}

@frozen
public struct F21_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F21_Ret
{
    public let f0 : UInt16;
    public let f1 : UInt32;
    public let f2 : Int64;
}

public func swiftCallbackFunc21(f: (Int32, Int16, F21_S0, Int32, F21_S1, Int64, UInt32, Int64, UInt8, UInt16) -> F21_Ret) -> F21_Ret {
    return f(256017319, 14555, F21_S0(f0: 2102091966108033, f1: 8617538752301505079), 834677431, F21_S1(f0: 7043), 7166819734655141128, 965538086, 3827752442102685645, 110, 33646)
}

@frozen
public struct F22_S0
{
    public let f0 : Int;
    public let f1 : Float;
    public let f2 : Double;
}

@frozen
public struct F22_S1
{
    public let f0 : UInt;
}

@frozen
public struct F22_S2
{
    public let f0 : Int32;
    public let f1 : Double;
    public let f2 : Float;
    public let f3 : Int16;
    public let f4 : UInt16;
}

@frozen
public struct F22_S3
{
    public let f0 : Int64;
    public let f1 : UInt16;
}

@frozen
public struct F22_S4
{
    public let f0 : Double;
    public let f1 : UInt16;
}

@frozen
public struct F22_S5
{
    public let f0 : UInt32;
    public let f1 : Int16;
}

@frozen
public struct F22_S6
{
    public let f0 : Float;
}

@frozen
public struct F22_Ret
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : UInt;
}

public func swiftCallbackFunc22(f: (Int32, F22_S0, F22_S1, F22_S2, F22_S3, Int8, F22_S4, UInt8, UInt16, Int64, F22_S5, Int64, Float, F22_S6, UInt16) -> F22_Ret) -> F22_Ret {
    return f(640156952, F22_S0(f0: 824774470287401457, f1: 6163704, f2: 54328782764685), F22_S1(f0: 1679730195865415747), F22_S2(f0: 1462995665, f1: 2554087365600344, f2: 8193295, f3: 16765, f4: 45388), F22_S3(f0: 5560492364570389430, f1: 48308), 71, F22_S4(f0: 1639169280741045, f1: 12045), 217, 62917, 1465918945905384332, F22_S5(f0: 1364750179, f1: 3311), 9003480567517966914, 2157327, F22_S6(f0: 6647392), 1760)
}

@frozen
public struct F23_S0
{
    public let f0 : Int;
}

@frozen
public struct F23_S1
{
    public let f0 : Int;
}

public func swiftCallbackFunc23(f: (UInt, UInt8, Int8, UInt8, UInt8, F23_S0, UInt, F23_S1, Double) -> Double) -> Double {
    return f(5779410841248940897, 192, -128, 133, 20, F23_S0(f0: 2959916071636885436), 3651155214497129159, F23_S1(f0: 8141565342203061885), 1465425469608034)
}

@frozen
public struct F24_S0
{
    public let f0 : Int8;
    public let f1 : UInt8;
    public let f2 : UInt64;
    public let f3 : UInt32;
}

@frozen
public struct F24_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F24_S2_S0
{
    public let f0 : UInt16;
    public let f1 : UInt32;
}

@frozen
public struct F24_S2_S1
{
    public let f0 : Int64;
}

@frozen
public struct F24_S2
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : F24_S2_S0;
    public let f3 : F24_S2_S1;
}

@frozen
public struct F24_S3
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : Int64;
}

@frozen
public struct F24_S4
{
    public let f0 : UInt8;
}

public func swiftCallbackFunc24(f: (Int32, UInt, F24_S0, UInt16, F24_S1, Int8, F24_S2, UInt64, UInt64, F24_S3, Double, F24_S4) -> Float) -> Float {
    return f(1710754874, 6447433131978039331, F24_S0(f0: -92, f1: 181, f2: 3710374263631495948, f3: 257210428), 6631, F24_S1(f0: 2303), 15, F24_S2(f0: 2509049432824972381, f1: 616918672, f2: F24_S2_S0(f0: 50635, f1: 1337844540), f3: F24_S2_S1(f0: 335964796567786281)), 1114365571136806382, 8988425145801188208, F24_S3(f0: 31969, f1: 3008861, f2: 5466306080595269107), 2027780227887952, F24_S4(f0: 234))
}

@frozen
public struct F25_S0
{
    public let f0 : UInt;
}

@frozen
public struct F25_S1
{
    public let f0 : Float;
    public let f1 : Int8;
    public let f2 : Float;
    public let f3 : Int;
}

@frozen
public struct F25_S2
{
    public let f0 : UInt;
    public let f1 : UInt;
    public let f2 : Int64;
    public let f3 : UInt8;
}

@frozen
public struct F25_S3
{
    public let f0 : Float;
}

@frozen
public struct F25_S4
{
    public let f0 : Int8;
}

@frozen
public struct F25_Ret
{
    public let f0 : UInt64;
    public let f1 : Int64;
    public let f2 : UInt8;
    public let f3 : UInt16;
}

public func swiftCallbackFunc25(f: (F25_S0, UInt16, UInt, F25_S1, Int16, F25_S2, UInt64, UInt64, UInt64, F25_S3, F25_S4) -> F25_Ret) -> F25_Ret {
    return f(F25_S0(f0: 6077761381429658786), 2300, 3498354181807010234, F25_S1(f0: 5360721, f1: -40, f2: 109485, f3: 2311625789899959825), -28395, F25_S2(f0: 8729509817732080529, f1: 860365359368130822, f2: 7498894262834346040, f3: 218), 961687210282504701, 7184177441364400868, 8389319500274436977, F25_S3(f0: 4437173), F25_S4(f0: -107))
}

@frozen
public struct F26_S0
{
    public let f0 : Int8;
    public let f1 : Int;
    public let f2 : UInt8;
    public let f3 : UInt8;
}

@frozen
public struct F26_S1_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F26_S1
{
    public let f0 : Int8;
    public let f1 : Int32;
    public let f2 : Int16;
    public let f3 : F26_S1_S0;
}

@frozen
public struct F26_S2
{
    public let f0 : Int64;
}

@frozen
public struct F26_S3
{
    public let f0 : UInt8;
}

@frozen
public struct F26_Ret
{
    public let f0 : UInt;
    public let f1 : UInt8;
}

public func swiftCallbackFunc26(f: (Int8, UInt8, UInt32, F26_S0, F26_S1, F26_S2, F26_S3) -> F26_Ret) -> F26_Ret {
    return f(-16, 220, 72386567, F26_S0(f0: -33, f1: 6488877286424796715, f2: 143, f3: 74), F26_S1(f0: 104, f1: 1719453315, f2: 20771, f3: F26_S1_S0(f0: 3636117595999837800)), F26_S2(f0: 2279530426119665839), F26_S3(f0: 207))
}

@frozen
public struct F27_S0
{
    public let f0 : Int16;
}

@frozen
public struct F27_S1_S0
{
    public let f0 : UInt16;
    public let f1 : Int8;
}

@frozen
public struct F27_S1
{
    public let f0 : Int64;
    public let f1 : F27_S1_S0;
    public let f2 : Float;
}

@frozen
public struct F27_S2
{
    public let f0 : UInt64;
    public let f1 : Int8;
    public let f2 : UInt32;
    public let f3 : Int64;
}

@frozen
public struct F27_S3_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F27_S3
{
    public let f0 : F27_S3_S0;
}

public func swiftCallbackFunc27(f: (UInt64, UInt8, F27_S0, UInt8, UInt8, F27_S1, Int32, F27_S2, Int, UInt32, F27_S3) -> Float) -> Float {
    return f(4847421047018330189, 214, F27_S0(f0: 31313), 207, 174, F27_S1(f0: 4476120319602257660, f1: F27_S1_S0(f0: 26662, f1: -55), f2: 70666), 1340306103, F27_S2(f0: 2772939788297637999, f1: -65, f2: 7500441, f3: 4926907273817562134), 5862689255099071258, 1077270996, F27_S3(f0: F27_S3_S0(f0: 35167)))
}

@frozen
public struct F28_S0
{
    public let f0 : UInt64;
    public let f1 : Int8;
}

@frozen
public struct F28_S1
{
    public let f0 : Int64;
    public let f1 : UInt;
    public let f2 : Int;
    public let f3 : Int32;
}

@frozen
public struct F28_S2
{
    public let f0 : Int;
}

@frozen
public struct F28_S3
{
    public let f0 : Int64;
}

@frozen
public struct F28_Ret_S0
{
    public let f0 : Float;
}

@frozen
public struct F28_Ret
{
    public let f0 : F28_Ret_S0;
    public let f1 : UInt16;
}

public func swiftCallbackFunc28(f: (UInt32, UInt16, Int8, Int8, UInt16, Float, F28_S0, Double, UInt64, F28_S1, F28_S2, F28_S3) -> F28_Ret) -> F28_Ret {
    return f(893827094, 38017, -90, -1, 16109, 5844449, F28_S0(f0: 176269147098539470, f1: 23), 1431426259441210, 6103261251702315645, F28_S1(f0: 3776818122826483419, f1: 9181420263296840471, f2: 3281861424961082542, f3: 1442905253), F28_S2(f0: 8760009193798370900), F28_S3(f0: 7119917900929398683))
}

@frozen
public struct F29_S0
{
    public let f0 : UInt8;
    public let f1 : Double;
    public let f2 : UInt16;
}

@frozen
public struct F29_S1
{
    public let f0 : UInt32;
    public let f1 : Int;
    public let f2 : UInt64;
    public let f3 : UInt32;
}

@frozen
public struct F29_S2
{
    public let f0 : Int32;
}

@frozen
public struct F29_S3
{
    public let f0 : UInt32;
    public let f1 : UInt32;
    public let f2 : Float;
}

@frozen
public struct F29_S4
{
    public let f0 : Int32;
}

@frozen
public struct F29_Ret_S0
{
    public let f0 : Int;
    public let f1 : UInt64;
}

@frozen
public struct F29_Ret
{
    public let f0 : UInt;
    public let f1 : UInt;
    public let f2 : UInt;
    public let f3 : F29_Ret_S0;
    public let f4 : UInt64;
    public let f5 : UInt32;
}

public func swiftCallbackFunc29(f: (F29_S0, Int, UInt64, UInt8, Int64, UInt8, Int, F29_S1, Int32, Int8, UInt8, UInt64, F29_S2, F29_S3, Int16, F29_S4, UInt32) -> F29_Ret) -> F29_Ret {
    return f(F29_S0(f0: 152, f1: 737900189383874, f2: 33674), 5162040247631126074, 6524156301721885895, 129, 6661424933974053497, 145, 7521422786615537370, F29_S1(f0: 1361601345, f1: 3366726213840694614, f2: 7767610514138029164, f3: 1266864987), 1115803878, 5, 80, 2041754562738600205, F29_S2(f0: 1492686870), F29_S3(f0: 142491811, f1: 1644962309, f2: 1905811), -3985, F29_S4(f0: 1921386549), 1510666400)
}

@frozen
public struct F30_S0
{
    public let f0 : UInt16;
    public let f1 : Int16;
    public let f2 : Int16;
    public let f3 : Int8;
}

@frozen
public struct F30_S1
{
    public let f0 : UInt16;
    public let f1 : UInt;
}

@frozen
public struct F30_S2
{
    public let f0 : Int64;
    public let f1 : Int8;
    public let f2 : UInt16;
}

@frozen
public struct F30_S3
{
    public let f0 : Int8;
}

public func swiftCallbackFunc30(f: (F30_S0, F30_S1, F30_S2, F30_S3, Int) -> Float) -> Float {
    return f(F30_S0(f0: 50723, f1: 19689, f2: -6469, f3: 83), F30_S1(f0: 51238, f1: 5879147675377398012), F30_S2(f0: 7909999288286190848, f1: -99, f2: 61385), F30_S3(f0: 48), 2980085298293056148)
}

@frozen
public struct F31_S0
{
    public let f0 : Int32;
    public let f1 : UInt64;
    public let f2 : UInt;
}

@frozen
public struct F31_Ret_S0
{
    public let f0 : UInt32;
    public let f1 : Float;
    public let f2 : UInt16;
    public let f3 : Int16;
    public let f4 : Float;
}

@frozen
public struct F31_Ret
{
    public let f0 : F31_Ret_S0;
    public let f1 : UInt16;
}

public func swiftCallbackFunc31(f: (F31_S0, Double) -> F31_Ret) -> F31_Ret {
    return f(F31_S0(f0: 1072945099, f1: 5760996810500287322, f2: 3952909367135409979), 2860786541632685)
}

@frozen
public struct F32_Ret
{
    public let f0 : UInt;
    public let f1 : Double;
    public let f2 : Int;
}

public func swiftCallbackFunc32(f: (UInt16, Int16) -> F32_Ret) -> F32_Ret {
    return f(21020, 7462)
}

@frozen
public struct F33_S0
{
    public let f0 : Int16;
    public let f1 : UInt64;
}

@frozen
public struct F33_S1_S0
{
    public let f0 : Int16;
}

@frozen
public struct F33_S1
{
    public let f0 : F33_S1_S0;
    public let f1 : UInt32;
    public let f2 : UInt;
}

@frozen
public struct F33_S2
{
    public let f0 : UInt32;
    public let f1 : UInt64;
    public let f2 : Int8;
    public let f3 : Int8;
    public let f4 : UInt;
}

@frozen
public struct F33_S3_S0_S0
{
    public let f0 : Int16;
}

@frozen
public struct F33_S3_S0
{
    public let f0 : F33_S3_S0_S0;
}

@frozen
public struct F33_S3
{
    public let f0 : F33_S3_S0;
}

public func swiftCallbackFunc33(f: (F33_S0, Float, F33_S1, UInt32, Int, Int8, Int8, Float, UInt8, Float, Int8, F33_S2, Int, F33_S3, Int, UInt32) -> UInt) -> UInt {
    return f(F33_S0(f0: -23471, f1: 2736941806609505888), 6930550, F33_S1(f0: F33_S1_S0(f0: 32476), f1: 165441961, f2: 3890227499323387948), 591524870, 1668420058132495503, -67, 94, 3180786, 42, 7674952, 43, F33_S2(f0: 771356149, f1: 3611576949210389997, f2: -15, f3: 7, f4: 2577587324978560192), 8266150294848599489, F33_S3(f0: F33_S3_S0(f0: F33_S3_S0_S0(f0: 9216))), 710302565025364450, 1060812904)
}

@frozen
public struct F34_S0_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F34_S0
{
    public let f0 : F34_S0_S0;
    public let f1 : UInt;
}

public func swiftCallbackFunc34(f: (UInt32, F34_S0, UInt, Int16) -> UInt16) -> UInt16 {
    return f(2068009847, F34_S0(f0: F34_S0_S0(f0: 845123292), f1: 5148244462913472487), 8632568386462910655, 7058)
}

@frozen
public struct F35_S0_S0_S0
{
    public let f0 : Int32;
}

@frozen
public struct F35_S0_S0
{
    public let f0 : Int64;
    public let f1 : F35_S0_S0_S0;
}

@frozen
public struct F35_S0_S1
{
    public let f0 : Double;
}

@frozen
public struct F35_S0
{
    public let f0 : F35_S0_S0;
    public let f1 : Int32;
    public let f2 : F35_S0_S1;
    public let f3 : Int;
}

@frozen
public struct F35_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F35_S2_S0
{
    public let f0 : Double;
}

@frozen
public struct F35_S2
{
    public let f0 : F35_S2_S0;
}

public func swiftCallbackFunc35(f: (UInt8, Int8, Float, Int64, Int, F35_S0, F35_S1, F35_S2) -> UInt64) -> UInt64 {
    return f(182, -16, 7763558, 5905028570860904693, 5991001624972063224, F35_S0(f0: F35_S0_S0(f0: 6663912001709962059, f1: F35_S0_S0_S0(f0: 1843939591)), f1: 1095170337, f2: F35_S0_S1(f0: 3908756332193409), f3: 8246190362462442203), F35_S1(f0: 52167), F35_S2(f0: F35_S2_S0(f0: 283499999631068)))
}

@frozen
public struct F36_S0
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : UInt8;
    public let f3 : UInt;
}

public func swiftCallbackFunc36(f: (UInt, Double, UInt, UInt8, Int64, F36_S0, Int8) -> Int) -> Int {
    return f(5079603407518207003, 2365862518115571, 6495651757722767835, 46, 1550138390178394449, F36_S0(f0: 1858960269, f1: 1925263848394986294, f2: 217, f3: 8520779488644482307), -83)
}

@frozen
public struct F37_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F37_S0
{
    public let f0 : UInt;
    public let f1 : UInt32;
    public let f2 : F37_S0_S0;
    public let f3 : Float;
}

@frozen
public struct F37_S1
{
    public let f0 : UInt;
    public let f1 : UInt32;
}

@frozen
public struct F37_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F37_Ret
{
    public let f0 : Float;
    public let f1 : UInt8;
    public let f2 : Int16;
    public let f3 : UInt64;
}

public func swiftCallbackFunc37(f: (UInt64, F37_S0, Double, UInt16, F37_S1, F37_S2) -> F37_Ret) -> F37_Ret {
    return f(1623104856688575867, F37_S0(f0: 3785544303342575322, f1: 717682682, f2: F37_S0_S0(f0: 2674933748436691896), f3: 3211458), 996705046384579, 8394, F37_S1(f0: 1048947722954084863, f1: 252415487), F37_S2(f0: 3664))
}

@frozen
public struct F38_S0_S0
{
    public let f0 : Int;
    public let f1 : Float;
}

@frozen
public struct F38_S0
{
    public let f0 : F38_S0_S0;
    public let f1 : UInt16;
    public let f2 : Int32;
    public let f3 : Float;
}

@frozen
public struct F38_S1
{
    public let f0 : Int16;
    public let f1 : Int32;
    public let f2 : UInt32;
}

public func swiftCallbackFunc38(f: (F38_S0, F38_S1, Double, Int16, Int8, UInt32, Int16, Float, Int, Float, UInt32, UInt8, Double, Int8) -> Double) -> Double {
    return f(F38_S0(f0: F38_S0_S0(f0: 7389960750529773276, f1: 4749108), f1: 54323, f2: 634649910, f3: 83587), F38_S1(f0: -15547, f1: 1747384081, f2: 851987981), 3543874366683681, 5045, -32, 2084540698, 25583, 3158067, 1655263182833369283, 829404, 1888859844, 153, 222366180309763, 61)
}

@frozen
public struct F39_S0_S0
{
    public let f0 : Int16;
}

@frozen
public struct F39_S0_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F39_S0
{
    public let f0 : F39_S0_S0;
    public let f1 : Int32;
    public let f2 : F39_S0_S1;
    public let f3 : UInt;
}

@frozen
public struct F39_S1
{
    public let f0 : UInt16;
    public let f1 : UInt8;
    public let f2 : Float;
    public let f3 : Int64;
}

@frozen
public struct F39_S2
{
    public let f0 : Int32;
    public let f1 : Float;
}

@frozen
public struct F39_S3
{
    public let f0 : UInt32;
    public let f1 : Int;
    public let f2 : Int;
}

public func swiftCallbackFunc39(f: (F39_S0, UInt, UInt32, Double, F39_S1, F39_S2, Int8, F39_S3, Int32, UInt64, UInt8) -> Int) -> Int {
    return f(F39_S0(f0: F39_S0_S0(f0: -31212), f1: 1623216479, f2: F39_S0_S1(f0: 7181), f3: 8643545152918150186), 799631211988519637, 94381581, 761127371030426, F39_S1(f0: 417, f1: 85, f2: 1543931, f3: 3918460222899735322), F39_S2(f0: 883468300, f1: 2739152), -94, F39_S3(f0: 1374766954, f1: 2042223450490396789, f2: 2672454113535023130), 946259065, 6805548458517673751, 61)
}

@frozen
public struct F40_S0
{
    public let f0 : Int16;
    public let f1 : Int32;
}

@frozen
public struct F40_S1
{
    public let f0 : Int32;
}

@frozen
public struct F40_S2
{
    public let f0 : Int64;
    public let f1 : UInt16;
    public let f2 : Int;
    public let f3 : UInt8;
}

@frozen
public struct F40_S3_S0
{
    public let f0 : Float;
}

@frozen
public struct F40_S3
{
    public let f0 : UInt;
    public let f1 : Double;
    public let f2 : F40_S3_S0;
    public let f3 : Double;
}

public func swiftCallbackFunc40(f: (F40_S0, UInt32, UInt8, F40_S1, F40_S2, UInt64, UInt, UInt64, Int, UInt16, UInt32, F40_S3, UInt) -> UInt) -> UInt {
    return f(F40_S0(f0: 22601, f1: 312892872), 1040102825, 56, F40_S1(f0: 101203812), F40_S2(f0: 4298883321494088257, f1: 2095, f2: 1536552108568739270, f3: 220), 2564624804830565018, 173855559108584219, 6222832940831380264, 1898370824516510398, 3352, 1643571476, F40_S3(f0: 7940054758811932961, f1: 246670432251533, f2: F40_S3_S0(f0: 7890596), f3: 1094140965415232), 2081923113238309816)
}

@frozen
public struct F41_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F41_Ret
{
    public let f0 : UInt64;
    public let f1 : Double;
    public let f2 : UInt32;
    public let f3 : UInt32;
}

public func swiftCallbackFunc41(f: (F41_S0) -> F41_Ret) -> F41_Ret {
    return f(F41_S0(f0: 1430200072))
}

@frozen
public struct F42_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F42_S0
{
    public let f0 : F42_S0_S0;
}

@frozen
public struct F42_S1
{
    public let f0 : UInt32;
}

public func swiftCallbackFunc42(f: (Int32, UInt32, F42_S0, Float, UInt8, F42_S1) -> Int) -> Int {
    return f(1046060439, 1987212952, F42_S0(f0: F42_S0_S0(f0: 4714080408858753964)), 2364146, 25, F42_S1(f0: 666986488))
}

@frozen
public struct F43_S0
{
    public let f0 : Int32;
    public let f1 : Int32;
    public let f2 : Int;
}

@frozen
public struct F43_S1
{
    public let f0 : Int8;
}

@frozen
public struct F43_Ret
{
    public let f0 : UInt16;
}

public func swiftCallbackFunc43(f: (F43_S0, F43_S1) -> F43_Ret) -> F43_Ret {
    return f(F43_S0(f0: 406102630, f1: 1946236062, f2: 663606396354980308), F43_S1(f0: -8))
}

@frozen
public struct F44_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F44_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F44_S1_S1
{
    public let f0 : UInt;
}

@frozen
public struct F44_S1
{
    public let f0 : Int16;
    public let f1 : Int16;
    public let f2 : F44_S1_S0;
    public let f3 : F44_S1_S1;
}

@frozen
public struct F44_S2
{
    public let f0 : UInt;
}

@frozen
public struct F44_S3
{
    public let f0 : Int8;
}

@frozen
public struct F44_Ret_S0
{
    public let f0 : UInt;
}

@frozen
public struct F44_Ret
{
    public let f0 : Int;
    public let f1 : F44_Ret_S0;
    public let f2 : Double;
}

public func swiftCallbackFunc44(f: (Double, F44_S0, F44_S1, F44_S2, F44_S3) -> F44_Ret) -> F44_Ret {
    return f(4281406007431544, F44_S0(f0: 2097291497), F44_S1(f0: -10489, f1: -9573, f2: F44_S1_S0(f0: 62959), f3: F44_S1_S1(f0: 7144119809173057975)), F44_S2(f0: 168733393207234277), F44_S3(f0: 64))
}

@frozen
public struct F45_S0
{
    public let f0 : UInt;
}

@frozen
public struct F45_S1
{
    public let f0 : UInt;
    public let f1 : Int16;
}

@frozen
public struct F45_Ret_S0
{
    public let f0 : Float;
}

@frozen
public struct F45_Ret
{
    public let f0 : Double;
    public let f1 : F45_Ret_S0;
    public let f2 : Int64;
    public let f3 : Double;
    public let f4 : UInt64;
    public let f5 : Int8;
    public let f6 : Int32;
}

public func swiftCallbackFunc45(f: (F45_S0, F45_S1, UInt8) -> F45_Ret) -> F45_Ret {
    return f(F45_S0(f0: 5311803360204128233), F45_S1(f0: 2204790044275015546, f1: 8942), 207)
}

@frozen
public struct F46_Ret
{
    public let f0 : UInt;
    public let f1 : Double;
    public let f2 : Int64;
    public let f3 : UInt16;
}

public func swiftCallbackFunc46(f: (Int, UInt, UInt16, UInt16, Int64) -> F46_Ret) -> F46_Ret {
    return f(1855296013283572041, 1145047910516899437, 20461, 58204, 1923767011143317115)
}

@frozen
public struct F47_S0
{
    public let f0 : UInt8;
    public let f1 : Int32;
}

@frozen
public struct F47_S1
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : Int8;
}

@frozen
public struct F47_S2_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F47_S2
{
    public let f0 : Int8;
    public let f1 : Float;
    public let f2 : Int32;
    public let f3 : Float;
    public let f4 : F47_S2_S0;
}

@frozen
public struct F47_S3
{
    public let f0 : UInt64;
    public let f1 : Int64;
}

@frozen
public struct F47_S4
{
    public let f0 : UInt64;
}

@frozen
public struct F47_Ret
{
    public let f0 : Int16;
    public let f1 : Int16;
    public let f2 : Int64;
}

public func swiftCallbackFunc47(f: (Int, Float, UInt32, F47_S0, F47_S1, UInt16, Float, Int, Int, UInt, UInt, Int16, F47_S2, F47_S3, F47_S4) -> F47_Ret) -> F47_Ret {
    return f(6545360066379352091, 1240616, 575670382, F47_S0(f0: 27, f1: 1769677101), F47_S1(f0: 4175209822525678639, f1: 483151627, f2: -41), 20891, 1011044, 8543308148327168378, 9126721646663585297, 5438914191614359864, 5284613245897089025, -9227, F47_S2(f0: -23, f1: 1294109, f2: 411726757, f3: 6621598, f4: F47_S2_S0(f0: 249)), F47_S3(f0: 5281612261430853979, f1: 7161295082465816089), F47_S4(f0: 1995556861952451598))
}

@frozen
public struct F48_S0
{
    public let f0 : UInt64;
    public let f1 : Int16;
    public let f2 : UInt64;
}

@frozen
public struct F48_S1_S0
{
    public let f0 : Float;
}

@frozen
public struct F48_S1
{
    public let f0 : Double;
    public let f1 : Int32;
    public let f2 : Int32;
    public let f3 : F48_S1_S0;
    public let f4 : UInt;
}

public func swiftCallbackFunc48(f: (Int8, Int16, Int16, UInt32, F48_S0, UInt32, F48_S1, Int32, Int32, UInt16, Int64, UInt32) -> Int64) -> Int64 {
    return f(-34, 11634, -27237, 1039294154, F48_S0(f0: 1367847206719062131, f1: 22330, f2: 689282484471011648), 1572626904, F48_S1(f0: 3054128759424009, f1: 1677338134, f2: 1257237843, f3: F48_S1_S0(f0: 6264494), f4: 8397097040610783205), 1060447208, 269785114, 20635, 7679010342730986048, 1362633148)
}

@frozen
public struct F49_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F49_S0
{
    public let f0 : F49_S0_S0;
    public let f1 : UInt64;
}

@frozen
public struct F49_Ret
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : UInt8;
    public let f3 : UInt8;
    public let f4 : Int8;
    public let f5 : Int64;
}

public func swiftCallbackFunc49(f: (F49_S0, Int64) -> F49_Ret) -> F49_Ret {
    return f(F49_S0(f0: F49_S0_S0(f0: 48), f1: 7563394992711018452), 4358370311341042916)
}

@frozen
public struct F50_S0_S0
{
    public let f0 : Double;
}

@frozen
public struct F50_S0
{
    public let f0 : UInt16;
    public let f1 : F50_S0_S0;
}

@frozen
public struct F50_S1
{
    public let f0 : Double;
    public let f1 : UInt16;
    public let f2 : Int32;
    public let f3 : Int;
    public let f4 : Double;
}

@frozen
public struct F50_S2
{
    public let f0 : Int32;
    public let f1 : Float;
    public let f2 : UInt32;
}

@frozen
public struct F50_S3
{
    public let f0 : Int64;
    public let f1 : Int32;
    public let f2 : Float;
    public let f3 : Int8;
}

@frozen
public struct F50_S4
{
    public let f0 : Int64;
}

@frozen
public struct F50_S5_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F50_S5
{
    public let f0 : F50_S5_S0;
}

public func swiftCallbackFunc50(f: (F50_S0, F50_S1, UInt8, F50_S2, Int32, UInt64, Int8, Int8, Float, F50_S3, F50_S4, F50_S5, Float) -> UInt8) -> UInt8 {
    return f(F50_S0(f0: 31857, f1: F50_S0_S0(f0: 1743417849706254)), F50_S1(f0: 4104577461772135, f1: 13270, f2: 2072598986, f3: 9056978834867675248, f4: 844742439929087), 87, F50_S2(f0: 1420884537, f1: 78807, f2: 1081688273), 336878110, 1146514566942283069, -93, 73, 2321639, F50_S3(f0: 1940888991336881606, f1: 688345394, f2: 712275, f3: -128), F50_S4(f0: 2638503583829414770), F50_S5(f0: F50_S5_S0(f0: 23681)), 8223218)
}

@frozen
public struct F51_S0
{
    public let f0 : Int64;
}

@frozen
public struct F51_Ret
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : Int;
    public let f3 : UInt16;
    public let f4 : UInt64;
}

public func swiftCallbackFunc51(f: (Int16, UInt, F51_S0, UInt64) -> F51_Ret) -> F51_Ret {
    return f(10812, 470861239714315155, F51_S0(f0: 5415660333180374788), 2389942629143476149)
}

@frozen
public struct F52_S0
{
    public let f0 : Float;
}

@frozen
public struct F52_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F52_Ret
{
    public let f0 : Float;
    public let f1 : UInt16;
    public let f2 : Int64;
    public let f3 : Int16;
    public let f4 : UInt64;
    public let f5 : Int8;
}

public func swiftCallbackFunc52(f: (Int, F52_S0, Int16, Int16, F52_S1) -> F52_Ret) -> F52_Ret {
    return f(3233654765973602550, F52_S0(f0: 5997729), -7404, -20804, F52_S1(f0: 17231))
}

@frozen
public struct F53_S0_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F53_S0_S0
{
    public let f0 : F53_S0_S0_S0;
}

@frozen
public struct F53_S0
{
    public let f0 : Int8;
    public let f1 : F53_S0_S0;
    public let f2 : UInt8;
    public let f3 : UInt;
    public let f4 : Int64;
}

@frozen
public struct F53_S1
{
    public let f0 : Float;
    public let f1 : UInt8;
}

@frozen
public struct F53_S2
{
    public let f0 : Int8;
    public let f1 : Int64;
}

@frozen
public struct F53_S3_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F53_S3
{
    public let f0 : Int32;
    public let f1 : UInt32;
    public let f2 : F53_S3_S0;
}

@frozen
public struct F53_S4
{
    public let f0 : Int16;
}

@frozen
public struct F53_S5_S0
{
    public let f0 : UInt32;
}

@frozen
public struct F53_S5_S1_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F53_S5_S1
{
    public let f0 : F53_S5_S1_S0;
}

@frozen
public struct F53_S5
{
    public let f0 : F53_S5_S0;
    public let f1 : UInt;
    public let f2 : UInt16;
    public let f3 : F53_S5_S1;
    public let f4 : Int8;
}

@frozen
public struct F53_S6
{
    public let f0 : Int;
}

@frozen
public struct F53_Ret
{
    public let f0 : Int;
}

public func swiftCallbackFunc53(f: (F53_S0, UInt8, Int64, F53_S1, F53_S2, F53_S3, Int64, F53_S4, F53_S5, F53_S6) -> F53_Ret) -> F53_Ret {
    return f(F53_S0(f0: -123, f1: F53_S0_S0(f0: F53_S0_S0_S0(f0: 3494916243607193741)), f2: 167, f3: 4018943158751734338, f4: 6768175524813742847), 207, 8667995458064724392, F53_S1(f0: 492157, f1: 175), F53_S2(f0: 76, f1: 5794486968525461488), F53_S3(f0: 2146070335, f1: 1109141712, f2: F53_S3_S0(f0: 44270)), 3581380181786253859, F53_S4(f0: 23565), F53_S5(f0: F53_S5_S0(f0: 1995174927), f1: 5025417700244056666, f2: 1847, f3: F53_S5_S1(f0: F53_S5_S1_S0(f0: 6)), f4: -87), F53_S6(f0: 5737280129078653969))
}

@frozen
public struct F54_S0
{
    public let f0 : Int32;
    public let f1 : Float;
    public let f2 : UInt;
    public let f3 : UInt8;
}

@frozen
public struct F54_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F54_S2_S0_S0
{
    public let f0 : Double;
}

@frozen
public struct F54_S2_S0
{
    public let f0 : Int16;
    public let f1 : F54_S2_S0_S0;
}

@frozen
public struct F54_S2
{
    public let f0 : Double;
    public let f1 : F54_S2_S0;
    public let f2 : Int64;
    public let f3 : UInt64;
}

@frozen
public struct F54_S3
{
    public let f0 : Float;
}

@frozen
public struct F54_S4
{
    public let f0 : UInt16;
    public let f1 : Int8;
}

@frozen
public struct F54_S5
{
    public let f0 : UInt16;
}

@frozen
public struct F54_Ret
{
    public let f0 : Int16;
    public let f1 : Int;
}

public func swiftCallbackFunc54(f: (UInt16, F54_S0, Float, F54_S1, Int64, Int32, F54_S2, F54_S3, F54_S4, Float, F54_S5) -> F54_Ret) -> F54_Ret {
    return f(16440, F54_S0(f0: 922752112, f1: 7843043, f2: 1521939500434086364, f3: 50), 3111108, F54_S1(f0: 50535), 4761507229870258916, 1670668155, F54_S2(f0: 432665443852892, f1: F54_S2_S0(f0: 13094, f1: F54_S2_S0_S0(f0: 669143993481144)), f2: 30067117315069590, f3: 874012622621600805), F54_S3(f0: 7995066), F54_S4(f0: 48478, f1: 23), 4383787, F54_S5(f0: 61633))
}

@frozen
public struct F55_S0_S0
{
    public let f0 : Double;
}

@frozen
public struct F55_S0
{
    public let f0 : UInt;
    public let f1 : F55_S0_S0;
    public let f2 : Int8;
}

@frozen
public struct F55_S1
{
    public let f0 : Int;
}

@frozen
public struct F55_S2
{
    public let f0 : UInt64;
}

@frozen
public struct F55_Ret_S0
{
    public let f0 : Int16;
    public let f1 : Int32;
}

@frozen
public struct F55_Ret
{
    public let f0 : UInt;
    public let f1 : Int;
    public let f2 : Double;
    public let f3 : F55_Ret_S0;
    public let f4 : UInt64;
}

public func swiftCallbackFunc55(f: (F55_S0, Int64, F55_S1, Int8, F55_S2, Float) -> F55_Ret) -> F55_Ret {
    return f(F55_S0(f0: 2856661562863799725, f1: F55_S0_S0(f0: 1260582440479139), f2: 5), 7945068527720423751, F55_S1(f0: 4321616441998677375), -68, F55_S2(f0: 3311106172201778367), 5600069)
}

@frozen
public struct F56_S0
{
    public let f0 : Double;
}

public func swiftCallbackFunc56(f: (F56_S0) -> UInt32) -> UInt32 {
    return f(F56_S0(f0: 3082602006731666))
}

@frozen
public struct F57_S0
{
    public let f0 : Int64;
    public let f1 : Int32;
    public let f2 : UInt64;
}

@frozen
public struct F57_S1
{
    public let f0 : UInt8;
}

@frozen
public struct F57_S2
{
    public let f0 : Float;
}

@frozen
public struct F57_Ret_S0
{
    public let f0 : Int64;
    public let f1 : UInt8;
    public let f2 : Int16;
}

@frozen
public struct F57_Ret
{
    public let f0 : F57_Ret_S0;
    public let f1 : UInt8;
}

public func swiftCallbackFunc57(f: (Int8, UInt, UInt32, Int64, UInt64, Int16, Int64, F57_S0, F57_S1, F57_S2) -> F57_Ret) -> F57_Ret {
    return f(54, 753245150862584974, 1470962934, 1269392070140776313, 2296560034524654667, 12381, 198893062684618980, F57_S0(f0: 1310571041794038100, f1: 18741662, f2: 7855196891704523814), F57_S1(f0: 156), F57_S2(f0: 72045))
}

@frozen
public struct F58_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F58_S1
{
    public let f0 : Float;
    public let f1 : UInt16;
}

@frozen
public struct F58_S2_S0_S0
{
    public let f0 : Int;
}

@frozen
public struct F58_S2_S0
{
    public let f0 : F58_S2_S0_S0;
}

@frozen
public struct F58_S2
{
    public let f0 : F58_S2_S0;
}

public func swiftCallbackFunc58(f: (UInt64, Int8, Int, F58_S0, F58_S1, Int64, F58_S2, Int32) -> Int) -> Int {
    return f(4612004722568513699, -96, 1970590839325113617, F58_S0(f0: 211), F58_S1(f0: 5454927, f1: 48737), 921570327236881486, F58_S2(f0: F58_S2_S0(f0: F58_S2_S0_S0(f0: 7726203059421444802))), 491616915)
}

public func swiftCallbackFunc59(f: (UInt16, Int64, Int) -> UInt64) -> UInt64 {
    return f(9232, 7281011081566942937, 8203439771560005792)
}

@frozen
public struct F60_S0
{
    public let f0 : Int;
}

@frozen
public struct F60_S1
{
    public let f0 : UInt64;
    public let f1 : Int32;
}

public func swiftCallbackFunc60(f: (Float, Double, Int64, UInt16, Float, Float, F60_S0, Int16, F60_S1, Int16, Int64) -> UInt64) -> UInt64 {
    return f(2682255, 2041676057169359, 5212916666940122160, 64444, 6372882, 8028835, F60_S0(f0: 6629286640024570381), 1520, F60_S1(f0: 8398497739914283366, f1: 1882981891), 7716, 6631047215535600409)
}

@frozen
public struct F61_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F61_S0
{
    public let f0 : F61_S0_S0;
    public let f1 : Int64;
    public let f2 : UInt32;
}

@frozen
public struct F61_S1
{
    public let f0 : Int8;
    public let f1 : Float;
    public let f2 : Int;
}

@frozen
public struct F61_S2_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F61_S2_S0
{
    public let f0 : F61_S2_S0_S0;
}

@frozen
public struct F61_S2_S1
{
    public let f0 : Int8;
}

@frozen
public struct F61_S2
{
    public let f0 : F61_S2_S0;
    public let f1 : F61_S2_S1;
}

@frozen
public struct F61_S3
{
    public let f0 : UInt64;
    public let f1 : Int;
}

public func swiftCallbackFunc61(f: (UInt32, UInt32, F61_S0, F61_S1, F61_S2, Int8, Int16, F61_S3, Int32, UInt32) -> UInt32) -> UInt32 {
    return f(1070797065, 135220309, F61_S0(f0: F61_S0_S0(f0: 6475887024664217162), f1: 563444654083452485, f2: 1748956360), F61_S1(f0: -112, f1: 3433396, f2: 8106074956722850624), F61_S2(f0: F61_S2_S0(f0: F61_S2_S0_S0(f0: 2318628619979263858)), f1: F61_S2_S1(f0: -93)), -122, -11696, F61_S3(f0: 5229393236090246212, f1: 4021449757638811198), 689517945, 657677740)
}

@frozen
public struct F62_S0
{
    public let f0 : Float;
}

@frozen
public struct F62_Ret
{
    public let f0 : UInt16;
    public let f1 : Int64;
    public let f2 : Int;
    public let f3 : Int64;
}

public func swiftCallbackFunc62(f: (F62_S0) -> F62_Ret) -> F62_Ret {
    return f(F62_S0(f0: 6500993))
}

@frozen
public struct F63_S0
{
    public let f0 : Int;
}

public func swiftCallbackFunc63(f: (F63_S0, Int16) -> Float) -> Float {
    return f(F63_S0(f0: 8391317504019075904), 11218)
}

@frozen
public struct F64_S0
{
    public let f0 : Int32;
}

@frozen
public struct F64_S1
{
    public let f0 : UInt64;
}

@frozen
public struct F64_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F64_Ret_S0
{
    public let f0 : UInt16;
    public let f1 : UInt;
    public let f2 : UInt64;
}

@frozen
public struct F64_Ret
{
    public let f0 : UInt;
    public let f1 : F64_Ret_S0;
    public let f2 : Double;
}

public func swiftCallbackFunc64(f: (Int8, F64_S0, F64_S1, UInt, F64_S2) -> F64_Ret) -> F64_Ret {
    return f(-22, F64_S0(f0: 1591678205), F64_S1(f0: 8355549563000003325), 5441989206466502201, F64_S2(f0: 2097092811))
}

@frozen
public struct F65_S0
{
    public let f0 : Double;
}

@frozen
public struct F65_S1
{
    public let f0 : UInt16;
    public let f1 : Int;
}

@frozen
public struct F65_S2
{
    public let f0 : Int16;
}

@frozen
public struct F65_S3
{
    public let f0 : Int32;
    public let f1 : UInt32;
    public let f2 : Int8;
    public let f3 : UInt;
    public let f4 : Double;
}

@frozen
public struct F65_Ret
{
    public let f0 : Int;
    public let f1 : Int;
    public let f2 : Int;
    public let f3 : Float;
}

public func swiftCallbackFunc65(f: (F65_S0, Int16, Double, UInt, F65_S1, UInt64, F65_S2, Int, F65_S3, Int32, Int64, UInt32, Double) -> F65_Ret) -> F65_Ret {
    return f(F65_S0(f0: 2969223123583220), -10269, 3909264978196109, 522883062031213707, F65_S1(f0: 37585, f1: 5879827541057349126), 1015270399093748716, F65_S2(f0: 19670), 1900026319968050423, F65_S3(f0: 1440511399, f1: 1203865685, f2: 12, f3: 4061296318630567634, f4: 2406524883317724), 1594888000, 2860599972459787263, 1989052358, 1036075606072593)
}

@frozen
public struct F66_Ret_S0
{
    public let f0 : Float;
    public let f1 : UInt8;
}

@frozen
public struct F66_Ret
{
    public let f0 : UInt32;
    public let f1 : Int32;
    public let f2 : UInt32;
    public let f3 : F66_Ret_S0;
    public let f4 : Int;
}

public func swiftCallbackFunc66(f: (Int64) -> F66_Ret) -> F66_Ret {
    return f(8300712022174991120)
}

@frozen
public struct F67_S0
{
    public let f0 : UInt32;
    public let f1 : UInt8;
    public let f2 : UInt8;
    public let f3 : Int32;
}

@frozen
public struct F67_S1
{
    public let f0 : UInt32;
}

@frozen
public struct F67_S2_S0
{
    public let f0 : Int;
}

@frozen
public struct F67_S2
{
    public let f0 : UInt64;
    public let f1 : UInt32;
    public let f2 : Int;
    public let f3 : UInt32;
    public let f4 : F67_S2_S0;
}

@frozen
public struct F67_S3
{
    public let f0 : Int16;
    public let f1 : UInt64;
    public let f2 : UInt64;
    public let f3 : Float;
}

public func swiftCallbackFunc67(f: (Double, F67_S0, Float, F67_S1, Int16, UInt, F67_S2, UInt16, UInt, UInt, F67_S3, UInt64) -> Int32) -> Int32 {
    return f(2365334314089079, F67_S0(f0: 1133369490, f1: 54, f2: 244, f3: 411611102), 4453912, F67_S1(f0: 837821989), -3824, 2394019088612006082, F67_S2(f0: 2219661088889353540, f1: 294254132, f2: 5363897228951721947, f3: 2038380379, f4: F67_S2_S0(f0: 8364879421385869437)), 27730, 1854446871602777695, 5020910156102352016, F67_S3(f0: -2211, f1: 5910581461792482729, f2: 9095210648679611609, f3: 6138428), 4274242076331880276)
}

@frozen
public struct F68_S0_S0
{
    public let f0 : Int8;
}

@frozen
public struct F68_S0
{
    public let f0 : Int64;
    public let f1 : F68_S0_S0;
}

@frozen
public struct F68_S1
{
    public let f0 : UInt16;
}

@frozen
public struct F68_S2_S0
{
    public let f0 : UInt;
}

@frozen
public struct F68_S2_S1_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F68_S2_S1
{
    public let f0 : F68_S2_S1_S0;
}

@frozen
public struct F68_S2
{
    public let f0 : F68_S2_S0;
    public let f1 : F68_S2_S1;
}

@frozen
public struct F68_S3
{
    public let f0 : Int16;
}

@frozen
public struct F68_Ret
{
    public let f0 : UInt16;
    public let f1 : Int64;
}

public func swiftCallbackFunc68(f: (UInt8, Float, Int32, Int, F68_S0, Int16, Int, Int32, Int, F68_S1, Double, F68_S2, F68_S3) -> F68_Ret) -> F68_Ret {
    return f(203, 7725681, 323096997, 7745650233784541800, F68_S0(f0: 4103074885750473230, f1: F68_S0_S0(f0: 12)), 28477, 3772772447290536725, 1075348149, 2017898311184593242, F68_S1(f0: 60280), 4052387873895590, F68_S2(f0: F68_S2_S0(f0: 1321857087602747558), f1: F68_S2_S1(f0: F68_S2_S1_S0(f0: 9011155097138053416))), F68_S3(f0: 8332))
}

@frozen
public struct F69_S0_S0
{
    public let f0 : UInt64;
}

@frozen
public struct F69_S0
{
    public let f0 : F69_S0_S0;
}

@frozen
public struct F69_S1
{
    public let f0 : Int64;
}

@frozen
public struct F69_S2
{
    public let f0 : Int32;
}

@frozen
public struct F69_S3
{
    public let f0 : UInt8;
}

@frozen
public struct F69_S4_S0
{
    public let f0 : Int64;
}

@frozen
public struct F69_S4
{
    public let f0 : F69_S4_S0;
}

@frozen
public struct F69_Ret
{
    public let f0 : UInt8;
    public let f1 : Int64;
    public let f2 : UInt32;
}

public func swiftCallbackFunc69(f: (F69_S0, Int, Int32, F69_S1, UInt32, Int8, F69_S2, Int, F69_S3, F69_S4) -> F69_Ret) -> F69_Ret {
    return f(F69_S0(f0: F69_S0_S0(f0: 7154553222175076145)), 6685908100026425691, 1166526155, F69_S1(f0: 6042278185730963289), 182060391, 45, F69_S2(f0: 1886331345), 485542148877875333, F69_S3(f0: 209), F69_S4(f0: F69_S4_S0(f0: 6856847647688321191)))
}

@frozen
public struct F70_S0
{
    public let f0 : Int64;
}

@frozen
public struct F70_S1
{
    public let f0 : Int;
    public let f1 : Double;
    public let f2 : Int16;
}

@frozen
public struct F70_S2
{
    public let f0 : UInt32;
}

@frozen
public struct F70_S3
{
    public let f0 : UInt16;
    public let f1 : Double;
    public let f2 : UInt8;
    public let f3 : UInt64;
    public let f4 : Int32;
}

@frozen
public struct F70_S4_S0
{
    public let f0 : UInt;
}

@frozen
public struct F70_S4
{
    public let f0 : F70_S4_S0;
}

@frozen
public struct F70_Ret
{
    public let f0 : Int8;
    public let f1 : UInt32;
    public let f2 : UInt64;
    public let f3 : Int16;
    public let f4 : Int16;
}

public func swiftCallbackFunc70(f: (Int16, UInt8, Int, UInt32, F70_S0, Int32, F70_S1, F70_S2, F70_S3, Int64, Int32, UInt16, Int, Int, UInt, F70_S4) -> F70_Ret) -> F70_Ret {
    return f(-13167, 126, 3641983584484741827, 1090448265, F70_S0(f0: 3696858216713616004), 1687025402, F70_S1(f0: 714916953527626038, f1: 459810445900614, f2: 4276), F70_S2(f0: 529194028), F70_S3(f0: 40800, f1: 3934985905568056, f2: 230, f3: 7358783417346157372, f4: 187926922), 228428560763393434, 146501405, 58804, 7098488973446286248, 1283658442251334575, 3644681944588099582, F70_S4(f0: F70_S4_S0(f0: 8197135412164695911)))
}

@frozen
public struct F71_S0_S0
{
    public let f0 : Int32;
}

@frozen
public struct F71_S0
{
    public let f0 : F71_S0_S0;
}

@frozen
public struct F71_S1
{
    public let f0 : Int64;
}

public func swiftCallbackFunc71(f: (F71_S0, F71_S1) -> UInt64) -> UInt64 {
    return f(F71_S0(f0: F71_S0_S0(f0: 258165353)), F71_S1(f0: 8603744544763953916))
}

@frozen
public struct F72_S0
{
    public let f0 : Int32;
}

@frozen
public struct F72_Ret
{
    public let f0 : UInt32;
    public let f1 : Float;
    public let f2 : Float;
    public let f3 : Int64;
}

public func swiftCallbackFunc72(f: (F72_S0, Int64, Int8) -> F72_Ret) -> F72_Ret {
    return f(F72_S0(f0: 2021509367), 2480039820482100351, 91)
}

@frozen
public struct F73_S0
{
    public let f0 : Int32;
}

@frozen
public struct F73_S1_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F73_S1
{
    public let f0 : F73_S1_S0;
}

@frozen
public struct F73_S2
{
    public let f0 : Int32;
    public let f1 : Float;
}

@frozen
public struct F73_S3
{
    public let f0 : UInt;
    public let f1 : Int16;
    public let f2 : Int8;
}

@frozen
public struct F73_S4
{
    public let f0 : Int16;
}

@frozen
public struct F73_S5
{
    public let f0 : UInt32;
}

public func swiftCallbackFunc73(f: (Double, Float, F73_S0, Int64, F73_S1, F73_S2, Int16, Double, Int8, Int32, Int64, F73_S3, UInt, UInt64, Int32, F73_S4, UInt8, F73_S5) -> Int8) -> Int8 {
    return f(3038361048801008, 7870661, F73_S0(f0: 1555231180), 7433951069104961, F73_S1(f0: F73_S1_S0(f0: 63298)), F73_S2(f0: 1759846580, f1: 1335901), 11514, 695278874601974, 108, 48660527, 7762050749172332624, F73_S3(f0: 7486686356276472663, f1: 11622, f2: 112), 884183974530885885, 7434462110419085390, 170242607, F73_S4(f0: -26039), 41, F73_S5(f0: 191302504))
}

@frozen
public struct F74_S0_S0
{
    public let f0 : UInt16;
    public let f1 : UInt;
    public let f2 : Int8;
}

@frozen
public struct F74_S0
{
    public let f0 : F74_S0_S0;
    public let f1 : Int;
}

@frozen
public struct F74_S1
{
    public let f0 : Float;
}

public func swiftCallbackFunc74(f: (F74_S0, F74_S1, Int16) -> Int64) -> Int64 {
    return f(F74_S0(f0: F74_S0_S0(f0: 59883, f1: 5554216411943233256, f2: 126), f1: 724541378819571203), F74_S1(f0: 172601), 27932)
}

@frozen
public struct F75_S0
{
    public let f0 : Int64;
}

@frozen
public struct F75_S1_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F75_S1
{
    public let f0 : F75_S1_S0;
}

@frozen
public struct F75_S2
{
    public let f0 : Int8;
}

@frozen
public struct F75_S3_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F75_S3
{
    public let f0 : F75_S3_S0;
}

@frozen
public struct F75_Ret
{
    public let f0 : UInt8;
    public let f1 : Double;
    public let f2 : Double;
    public let f3 : Int64;
    public let f4 : UInt32;
}

public func swiftCallbackFunc75(f: (Int8, Int8, Int8, F75_S0, F75_S1, F75_S2, F75_S3) -> F75_Ret) -> F75_Ret {
    return f(-105, 71, 108, F75_S0(f0: 7224638108479292438), F75_S1(f0: F75_S1_S0(f0: 126)), F75_S2(f0: -88), F75_S3(f0: F75_S3_S0(f0: 4934)))
}

@frozen
public struct F76_S0
{
    public let f0 : UInt16;
    public let f1 : Int;
}

@frozen
public struct F76_S1_S0
{
    public let f0 : Int;
}

@frozen
public struct F76_S1
{
    public let f0 : F76_S1_S0;
    public let f1 : UInt;
    public let f2 : Double;
}

@frozen
public struct F76_S2
{
    public let f0 : UInt64;
    public let f1 : Int;
    public let f2 : UInt16;
}

@frozen
public struct F76_S3_S0
{
    public let f0 : Int64;
}

@frozen
public struct F76_S3
{
    public let f0 : F76_S3_S0;
}

@frozen
public struct F76_S4
{
    public let f0 : Int64;
}

@frozen
public struct F76_S5
{
    public let f0 : UInt;
    public let f1 : Double;
}

public func swiftCallbackFunc76(f: (UInt8, F76_S0, Int8, F76_S1, F76_S2, F76_S3, UInt32, F76_S4, UInt8, F76_S5, Double, Int16) -> UInt64) -> UInt64 {
    return f(69, F76_S0(f0: 25503, f1: 4872234474620951743), 43, F76_S1(f0: F76_S1_S0(f0: 1199076663426903579), f1: 4639522222462236688, f2: 4082956091930029), F76_S2(f0: 5171821618947987626, f1: 3369410144919558564, f2: 5287), F76_S3(f0: F76_S3_S0(f0: 929854460912895550)), 1208311201, F76_S4(f0: 7033993025788649145), 58, F76_S5(f0: 1401399014740601512, f1: 2523645319232571), 230232835550369, -22975)
}

@frozen
public struct F77_S0
{
    public let f0 : Int64;
    public let f1 : Double;
    public let f2 : UInt;
}

@frozen
public struct F77_S1
{
    public let f0 : Int16;
    public let f1 : Float;
    public let f2 : Float;
    public let f3 : Int64;
    public let f4 : Int64;
}

@frozen
public struct F77_S2
{
    public let f0 : UInt16;
    public let f1 : Int8;
    public let f2 : Int32;
    public let f3 : Float;
    public let f4 : Float;
}

@frozen
public struct F77_Ret
{
    public let f0 : Double;
    public let f1 : UInt16;
    public let f2 : Int8;
    public let f3 : UInt;
}

public func swiftCallbackFunc77(f: (Double, F77_S0, F77_S1, F77_S2, UInt32) -> F77_Ret) -> F77_Ret {
    return f(1623173949127682, F77_S0(f0: 5204451347781433070, f1: 3469485630755805, f2: 7586276835848725004), F77_S1(f0: 2405, f1: 2419792, f2: 6769317, f3: 1542327522833750776, f4: 1297586130846695275), F77_S2(f0: 10102, f1: -48, f2: 14517107, f3: 4856023, f4: 2681358), 1463251524)
}

@frozen
public struct F78_S0
{
    public let f0 : UInt;
    public let f1 : Int;
}

@frozen
public struct F78_S1_S0
{
    public let f0 : Int8;
}

@frozen
public struct F78_S1
{
    public let f0 : Int16;
    public let f1 : UInt64;
    public let f2 : F78_S1_S0;
    public let f3 : Int32;
    public let f4 : Int;
}

@frozen
public struct F78_S2
{
    public let f0 : UInt;
    public let f1 : UInt64;
}

@frozen
public struct F78_S3
{
    public let f0 : UInt64;
}

@frozen
public struct F78_S4
{
    public let f0 : UInt64;
}

public func swiftCallbackFunc78(f: (UInt64, F78_S0, UInt64, F78_S1, F78_S2, Int32, UInt64, Int64, F78_S3, Float, Float, UInt16, F78_S4, Double) -> Double) -> Double {
    return f(6780767594736146373, F78_S0(f0: 6264193481541646332, f1: 6600856439035088503), 1968254881389492170, F78_S1(f0: -17873, f1: 5581169895682201971, f2: F78_S1_S0(f0: 127), f3: 1942346704, f4: 118658265323815307), F78_S2(f0: 1489326778640378879, f1: 1427061853707270770), 858391966, 5830110056171302270, 2953614358173898788, F78_S3(f0: 6761452244699684409), 3452451, 3507119, 40036, F78_S4(f0: 4800085294404376817), 780368756754436)
}

@frozen
public struct F79_S0_S0
{
    public let f0 : UInt;
}

@frozen
public struct F79_S0
{
    public let f0 : F79_S0_S0;
    public let f1 : Int;
}

@frozen
public struct F79_Ret
{
    public let f0 : UInt32;
    public let f1 : UInt64;
    public let f2 : Double;
}

public func swiftCallbackFunc79(f: (F79_S0, Float) -> F79_Ret) -> F79_Ret {
    return f(F79_S0(f0: F79_S0_S0(f0: 1013911700897046117), f1: 7323935615297665289), 5159506)
}

@frozen
public struct F80_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F80_S1_S0_S0
{
    public let f0 : UInt8;
}

@frozen
public struct F80_S1_S0
{
    public let f0 : F80_S1_S0_S0;
}

@frozen
public struct F80_S1
{
    public let f0 : Int;
    public let f1 : F80_S1_S0;
}

@frozen
public struct F80_S2
{
    public let f0 : UInt64;
}

public func swiftCallbackFunc80(f: (UInt64, Int, Int32, Int16, UInt, F80_S0, Int16, Int, Int8, Int32, UInt32, F80_S1, F80_S2, UInt64) -> Float) -> Float {
    return f(4470427843910624516, 8383677749057878551, 2017117925, -10531, 3438375001906177611, F80_S0(f0: 65220), 7107, 7315288835693680178, -48, 813870434, 1092037477, F80_S1(f0: 7104962838387954470, f1: F80_S1_S0(f0: F80_S1_S0_S0(f0: 236))), F80_S2(f0: 7460392384225808790), 364121728483540667)
}

@frozen
public struct F81_S0
{
    public let f0 : Float;
    public let f1 : Float;
    public let f2 : Int;
    public let f3 : Int;
    public let f4 : Int;
}

@frozen
public struct F81_Ret
{
    public let f0 : Int;
}

public func swiftCallbackFunc81(f: (UInt8, UInt32, UInt8, F81_S0, Int8) -> F81_Ret) -> F81_Ret {
    return f(53, 57591489, 19, F81_S0(f0: 5675845, f1: 6469988, f2: 5775316279348621124, f3: 7699091894067057939, f4: 1049086627558950131), 15)
}

@frozen
public struct F82_S0_S0
{
    public let f0 : Float;
    public let f1 : UInt;
    public let f2 : UInt16;
}

@frozen
public struct F82_S0
{
    public let f0 : UInt;
    public let f1 : F82_S0_S0;
    public let f2 : UInt16;
}

@frozen
public struct F82_S1
{
    public let f0 : Int32;
}

@frozen
public struct F82_S2
{
    public let f0 : Int;
}

@frozen
public struct F82_S3_S0
{
    public let f0 : Int32;
}

@frozen
public struct F82_S3
{
    public let f0 : Double;
    public let f1 : UInt;
    public let f2 : F82_S3_S0;
}

@frozen
public struct F82_S4
{
    public let f0 : UInt64;
}

public func swiftCallbackFunc82(f: (Int64, F82_S0, Int16, Int8, UInt32, F82_S1, Int32, Int64, Int8, Double, F82_S2, F82_S3, F82_S4) -> Float) -> Float {
    return f(6454754584537364459, F82_S0(f0: 6703634779264968131, f1: F82_S0_S0(f0: 1010059, f1: 4772968591609202284, f2: 64552), f2: 47126), 9869, -8, 1741550381, F82_S1(f0: 705741282), 1998781399, 7787961471254401526, -27, 4429830670351707, F82_S2(f0: 4975772762589349422), F82_S3(f0: 1423948098664774, f1: 504607538824251986, f2: F82_S3_S0(f0: 1940911018)), F82_S4(f0: 2988623645681463667))
}

@frozen
public struct F83_S0
{
    public let f0 : Int32;
}

@frozen
public struct F83_Ret
{
    public let f0 : Int16;
}

public func swiftCallbackFunc83(f: (Int8, F83_S0, Int16) -> F83_Ret) -> F83_Ret {
    return f(17, F83_S0(f0: 530755056), -11465)
}

@frozen
public struct F84_S0
{
    public let f0 : UInt;
    public let f1 : UInt32;
    public let f2 : UInt;
    public let f3 : UInt64;
    public let f4 : Int32;
}

@frozen
public struct F84_S1
{
    public let f0 : UInt;
}

@frozen
public struct F84_S2
{
    public let f0 : Float;
}

@frozen
public struct F84_S3
{
    public let f0 : UInt8;
}

@frozen
public struct F84_S4
{
    public let f0 : Int16;
}

@frozen
public struct F84_S5
{
    public let f0 : Int;
    public let f1 : Int16;
}

@frozen
public struct F84_S6
{
    public let f0 : Int16;
}

@frozen
public struct F84_S7
{
    public let f0 : Int32;
}

public func swiftCallbackFunc84(f: (Int32, F84_S0, F84_S1, Double, Int32, Int16, Double, F84_S2, F84_S3, Double, F84_S4, F84_S5, F84_S6, F84_S7, UInt) -> Int) -> Int {
    return f(1605022009, F84_S0(f0: 6165049220831866664, f1: 1235491183, f2: 7926620970405586826, f3: 2633248816907294140, f4: 2012834055), F84_S1(f0: 2881830362339122988), 4065309434963087, 1125165825, -32360, 1145602045200029, F84_S2(f0: 5655563), F84_S3(f0: 14), 3919593995303128, F84_S4(f0: 26090), F84_S5(f0: 8584898862398781737, f1: -5185), F84_S6(f0: 144), F84_S7(f0: 2138004352), 9102562043027810686)
}

@frozen
public struct F85_S0
{
    public let f0 : Double;
    public let f1 : Double;
    public let f2 : Int8;
    public let f3 : Int32;
}

@frozen
public struct F85_S1
{
    public let f0 : Int64;
    public let f1 : UInt16;
    public let f2 : UInt64;
    public let f3 : UInt;
}

@frozen
public struct F85_S2
{
    public let f0 : Float;
    public let f1 : Float;
    public let f2 : UInt32;
}

@frozen
public struct F85_S3
{
    public let f0 : UInt8;
}

@frozen
public struct F85_S4
{
    public let f0 : UInt;
}

@frozen
public struct F85_S5
{
    public let f0 : Double;
}

@frozen
public struct F85_Ret
{
    public let f0 : UInt32;
    public let f1 : UInt16;
    public let f2 : Int32;
    public let f3 : Double;
    public let f4 : Int;
    public let f5 : UInt64;
    public let f6 : Int64;
}

public func swiftCallbackFunc85(f: (F85_S0, F85_S1, UInt32, F85_S2, Int64, F85_S3, Int64, F85_S4, UInt16, UInt8, Int32, UInt32, Int32, Float, F85_S5, Int64) -> F85_Ret) -> F85_Ret {
    return f(F85_S0(f0: 4325646965362202, f1: 3313084380250914, f2: 42, f3: 2034100272), F85_S1(f0: 1365643665271339575, f1: 25442, f2: 3699631470459352980, f3: 7611776251925132200), 911446742, F85_S2(f0: 352423, f1: 7150341, f2: 2090089360), 5731257538910387688, F85_S3(f0: 171), 5742887585483060342, F85_S4(f0: 1182236975680416316), 32137, 44, 2143531010, 1271996557, 1035188446, 1925443, F85_S5(f0: 2591574394337603), 721102428782331317)
}

@frozen
public struct F86_S0
{
    public let f0 : Int;
    public let f1 : Float;
    public let f2 : Int16;
    public let f3 : Int8;
}

@frozen
public struct F86_S1
{
    public let f0 : Double;
}

@frozen
public struct F86_S2
{
    public let f0 : Int;
    public let f1 : Float;
}

@frozen
public struct F86_S3
{
    public let f0 : UInt16;
    public let f1 : Float;
}

@frozen
public struct F86_Ret
{
    public let f0 : Int16;
    public let f1 : UInt32;
    public let f2 : Double;
    public let f3 : UInt8;
}

public func swiftCallbackFunc86(f: (Float, Int16, Int, Int16, Float, F86_S0, F86_S1, F86_S2, Int, UInt32, UInt, UInt, Float, Int64, F86_S3, UInt) -> F86_Ret) -> F86_Ret {
    return f(2913632, 3735, 2773655476379499086, 22973, 8292778, F86_S0(f0: 5562042565258891920, f1: 8370233, f2: 18292, f3: -32), F86_S1(f0: 486951152980016), F86_S2(f0: 170033426151098456, f1: 3867810), 7390780928011218856, 1504267943, 2046987193814931100, 4860202472307588968, 1644019, 8084012412562897328, F86_S3(f0: 46301, f1: 5633701), 1911608136082175332)
}

@frozen
public struct F87_S0
{
    public let f0 : Int32;
    public let f1 : Int16;
    public let f2 : Int32;
}

@frozen
public struct F87_S1
{
    public let f0 : Float;
}

public func swiftCallbackFunc87(f: (Float, Int, F87_S0, F87_S1) -> UInt64) -> UInt64 {
    return f(1413086, 4206825694012787823, F87_S0(f0: 70240457, f1: 30503, f2: 671751848), F87_S1(f0: 6641304))
}

@frozen
public struct F88_S0
{
    public let f0 : Int8;
    public let f1 : Int16;
    public let f2 : UInt8;
    public let f3 : Double;
    public let f4 : UInt16;
}

@frozen
public struct F88_S1
{
    public let f0 : Double;
    public let f1 : UInt8;
}

@frozen
public struct F88_S2
{
    public let f0 : UInt;
}

@frozen
public struct F88_S3
{
    public let f0 : Int8;
    public let f1 : UInt32;
}

@frozen
public struct F88_Ret
{
    public let f0 : Int32;
    public let f1 : UInt32;
    public let f2 : Int;
    public let f3 : UInt64;
}

public func swiftCallbackFunc88(f: (F88_S0, F88_S1, Float, UInt, Float, Int, F88_S2, UInt64, F88_S3, UInt64) -> F88_Ret) -> F88_Ret {
    return f(F88_S0(f0: 125, f1: -10705, f2: 21, f3: 361845689097003, f4: 41749), F88_S1(f0: 1754583995806427, f1: 178), 4705205, 5985040566226273121, 2484194, 1904196135427766362, F88_S2(f0: 5436710892090266406), 4250368992471675181, F88_S3(f0: -87, f1: 362108395), 3388632419732870796)
}

@frozen
public struct F89_S0
{
    public let f0 : Double;
}

@frozen
public struct F89_Ret_S0
{
    public let f0 : Double;
}

@frozen
public struct F89_Ret
{
    public let f0 : Int32;
    public let f1 : F89_Ret_S0;
    public let f2 : UInt;
    public let f3 : Int64;
}

public func swiftCallbackFunc89(f: (F89_S0) -> F89_Ret) -> F89_Ret {
    return f(F89_S0(f0: 2137010348736191))
}

@frozen
public struct F90_S0_S0_S0
{
    public let f0 : UInt;
}

@frozen
public struct F90_S0_S0
{
    public let f0 : F90_S0_S0_S0;
}

@frozen
public struct F90_S0
{
    public let f0 : F90_S0_S0;
    public let f1 : UInt;
    public let f2 : UInt32;
    public let f3 : Int64;
    public let f4 : Int16;
}

@frozen
public struct F90_S1
{
    public let f0 : UInt16;
    public let f1 : Int16;
}

@frozen
public struct F90_S2
{
    public let f0 : Int;
}

@frozen
public struct F90_S3
{
    public let f0 : UInt;
}

@frozen
public struct F90_S4
{
    public let f0 : UInt64;
}

@frozen
public struct F90_Ret
{
    public let f0 : Int16;
    public let f1 : Int;
}

public func swiftCallbackFunc90(f: (Int64, Float, F90_S0, UInt32, UInt16, F90_S1, F90_S2, F90_S3, F90_S4) -> F90_Ret) -> F90_Ret {
    return f(920081051198141017, 661904, F90_S0(f0: F90_S0_S0(f0: F90_S0_S0_S0(f0: 3898354148166517637)), f1: 1003118682503285076, f2: 1418362079, f3: 3276689793574299746, f4: -18559), 1773011602, 32638, F90_S1(f0: 47129, f1: -31849), F90_S2(f0: 4795020225668482328), F90_S3(f0: 5307513663902191175), F90_S4(f0: 7057074401404034083))
}

@frozen
public struct F91_S0
{
    public let f0 : Int8;
    public let f1 : Int;
    public let f2 : UInt16;
    public let f3 : UInt16;
}

@frozen
public struct F91_S1
{
    public let f0 : Double;
    public let f1 : UInt64;
    public let f2 : Int8;
    public let f3 : Int64;
    public let f4 : Float;
}

@frozen
public struct F91_S2_S0_S0
{
    public let f0 : Int64;
}

@frozen
public struct F91_S2_S0
{
    public let f0 : F91_S2_S0_S0;
}

@frozen
public struct F91_S2
{
    public let f0 : Double;
    public let f1 : F91_S2_S0;
    public let f2 : Int16;
}

@frozen
public struct F91_S3_S0
{
    public let f0 : UInt;
}

@frozen
public struct F91_S3
{
    public let f0 : F91_S3_S0;
}

@frozen
public struct F91_Ret
{
    public let f0 : Int64;
    public let f1 : UInt64;
    public let f2 : Int16;
    public let f3 : UInt32;
}

public func swiftCallbackFunc91(f: (F91_S0, Int16, UInt32, Double, F91_S1, Int64, UInt64, Float, F91_S2, Int, F91_S3) -> F91_Ret) -> F91_Ret {
    return f(F91_S0(f0: -117, f1: 6851485542307521521, f2: 23224, f3: 28870), -26318, 874052395, 3651199868446152, F91_S1(f0: 3201729800438540, f1: 7737032265509566019, f2: 123, f3: 7508633930609553617, f4: 8230501), 2726677037673277403, 4990410590084533996, 3864639, F91_S2(f0: 1763083442463892, f1: F91_S2_S0(f0: F91_S2_S0_S0(f0: 6783710957456602933)), f2: 2927), 3359440517385934325, F91_S3(f0: F91_S3_S0(f0: 3281136825102667421)))
}

@frozen
public struct F92_S0
{
    public let f0 : Double;
    public let f1 : Double;
}

@frozen
public struct F92_S1
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : UInt32;
    public let f3 : Int16;
    public let f4 : UInt64;
}

@frozen
public struct F92_S2_S0
{
    public let f0 : UInt16;
}

@frozen
public struct F92_S2
{
    public let f0 : UInt32;
    public let f1 : Int64;
    public let f2 : F92_S2_S0;
}

@frozen
public struct F92_Ret
{
    public let f0 : Int32;
}

public func swiftCallbackFunc92(f: (UInt32, Int64, F92_S0, Int, UInt8, F92_S1, F92_S2, UInt8, Int, Int32) -> F92_Ret) -> F92_Ret {
    return f(479487770, 3751818229732502126, F92_S0(f0: 3486664439392893, f1: 1451061144702448), 1103649059951788126, 17, F92_S1(f0: 1542537473, f1: 2256304993713022795, f2: 1773847876, f3: -4712, f4: 2811859744132572185), F92_S2(f0: 290315682, f1: 4847587202070249866, f2: F92_S2_S0(f0: 20774)), 8, 2206063999764082749, 1481391120)
}

@frozen
public struct F93_S0
{
    public let f0 : Int8;
    public let f1 : UInt32;
}

@frozen
public struct F93_S1
{
    public let f0 : UInt32;
}

@frozen
public struct F93_Ret
{
    public let f0 : Int;
    public let f1 : UInt64;
}

public func swiftCallbackFunc93(f: (UInt, UInt16, Double, F93_S0, F93_S1) -> F93_Ret) -> F93_Ret {
    return f(5170226481546239050, 2989, 1630717078645270, F93_S0(f0: -46, f1: 859171256), F93_S1(f0: 254449240))
}

@frozen
public struct F94_S0
{
    public let f0 : UInt;
}

@frozen
public struct F94_S1
{
    public let f0 : Int32;
    public let f1 : UInt;
}

@frozen
public struct F94_S2
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : UInt16;
}

@frozen
public struct F94_S3
{
    public let f0 : UInt8;
    public let f1 : Int32;
    public let f2 : Float;
}

@frozen
public struct F94_S4
{
    public let f0 : Int32;
    public let f1 : Int64;
    public let f2 : Float;
}

@frozen
public struct F94_S5
{
    public let f0 : Int16;
    public let f1 : UInt;
    public let f2 : Int16;
    public let f3 : Int8;
}

@frozen
public struct F94_Ret
{
    public let f0 : Int64;
}

public func swiftCallbackFunc94(f: (F94_S0, Int16, F94_S1, F94_S2, F94_S3, Float, F94_S4, UInt32, F94_S5, Int16) -> F94_Ret) -> F94_Ret {
    return f(F94_S0(f0: 8626725032375870186), -7755, F94_S1(f0: 544707027, f1: 2251410026467996594), F94_S2(f0: 2972912419231960385, f1: 740529487, f2: 34526), F94_S3(f0: 41, f1: 1598856955, f2: 5126603), 7242977, F94_S4(f0: 473684762, f1: 4023878650965716094, f2: 2777693), 1612378906, F94_S5(f0: -17074, f1: 2666903737827472071, f2: 418, f3: 106), -14547)
}

@frozen
public struct F95_S0
{
    public let f0 : UInt16;
    public let f1 : Int64;
}

@frozen
public struct F95_S1
{
    public let f0 : UInt32;
    public let f1 : Int16;
    public let f2 : Double;
}

@frozen
public struct F95_S2
{
    public let f0 : UInt16;
}

@frozen
public struct F95_Ret_S0
{
    public let f0 : Int16;
}

@frozen
public struct F95_Ret
{
    public let f0 : Int;
    public let f1 : Int16;
    public let f2 : Int8;
    public let f3 : UInt8;
    public let f4 : F95_Ret_S0;
}

public func swiftCallbackFunc95(f: (F95_S0, UInt, F95_S1, F95_S2) -> F95_Ret) -> F95_Ret {
    return f(F95_S0(f0: 45388, f1: 6620047889014935849), 97365157264460373, F95_S1(f0: 357234637, f1: -13720, f2: 3313430568949662), F95_S2(f0: 14248))
}

@frozen
public struct F96_S0
{
    public let f0 : Int64;
    public let f1 : UInt32;
    public let f2 : Int16;
    public let f3 : Double;
    public let f4 : Double;
}

@frozen
public struct F96_S1
{
    public let f0 : UInt64;
}

@frozen
public struct F96_S2
{
    public let f0 : Float;
}

public func swiftCallbackFunc96(f: (UInt32, F96_S0, Float, UInt64, UInt32, UInt32, F96_S1, F96_S2, Int64) -> UInt64) -> UInt64 {
    return f(1103144790, F96_S0(f0: 496343164737276588, f1: 1541085564, f2: -16271, f3: 1062575289573718, f4: 570255786498865), 7616839, 7370881799887414383, 390392554, 1492692139, F96_S1(f0: 1666031716012978365), F96_S2(f0: 3427394), 4642371619161527189)
}

@frozen
public struct F97_S0
{
    public let f0 : Int8;
}

@frozen
public struct F97_S1
{
    public let f0 : Int64;
    public let f1 : UInt64;
}

@frozen
public struct F97_S2
{
    public let f0 : UInt8;
    public let f1 : Int64;
}

@frozen
public struct F97_S3
{
    public let f0 : Double;
}

@frozen
public struct F97_Ret_S0
{
    public let f0 : Int32;
}

@frozen
public struct F97_Ret
{
    public let f0 : Double;
    public let f1 : UInt;
    public let f2 : F97_Ret_S0;
    public let f3 : UInt16;
    public let f4 : UInt32;
}

public func swiftCallbackFunc97(f: (F97_S0, F97_S1, F97_S2, F97_S3) -> F97_Ret) -> F97_Ret {
    return f(F97_S0(f0: -87), F97_S1(f0: 1414208343412494909, f1: 453284654311256466), F97_S2(f0: 224, f1: 1712859616922087053), F97_S3(f0: 3987671154739178))
}

@frozen
public struct F98_S0
{
    public let f0 : Int32;
}

public func swiftCallbackFunc98(f: (Float, UInt16, F98_S0, UInt16) -> Int) -> Int {
    return f(2863898, 37573, F98_S0(f0: 1073068257), 53560)
}

@frozen
public struct F99_S0
{
    public let f0 : Int;
    public let f1 : UInt32;
    public let f2 : Int32;
    public let f3 : UInt32;
}

@frozen
public struct F99_S1
{
    public let f0 : Int16;
}

@frozen
public struct F99_S2
{
    public let f0 : UInt8;
}

public func swiftCallbackFunc99(f: (Int64, UInt, Float, UInt16, F99_S0, UInt8, Float, UInt8, Int8, F99_S1, F99_S2) -> UInt64) -> UInt64 {
    return f(1152281003884062246, 2482384127373829622, 3361150, 2121, F99_S0(f0: 4484545590050696958, f1: 422528630, f2: 1418346646, f3: 1281567856), 223, 1917656, 103, -46, F99_S1(f0: 14554), F99_S2(f0: 68))
}

