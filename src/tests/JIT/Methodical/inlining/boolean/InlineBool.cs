// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests for boolean expression, control flow, and inlining optimizations.
// All 100 variants of Id should generate the same code.

using System;
using System.Runtime.CompilerServices;
using Xunit;
namespace InlineBool
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False00() { return false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False01() { return !true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False02() { return Id00(False00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False03() { return Not00(True00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False04() { return Id00(Id00(False00())); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False05() { return Not00(Not00(False00())); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True00() { return true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True01() { return !false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True02() { return Id00(True00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True03() { return Not00(False00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True04() { return Id00(Id00(True00())); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True05() { return Not00(Not00(True00())); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not00(bool x) { return !x; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not01(bool x) { return x == false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not02(bool x) { return Not00(Id00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not03(bool x) { return Id00(Not00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not04(bool x) { return Id00(!x); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not05(bool x) { return !Id00(x); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id00(bool x) { return x; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id01(bool x) { return x == true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id02(bool x) { return Not00(Not00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id03(bool x) { return Id00(Id00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id04(bool x) { return Id00(x); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id05(bool x) { return !Id00(!x); }
        static bool Id06(bool x) { return x ? true : false; }
        static bool Id07(bool x) { return !x ? false : true; }
        static bool Id08(bool x) { if (x) return true; else return false; }
        static bool Id09(bool x) { if (!x) return false; else return true; }
        static bool Id10(bool x) { return Id00(x) ? true : false; }
        static bool Id11(bool x) { return Not00(x) ? false : true; }
        static bool Id12(bool x) { if (x) return x; else return x; }
        static bool Id13(bool x) { if (!x) return x; else return x; }
        static bool Id14(bool x) { return x ? x : x; }
        static bool Id15(bool x) { return !x ? x : x; }
        static bool Id16(bool x) { return x == true ? true : false; }
        static bool Id17(bool x) { return x == false ? false : true; }
        static bool Id18(bool x) { return x != true ? false : true; }
        static bool Id19(bool x) { return x != false ? true : false; }
        static bool Id20(bool x) { return !x == true ? false : true; }
        static bool Id21(bool x) { return !x == false ? true : false; }
        static bool Id22(bool x) { return !!x; }
        static bool Id23(bool x) { return !!!!x; }
        static bool Id24(bool x) { return !!!!!!x; }
        static bool Id25(bool x) { return !!!!!!!!x; }
        static bool Id26(bool x) { return !!x ? !!x : !!x; }
        static bool Id27(bool x) { return !x ? !!x : !!x;  }
        static bool Id28(bool x) { return x & x; }
        static bool Id29(bool x) { return x && x; }
        static bool Id30(bool x) { return x | x; }
        static bool Id31(bool x) { return x | x & x | x; }
        static bool Id32(bool x) { return x || x; }
        static bool Id33(bool x) { return x & x ? true : false; }
        static bool Id34(bool x) { return x | x ? true : false; }
        static bool Id35(bool x) { return x && x ? true : false; }
        static bool Id36(bool x) { return x || x ? true : false; }
        static bool Id37(bool x) { return x && x && x ? true : false; }
        static bool Id38(bool x) { return x || x || x ? true : false; }
        static bool Id39(bool x) { return x | x & x | x ? ! x ^ x : x ^ x; }
        static bool Id40(bool x) { return !!Id00(x); }
        static bool Id41(bool x) { return x == True00(); }
        static bool Id42(bool x) { return !Not00(!Not00(x)); }
        static bool Id43(bool x) { return !Id00(!Id00(x)); }
        static bool Id44(bool x) { return Id00(!!x); }
        static bool Id45(bool x) { return !!Id00(!!x); }
        static bool Id46(bool x) { return x ? True00() : False00(); }
        static bool Id47(bool x) { return !x ? False00() : True00(); }
        static bool Id48(bool x) { if (x) return True00(); else return False00(); }
        static bool Id49(bool x) { if (!x) return False00(); else return True00(); }
        static bool Id50(bool x) { return Id00(x) ? True00() : False00(); }
        static bool Id51(bool x) { return Not00(x) ? False00() : True00(); }
        static bool Id52(bool x) { if (Id00(x)) return Id00(x); else return Id00(x); }
        static bool Id53(bool x) { if (!Id00(x)) return Id00(x); else return Id00(x); }
        static bool Id54(bool x) { return Id00(x) ? Id00(x) : Id00(x); }
        static bool Id55(bool x) { return Not00(x) ? Id00(x) : Id00(x); }
        static bool Id56(bool x) { return x == True00() ? true : false; }
        static bool Id57(bool x) { return x == False00() ? false : true; }
        static bool Id58(bool x) { return Id00(x != true) ? false : true; }
        static bool Id59(bool x) { return Id00(x != false) ? true : false; }
        static bool Id60(bool x) { return Id01(x) ? True01() : False01(); }
        static bool Id61(bool x) { return Not01(x) ? False01() : True01(); }
        static bool Id62(bool x) { if (Id01(x)) return Id01(x); else return Id01(x); }
        static bool Id63(bool x) { if (!Id01(x)) return Id01(x); else return Id01(x); }
        static bool Id64(bool x) { return Id01(x) ? Id01(x) : Id01(x); }
        static bool Id65(bool x) { return Not01(x) ? Id01(x) : Id01(x); }
        static bool Id66(bool x) { return x == True01() ? true : false; }
        static bool Id67(bool x) { return x == False01() ? false : true; }
        static bool Id68(bool x) { return Id01(x != true) ? false : true; }
        static bool Id69(bool x) { return Id01(x != false) ? true : false; }
        static bool Id70(bool x) { return Id02(x) ? True02() : False02(); }
        static bool Id71(bool x) { return Not02(x) ? False02() : True02(); }
        static bool Id72(bool x) { if (Id02(x)) return Id02(x); else return Id02(x); }
        static bool Id73(bool x) { if (!Id02(x)) return Id02(x); else return Id02(x); }
        static bool Id74(bool x) { return Id02(x) ? Id02(x) : Id02(x); }
        static bool Id75(bool x) { return Not02(x) ? Id02(x) : Id02(x); }
        static bool Id76(bool x) { return x == True02() ? true : false; }
        static bool Id77(bool x) { return x == False02() ? false : true; }
        static bool Id78(bool x) { return Id02(x != true) ? false : true; }
        static bool Id79(bool x) { return Id02(x != false) ? true : false; }
        static bool Id80(bool x) { return Id03(x) ? True03() : False03(); }
        static bool Id81(bool x) { return Not03(x) ? False03() : True03(); }
        static bool Id82(bool x) { if (Id03(x)) return Id03(x); else return Id03(x); }
        static bool Id83(bool x) { if (!Id03(x)) return Id03(x); else return Id03(x); }
        static bool Id84(bool x) { return Id03(x) ? Id03(x) : Id03(x); }
        static bool Id85(bool x) { return Not03(x) ? Id03(x) : Id03(x); }
        static bool Id86(bool x) { return x == True03() ? true : false; }
        static bool Id87(bool x) { return x == False03() ? false : true; }
        static bool Id88(bool x) { return Id03(x != true) ? false : true; }
        static bool Id89(bool x) { return Id03(x != false) ? true : false; }
        static bool Id90(bool x) { return Id04(x) ? True04() : False04(); }
        static bool Id91(bool x) { return Not04(x) ? False04() : True04(); }
        static bool Id92(bool x) { if (Id04(x)) return Id04(x); else return Id04(x); }
        static bool Id93(bool x) { if (!Id04(x)) return Id04(x); else return Id04(x); }
        static bool Id94(bool x) { return Id04(x) ? Id04(x) : Id04(x); }
        static bool Id95(bool x) { return Not04(x) ? Id04(x) : Id04(x); }
        static bool Id96(bool x) { return x == True04() ? true : false; }
        static bool Id97(bool x) { return x == False04() ? false : true; }
        static bool Id98(bool x) { return Id04(x != true) ? false : true; }
        static bool Id99(bool x) { return Id04(x != false) ? true : false; }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Fact]
        public static int TestEntryPoint()
        {
            bool result = true;

            result &= Id00(true);
            result &= ! Id00(false);
            result &= Id01(true);
            result &= ! Id01(false);
            result &= Id02(true);
            result &= ! Id02(false);
            result &= Id03(true);
            result &= ! Id03(false);
            result &= Id04(true);
            result &= ! Id04(false);
            result &= Id05(true);
            result &= ! Id05(false);
            result &= Id06(true);
            result &= ! Id06(false);
            result &= Id07(true);
            result &= ! Id07(false);
            result &= Id08(true);
            result &= ! Id08(false);
            result &= Id09(true);
            result &= ! Id09(false);

            result &= Id10(true);
            result &= ! Id10(false);
            result &= Id11(true);
            result &= ! Id11(false);
            result &= Id12(true);
            result &= ! Id12(false);
            result &= Id13(true);
            result &= ! Id13(false);
            result &= Id14(true);
            result &= ! Id14(false);
            result &= Id15(true);
            result &= ! Id15(false);
            result &= Id16(true);
            result &= ! Id16(false);
            result &= Id17(true);
            result &= ! Id17(false);
            result &= Id18(true);
            result &= ! Id18(false);
            result &= Id19(true);
            result &= ! Id19(false);

            result &= Id20(true);
            result &= ! Id20(false);
            result &= Id21(true);
            result &= ! Id21(false);
            result &= Id22(true);
            result &= ! Id22(false);
            result &= Id23(true);
            result &= ! Id23(false);
            result &= Id24(true);
            result &= ! Id24(false);
            result &= Id25(true);
            result &= ! Id25(false);
            result &= Id26(true);
            result &= ! Id26(false);
            result &= Id27(true);
            result &= ! Id27(false);
            result &= Id28(true);
            result &= ! Id28(false);
            result &= Id29(true);
            result &= ! Id29(false);

            result &= Id30(true);
            result &= ! Id30(false);
            result &= Id31(true);
            result &= ! Id31(false);
            result &= Id32(true);
            result &= ! Id32(false);
            result &= Id33(true);
            result &= ! Id33(false);
            result &= Id34(true);
            result &= ! Id34(false);
            result &= Id35(true);
            result &= ! Id35(false);
            result &= Id36(true);
            result &= ! Id36(false);
            result &= Id37(true);
            result &= ! Id37(false);
            result &= Id38(true);
            result &= ! Id38(false);
            result &= Id39(true);
            result &= ! Id39(false);

            result &= Id40(true);
            result &= ! Id40(false);
            result &= Id41(true);
            result &= ! Id41(false);
            result &= Id42(true);
            result &= ! Id42(false);
            result &= Id43(true);
            result &= ! Id43(false);
            result &= Id44(true);
            result &= ! Id44(false);
            result &= Id45(true);
            result &= ! Id45(false);
            result &= Id46(true);
            result &= ! Id46(false);
            result &= Id47(true);
            result &= ! Id47(false);
            result &= Id48(true);
            result &= ! Id48(false);
            result &= Id49(true);
            result &= ! Id49(false);

            result &= Id50(true);
            result &= ! Id50(false);
            result &= Id51(true);
            result &= ! Id51(false);
            result &= Id52(true);
            result &= ! Id52(false);
            result &= Id53(true);
            result &= ! Id53(false);
            result &= Id54(true);
            result &= ! Id54(false);
            result &= Id55(true);
            result &= ! Id55(false);
            result &= Id56(true);
            result &= ! Id56(false);
            result &= Id57(true);
            result &= ! Id57(false);
            result &= Id58(true);
            result &= ! Id58(false);
            result &= Id59(true);
            result &= ! Id59(false);

            result &= Id60(true);
            result &= ! Id60(false);
            result &= Id61(true);
            result &= ! Id61(false);
            result &= Id62(true);
            result &= ! Id62(false);
            result &= Id63(true);
            result &= ! Id63(false);
            result &= Id64(true);
            result &= ! Id64(false);
            result &= Id65(true);
            result &= ! Id65(false);
            result &= Id66(true);
            result &= ! Id66(false);
            result &= Id67(true);
            result &= ! Id67(false);
            result &= Id68(true);
            result &= ! Id68(false);
            result &= Id69(true);
            result &= ! Id69(false);

            result &= Id70(true);
            result &= ! Id70(false);
            result &= Id71(true);
            result &= ! Id71(false);
            result &= Id72(true);
            result &= ! Id72(false);
            result &= Id73(true);
            result &= ! Id73(false);
            result &= Id74(true);
            result &= ! Id74(false);
            result &= Id75(true);
            result &= ! Id75(false);
            result &= Id76(true);
            result &= ! Id76(false);
            result &= Id77(true);
            result &= ! Id77(false);
            result &= Id78(true);
            result &= ! Id78(false);
            result &= Id79(true);
            result &= ! Id79(false);

            result &= Id80(true);
            result &= ! Id80(false);
            result &= Id81(true);
            result &= ! Id81(false);
            result &= Id82(true);
            result &= ! Id82(false);
            result &= Id83(true);
            result &= ! Id83(false);
            result &= Id84(true);
            result &= ! Id84(false);
            result &= Id85(true);
            result &= ! Id85(false);
            result &= Id86(true);
            result &= ! Id86(false);
            result &= Id87(true);
            result &= ! Id87(false);
            result &= Id88(true);
            result &= ! Id88(false);
            result &= Id89(true);
            result &= ! Id89(false);

            result &= Id90(true);
            result &= ! Id90(false);
            result &= Id91(true);
            result &= ! Id91(false);
            result &= Id92(true);
            result &= ! Id92(false);
            result &= Id93(true);
            result &= ! Id93(false);
            result &= Id94(true);
            result &= ! Id94(false);
            result &= Id95(true);
            result &= ! Id95(false);
            result &= Id96(true);
            result &= ! Id96(false);
            result &= Id97(true);
            result &= ! Id97(false);
            result &= Id98(true);
            result &= ! Id98(false);
            result &= Id99(true);
            result &= ! Id99(false);

            return result ? 100 : -1;
        }
    }
}
