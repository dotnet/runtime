// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests for boolean expression, control flow, and inlining optimizations.
// All 100 variants of Id should generate the same code.

using System;
using System.Runtime.CompilerServices;
namespace InlineBool
{
    public class Program
    {
        static bool False00() { return false; }
        static bool False01() { return !true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False02() { return Id00(False00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False03() { return Not00(True00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False04() { return Id00(Id00(False00())); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool False05() { return Not00(Not00(False00())); }

        static bool True00() { return true; }
        static bool True01() { return !false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True02() { return Id00(True00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True03() { return Not00(False00()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True04() { return Id00(Id00(True00())); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool True05() { return Not00(Not00(True00())); }

        static bool Id00(bool x) { return x; }
        static bool Id01(bool x) { return x == true; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id02(bool x) { return Not00(Not00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id03(bool x) { return Id00(Id00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Id04(bool x) { return Id00(x); }
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

        static bool Not00(bool x) { return !x; }
        static bool Not01(bool x) { return x == false; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not02(bool x) { return Not00(Id00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not03(bool x) { return Id00(Not00(x)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Not04(bool x) { return Id00(!x); }
        static bool Not05(bool x) { return !Id00(x); }

        public static int Main(string[] args)
        {
            bool result = true;

            result &= ((Func<bool, bool>)Id00).Invoke(true);
            result &= ! ((Func<bool, bool>)Id00).Invoke(false);
            result &= ((Func<bool, bool>)Id01).Invoke(true);
            result &= ! ((Func<bool, bool>)Id01).Invoke(false);
            result &= ((Func<bool, bool>)Id02).Invoke(true);
            result &= ! ((Func<bool, bool>)Id02).Invoke(false);
            result &= ((Func<bool, bool>)Id03).Invoke(true);
            result &= ! ((Func<bool, bool>)Id03).Invoke(false);
            result &= ((Func<bool, bool>)Id04).Invoke(true);
            result &= ! ((Func<bool, bool>)Id04).Invoke(false);
            result &= ((Func<bool, bool>)Id05).Invoke(true);
            result &= ! ((Func<bool, bool>)Id05).Invoke(false);
            result &= ((Func<bool, bool>)Id06).Invoke(true);
            result &= ! ((Func<bool, bool>)Id06).Invoke(false);
            result &= ((Func<bool, bool>)Id07).Invoke(true);
            result &= ! ((Func<bool, bool>)Id07).Invoke(false);
            result &= ((Func<bool, bool>)Id08).Invoke(true);
            result &= ! ((Func<bool, bool>)Id08).Invoke(false);
            result &= ((Func<bool, bool>)Id09).Invoke(true);
            result &= ! ((Func<bool, bool>)Id09).Invoke(false);

            result &= ((Func<bool, bool>)Id10).Invoke(true);
            result &= ! ((Func<bool, bool>)Id10).Invoke(false);
            result &= ((Func<bool, bool>)Id11).Invoke(true);
            result &= ! ((Func<bool, bool>)Id11).Invoke(false);
            result &= ((Func<bool, bool>)Id12).Invoke(true);
            result &= ! ((Func<bool, bool>)Id12).Invoke(false);
            result &= ((Func<bool, bool>)Id13).Invoke(true);
            result &= ! ((Func<bool, bool>)Id13).Invoke(false);
            result &= ((Func<bool, bool>)Id14).Invoke(true);
            result &= ! ((Func<bool, bool>)Id14).Invoke(false);
            result &= ((Func<bool, bool>)Id15).Invoke(true);
            result &= ! ((Func<bool, bool>)Id15).Invoke(false);
            result &= ((Func<bool, bool>)Id16).Invoke(true);
            result &= ! ((Func<bool, bool>)Id16).Invoke(false);
            result &= ((Func<bool, bool>)Id17).Invoke(true);
            result &= ! ((Func<bool, bool>)Id17).Invoke(false);
            result &= ((Func<bool, bool>)Id18).Invoke(true);
            result &= ! ((Func<bool, bool>)Id18).Invoke(false);
            result &= ((Func<bool, bool>)Id19).Invoke(true);
            result &= ! ((Func<bool, bool>)Id19).Invoke(false);

            result &= ((Func<bool, bool>)Id20).Invoke(true);
            result &= ! ((Func<bool, bool>)Id20).Invoke(false);
            result &= ((Func<bool, bool>)Id21).Invoke(true);
            result &= ! ((Func<bool, bool>)Id21).Invoke(false);
            result &= ((Func<bool, bool>)Id22).Invoke(true);
            result &= ! ((Func<bool, bool>)Id22).Invoke(false);
            result &= ((Func<bool, bool>)Id23).Invoke(true);
            result &= ! ((Func<bool, bool>)Id23).Invoke(false);
            result &= ((Func<bool, bool>)Id24).Invoke(true);
            result &= ! ((Func<bool, bool>)Id24).Invoke(false);
            result &= ((Func<bool, bool>)Id25).Invoke(true);
            result &= ! ((Func<bool, bool>)Id25).Invoke(false);
            result &= ((Func<bool, bool>)Id26).Invoke(true);
            result &= ! ((Func<bool, bool>)Id26).Invoke(false);
            result &= ((Func<bool, bool>)Id27).Invoke(true);
            result &= ! ((Func<bool, bool>)Id27).Invoke(false);
            result &= ((Func<bool, bool>)Id28).Invoke(true);
            result &= ! ((Func<bool, bool>)Id28).Invoke(false);
            result &= ((Func<bool, bool>)Id29).Invoke(true);
            result &= ! ((Func<bool, bool>)Id29).Invoke(false);

            result &= ((Func<bool, bool>)Id30).Invoke(true);
            result &= ! ((Func<bool, bool>)Id30).Invoke(false);
            result &= ((Func<bool, bool>)Id31).Invoke(true);
            result &= ! ((Func<bool, bool>)Id31).Invoke(false);
            result &= ((Func<bool, bool>)Id32).Invoke(true);
            result &= ! ((Func<bool, bool>)Id32).Invoke(false);
            result &= ((Func<bool, bool>)Id33).Invoke(true);
            result &= ! ((Func<bool, bool>)Id33).Invoke(false);
            result &= ((Func<bool, bool>)Id34).Invoke(true);
            result &= ! ((Func<bool, bool>)Id34).Invoke(false);
            result &= ((Func<bool, bool>)Id35).Invoke(true);
            result &= ! ((Func<bool, bool>)Id35).Invoke(false);
            result &= ((Func<bool, bool>)Id36).Invoke(true);
            result &= ! ((Func<bool, bool>)Id36).Invoke(false);
            result &= ((Func<bool, bool>)Id37).Invoke(true);
            result &= ! ((Func<bool, bool>)Id37).Invoke(false);
            result &= ((Func<bool, bool>)Id38).Invoke(true);
            result &= ! ((Func<bool, bool>)Id38).Invoke(false);
            result &= ((Func<bool, bool>)Id39).Invoke(true);
            result &= ! ((Func<bool, bool>)Id39).Invoke(false);

            result &= ((Func<bool, bool>)Id40).Invoke(true);
            result &= ! ((Func<bool, bool>)Id40).Invoke(false);
            result &= ((Func<bool, bool>)Id41).Invoke(true);
            result &= ! ((Func<bool, bool>)Id41).Invoke(false);
            result &= ((Func<bool, bool>)Id42).Invoke(true);
            result &= ! ((Func<bool, bool>)Id42).Invoke(false);
            result &= ((Func<bool, bool>)Id43).Invoke(true);
            result &= ! ((Func<bool, bool>)Id43).Invoke(false);
            result &= ((Func<bool, bool>)Id44).Invoke(true);
            result &= ! ((Func<bool, bool>)Id44).Invoke(false);
            result &= ((Func<bool, bool>)Id45).Invoke(true);
            result &= ! ((Func<bool, bool>)Id45).Invoke(false);
            result &= ((Func<bool, bool>)Id46).Invoke(true);
            result &= ! ((Func<bool, bool>)Id46).Invoke(false);
            result &= ((Func<bool, bool>)Id47).Invoke(true);
            result &= ! ((Func<bool, bool>)Id47).Invoke(false);
            result &= ((Func<bool, bool>)Id48).Invoke(true);
            result &= ! ((Func<bool, bool>)Id48).Invoke(false);
            result &= ((Func<bool, bool>)Id49).Invoke(true);
            result &= ! ((Func<bool, bool>)Id49).Invoke(false);

            result &= ((Func<bool, bool>)Id50).Invoke(true);
            result &= ! ((Func<bool, bool>)Id50).Invoke(false);
            result &= ((Func<bool, bool>)Id51).Invoke(true);
            result &= ! ((Func<bool, bool>)Id51).Invoke(false);
            result &= ((Func<bool, bool>)Id52).Invoke(true);
            result &= ! ((Func<bool, bool>)Id52).Invoke(false);
            result &= ((Func<bool, bool>)Id53).Invoke(true);
            result &= ! ((Func<bool, bool>)Id53).Invoke(false);
            result &= ((Func<bool, bool>)Id54).Invoke(true);
            result &= ! ((Func<bool, bool>)Id54).Invoke(false);
            result &= ((Func<bool, bool>)Id55).Invoke(true);
            result &= ! ((Func<bool, bool>)Id55).Invoke(false);
            result &= ((Func<bool, bool>)Id56).Invoke(true);
            result &= ! ((Func<bool, bool>)Id56).Invoke(false);
            result &= ((Func<bool, bool>)Id57).Invoke(true);
            result &= ! ((Func<bool, bool>)Id57).Invoke(false);
            result &= ((Func<bool, bool>)Id58).Invoke(true);
            result &= ! ((Func<bool, bool>)Id58).Invoke(false);
            result &= ((Func<bool, bool>)Id59).Invoke(true);
            result &= ! ((Func<bool, bool>)Id59).Invoke(false);

            result &= ((Func<bool, bool>)Id60).Invoke(true);
            result &= ! ((Func<bool, bool>)Id60).Invoke(false);
            result &= ((Func<bool, bool>)Id61).Invoke(true);
            result &= ! ((Func<bool, bool>)Id61).Invoke(false);
            result &= ((Func<bool, bool>)Id62).Invoke(true);
            result &= ! ((Func<bool, bool>)Id62).Invoke(false);
            result &= ((Func<bool, bool>)Id63).Invoke(true);
            result &= ! ((Func<bool, bool>)Id63).Invoke(false);
            result &= ((Func<bool, bool>)Id64).Invoke(true);
            result &= ! ((Func<bool, bool>)Id64).Invoke(false);
            result &= ((Func<bool, bool>)Id65).Invoke(true);
            result &= ! ((Func<bool, bool>)Id65).Invoke(false);
            result &= ((Func<bool, bool>)Id66).Invoke(true);
            result &= ! ((Func<bool, bool>)Id66).Invoke(false);
            result &= ((Func<bool, bool>)Id67).Invoke(true);
            result &= ! ((Func<bool, bool>)Id67).Invoke(false);
            result &= ((Func<bool, bool>)Id68).Invoke(true);
            result &= ! ((Func<bool, bool>)Id68).Invoke(false);
            result &= ((Func<bool, bool>)Id69).Invoke(true);
            result &= ! ((Func<bool, bool>)Id69).Invoke(false);

            result &= ((Func<bool, bool>)Id70).Invoke(true);
            result &= ! ((Func<bool, bool>)Id70).Invoke(false);
            result &= ((Func<bool, bool>)Id71).Invoke(true);
            result &= ! ((Func<bool, bool>)Id71).Invoke(false);
            result &= ((Func<bool, bool>)Id72).Invoke(true);
            result &= ! ((Func<bool, bool>)Id72).Invoke(false);
            result &= ((Func<bool, bool>)Id73).Invoke(true);
            result &= ! ((Func<bool, bool>)Id73).Invoke(false);
            result &= ((Func<bool, bool>)Id74).Invoke(true);
            result &= ! ((Func<bool, bool>)Id74).Invoke(false);
            result &= ((Func<bool, bool>)Id75).Invoke(true);
            result &= ! ((Func<bool, bool>)Id75).Invoke(false);
            result &= ((Func<bool, bool>)Id76).Invoke(true);
            result &= ! ((Func<bool, bool>)Id76).Invoke(false);
            result &= ((Func<bool, bool>)Id77).Invoke(true);
            result &= ! ((Func<bool, bool>)Id77).Invoke(false);
            result &= ((Func<bool, bool>)Id78).Invoke(true);
            result &= ! ((Func<bool, bool>)Id78).Invoke(false);
            result &= ((Func<bool, bool>)Id79).Invoke(true);
            result &= ! ((Func<bool, bool>)Id79).Invoke(false);

            result &= ((Func<bool, bool>)Id80).Invoke(true);
            result &= ! ((Func<bool, bool>)Id80).Invoke(false);
            result &= ((Func<bool, bool>)Id81).Invoke(true);
            result &= ! ((Func<bool, bool>)Id81).Invoke(false);
            result &= ((Func<bool, bool>)Id82).Invoke(true);
            result &= ! ((Func<bool, bool>)Id82).Invoke(false);
            result &= ((Func<bool, bool>)Id83).Invoke(true);
            result &= ! ((Func<bool, bool>)Id83).Invoke(false);
            result &= ((Func<bool, bool>)Id84).Invoke(true);
            result &= ! ((Func<bool, bool>)Id84).Invoke(false);
            result &= ((Func<bool, bool>)Id85).Invoke(true);
            result &= ! ((Func<bool, bool>)Id85).Invoke(false);
            result &= ((Func<bool, bool>)Id86).Invoke(true);
            result &= ! ((Func<bool, bool>)Id86).Invoke(false);
            result &= ((Func<bool, bool>)Id87).Invoke(true);
            result &= ! ((Func<bool, bool>)Id87).Invoke(false);
            result &= ((Func<bool, bool>)Id88).Invoke(true);
            result &= ! ((Func<bool, bool>)Id88).Invoke(false);
            result &= ((Func<bool, bool>)Id89).Invoke(true);
            result &= ! ((Func<bool, bool>)Id89).Invoke(false);

            result &= ((Func<bool, bool>)Id90).Invoke(true);
            result &= !((Func<bool, bool>)Id90).Invoke(false);
            result &= ((Func<bool, bool>)Id91).Invoke(true);
            result &= !((Func<bool, bool>)Id91).Invoke(false);
            result &= ((Func<bool, bool>)Id92).Invoke(true);
            result &= !((Func<bool, bool>)Id92).Invoke(false);
            result &= ((Func<bool, bool>)Id93).Invoke(true);
            result &= !((Func<bool, bool>)Id93).Invoke(false);
            result &= ((Func<bool, bool>)Id94).Invoke(true);
            result &= !((Func<bool, bool>)Id94).Invoke(false);
            result &= ((Func<bool, bool>)Id95).Invoke(true);
            result &= !((Func<bool, bool>)Id95).Invoke(false);
            result &= ((Func<bool, bool>)Id96).Invoke(true);
            result &= !((Func<bool, bool>)Id96).Invoke(false);
            result &= ((Func<bool, bool>)Id97).Invoke(true);
            result &= !((Func<bool, bool>)Id97).Invoke(false);
            result &= ((Func<bool, bool>)Id98).Invoke(true);
            result &= !((Func<bool, bool>)Id98).Invoke(false);
            result &= ((Func<bool, bool>)Id99).Invoke(true);
            result &= !((Func<bool, bool>)Id99).Invoke(false);

            return result ? 100 : -1;
        }
    }
}
