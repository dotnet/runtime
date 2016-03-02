// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;

class EncodingGetBytes1
{
    static int Main()
    {
        EncodingGetBytes1 test = new EncodingGetBytes1();

        TestFramework.BeginTestCase("Encoding.GetBytes");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }

    }

    public bool RunTests()
    {
        bool ret = true;

        ret &= Test1();
        ret &= Test2();
        ret &= Test3();
        ret &= Test4();
        ret &= Test5();
        ret &= Test6();
        ret &= Test7();
        ret &= Test8();
        ret &= Test9();
        ret &= Test10();

        ret &= Test11();
        ret &= Test12();
        ret &= Test13();
    
        ret &= Test40();

        ret &= Test41();
        ret &= Test42();
        ret &= Test43();
        ret &= Test44();
        ret &= Test45();
        ret &= Test46();
        ret &= Test47();
        ret &= Test48();
        ret &= Test49();
        ret &= Test50();

        ret &= Test51();
        ret &= Test52();
        ret &= Test53();
        ret &= Test54();
        ret &= Test55();
        ret &= Test56();
        ret &= Test57();
        ret &= Test58();
        ret &= Test59();
        ret &= Test60();

        ret &= Test61();
        ret &= Test62();
        ret &= Test63();
        ret &= Test64();
        ret &= Test65();
        ret &= Test66();
           ret &= Test69();
        ret &= Test70();

        ret &= Test71();
        ret &= Test74();
        ret &= Test75();
        ret &= Test76();
        ret &= Test7();
        ret &= Test79();
        ret &= Test80();

        ret &= Test81();
        ret &= Test82();
        ret &= Test83();
        ret &= Test84();
        ret &= Test85();
   
        ret &= Test96();
        ret &= Test97();
        ret &= Test98();
        ret &= Test99();
        ret &= Test100();

        ret &= Test101();
        ret &= Test102();
        ret &= Test103();
        ret &= Test104();
        ret &= Test105();
        ret &= Test106();
        ret &= Test107();
        ret &= Test108();
        ret &= Test109();
        ret &= Test110();

        ret &= Test111();
        ret &= Test112();
        ret &= Test113();
        ret &= Test114();
    
        ret &= Test133();
        ret &= Test134();
        ret &= Test135();
        ret &= Test136();
        ret &= Test137();
        ret &= Test138();
        ret &= Test139();
        ret &= Test140();

        ret &= Test141();
        ret &= Test142();
        ret &= Test143();
        ret &= Test144();
        ret &= Test145();
        ret &= Test146();
        ret &= Test147();
        ret &= Test148();
        ret &= Test149();
        ret &= Test150();

        ret &= Test151();
        ret &= Test152();
        ret &= Test153();
        ret &= Test154();
        ret &= Test155();
        ret &= Test156();
        ret &= Test157();
        ret &= Test158();
        ret &= Test159();
   
        ret &= Test107();
        ret &= Test179();
        ret &= Test180();

        ret &= Test181();
        ret &= Test182();
        ret &= Test183();
        ret &= Test184();
        ret &= Test185();
        ret &= Test186();
        ret &= Test187();
        ret &= Test188();
        ret &= Test189();
        ret &= Test190();

        ret &= Test191();
        ret &= Test192();
        ret &= Test193();
        ret &= Test194();
        ret &= Test195();

        return ret;
    }

            // Positive Tests
        public bool Test1() { return PositiveTestString(Encoding.UTF8, "TestString", new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, "00A"); }
        public bool Test2() { return PositiveTestString(Encoding.UTF8, "", new byte[] {  }, "00B"); }
        public bool Test3() { return PositiveTestString(Encoding.UTF8, "FooBA\u0400R", new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 }, "00C"); }
        public bool Test4() { return PositiveTestString(Encoding.UTF8, "\u00C0nima\u0300l", new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, "00D"); }
        public bool Test5() { return PositiveTestString(Encoding.UTF8, "Test\uD803\uDD75Test", new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, "00E"); }
        public bool Test6() { return PositiveTestString(Encoding.UTF8, "Test\uD803Test", new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 }, "00F"); }
        public bool Test7() { return PositiveTestString(Encoding.UTF8, "Test\uDD75Test", new byte[] { 84, 101, 115, 116, 239, 191, 189, 84, 101, 115, 116 }, "00G"); }
        public bool Test8() { return PositiveTestString(Encoding.UTF8, "TestTest\uDD75", new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 }, "00H"); }
        public bool Test9() { return PositiveTestString(Encoding.UTF8, "TestTest\uD803", new byte[] { 84, 101, 115, 116, 84, 101, 115, 116, 239, 191, 189 }, "00I"); }
        public bool Test10() { return PositiveTestString(Encoding.UTF8, "\uDD75", new byte[] { 239, 191, 189 }, "00J"); }
        public bool Test11() { return PositiveTestString(Encoding.UTF8, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, "00K"); }
        public bool Test12() { return PositiveTestString(Encoding.UTF8, "\u0130", new byte[] { 196, 176 }, "00L"); }
        public bool Test13() { return PositiveTestString(Encoding.UTF8, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 239, 191, 189, 240, 144, 181, 181, 239, 191, 189, 239, 191, 189, 239, 191, 189 }, "0A2"); }
         
      
        public bool Test40() { return PositiveTestString(Encoding.Unicode, "TestString", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00A3"); }
        public bool Test41() { return PositiveTestString(Encoding.Unicode, "", new byte[] { }, "00B3"); }
        public bool Test42() { return PositiveTestString(Encoding.Unicode, "FooBA\u0400R", new byte[] { 70, 0, 111, 0, 111, 0, 66, 0, 65, 0, 0, 4, 82, 0 }, "00C3"); }
        public bool Test43() { return PositiveTestString(Encoding.Unicode, "\u00C0nima\u0300l", new byte[] { 192, 0, 110, 0, 105, 0, 109, 0, 97, 0, 0, 3, 108, 0 }, "00D3"); }
        public bool Test44() { return PositiveTestString(Encoding.Unicode, "Test\uD803\uDD75Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 3, 216, 117, 221, 84, 0, 101, 0, 115, 0, 116, 0 }, "00E3"); }
        public bool Test45() { return PositiveTestString(Encoding.Unicode, "Test\uD803Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0 }, "00F3"); }
        public bool Test46() { return PositiveTestString(Encoding.Unicode, "Test\uDD75Test", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 253, 255, 84, 0, 101, 0, 115, 0, 116, 0, }, "00G3"); }
        public bool Test47() { return PositiveTestString(Encoding.Unicode, "TestTest\uDD75", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 }, "00H3"); }
        public bool Test48() { return PositiveTestString(Encoding.Unicode, "TestTest\uD803", new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 0, 253, 255 }, "00I3"); }
        public bool Test49() { return PositiveTestString(Encoding.Unicode, "\uDD75", new byte[] { 253, 255 }, "00J3"); }
        public bool Test50() { return PositiveTestString(Encoding.Unicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 3, 216, 117, 221, 3, 216, 117, 221, 3, 216, 117, 221 }, "00K3"); }
        public bool Test51() { return PositiveTestString(Encoding.Unicode, "\u0130", new byte[] { 48, 1 }, "00L3"); }
        public bool Test52() { return PositiveTestString(Encoding.Unicode, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255, 253, 255, 253, 255, 3, 216, 117, 221, 253, 255, 253, 255, 253, 255 }, "0A23"); }

        public bool Test53() { return PositiveTestString(Encoding.BigEndianUnicode, "TestString", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, "00A4"); }
        public bool Test54() { return PositiveTestString(Encoding.BigEndianUnicode, "", new byte[] { }, "00B4"); }
        public bool Test55() { return PositiveTestString(Encoding.BigEndianUnicode, "FooBA\u0400R", new byte[] { 0, 70, 0, 111, 0, 111, 0, 66, 0, 65, 4, 0, 0, 82 }, "00C4"); }
        public bool Test56() { return PositiveTestString(Encoding.BigEndianUnicode, "\u00C0nima\u0300l", new byte[] { 0, 192, 0, 110, 0, 105, 0, 109, 0, 97, 3, 0, 0, 108 }, "00D4"); }
        public bool Test57() { return PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803\uDD75Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 216, 3, 221, 117, 0, 84, 0, 101, 0, 115, 0, 116 }, "00E4"); }
        public bool Test58() { return PositiveTestString(Encoding.BigEndianUnicode, "Test\uD803Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 255, 253, 0, 84, 0, 101, 0, 115, 0, 116 }, "00F4"); }
        public bool Test59() { return PositiveTestString(Encoding.BigEndianUnicode, "Test\uDD75Test", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 255, 253, 0, 84, 0, 101, 0, 115, 0, 116 }, "00G4"); }
        public bool Test60() { return PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uDD75", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 255, 253 }, "00H4"); }
        public bool Test61() { return PositiveTestString(Encoding.BigEndianUnicode, "TestTest\uD803", new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 84, 0, 101, 0, 115, 0, 116, 255, 253 }, "00I4"); }
        public bool Test62() { return PositiveTestString(Encoding.BigEndianUnicode, "\uDD75", new byte[] { 255, 253 }, "00J4"); }
        public bool Test63() { return PositiveTestString(Encoding.BigEndianUnicode, "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 216, 3, 221, 117, 216, 3, 221, 117, 216, 3, 221, 117 }, "00K4"); }
        public bool Test64() { return PositiveTestString(Encoding.BigEndianUnicode, "\u0130", new byte[] { 1, 48 }, "00L4"); }
        public bool Test65() { return PositiveTestString(Encoding.BigEndianUnicode, "\uDD75\uDD75\uD803\uDD75\uDD75\uDD75\uDD75\uD803\uD803\uD803\uDD75\uDD75\uDD75\uDD75", new byte[] { 255, 253, 255, 253, 216, 3, 221, 117, 255, 253, 255, 253, 255, 253, 255, 253, 255, 253, 216, 3, 221, 117, 255, 253, 255, 253, 255, 253 }, "0A24"); }

        public bool Test66() { return PositiveTestChars(Encoding.UTF8, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, "00M"); }

        public bool Test69() { return PositiveTestChars(Encoding.Unicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0 }, "00M3"); }
        public bool Test70() { return PositiveTestChars(Encoding.BigEndianUnicode, new char[] { 'T', 'e', 's', 't', 'S', 't', 'r', 'i', 'n', 'g' }, new byte[] { 0, 84, 0, 101, 0, 115, 0, 116, 0, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103 }, "00M4"); }

        // Negative Tests
        public bool Test71() { return NegativeTestString(new UTF8Encoding(), null, typeof(ArgumentNullException), "00N"); }

        public bool Test74() { return NegativeTestString(new UnicodeEncoding(), null, typeof(ArgumentNullException), "00N3"); }
        public bool Test75() { return NegativeTestString(new UnicodeEncoding(true, false), null, typeof(ArgumentNullException), "00N4"); }

        public bool Test76() { return NegativeTestChars(new UTF8Encoding(), null, typeof(ArgumentNullException), "00O"); }

        public bool Test79() { return NegativeTestChars(new UnicodeEncoding(), null, typeof(ArgumentNullException), "00O3"); }
        public bool Test80() { return NegativeTestChars(new UnicodeEncoding(true, false), null, typeof(ArgumentNullException), "00O4"); }

        public bool Test81() { return NegativeTestChars2(new UTF8Encoding(), null, 0, 0, typeof(ArgumentNullException), "00P"); }
        public bool Test82() { return NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P"); }
        public bool Test83() { return NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q"); }
        public bool Test84() { return NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R"); }
        public bool Test85() { return NegativeTestChars2(new UTF8Encoding(), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S"); }

        public bool Test96() { return NegativeTestChars2(new UnicodeEncoding(), null, 0, 0, typeof(ArgumentNullException), "00P3"); }
        public bool Test97() { return NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P3"); }
        public bool Test98() { return NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q3"); }
        public bool Test99() { return NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R3"); }
        public bool Test100() { return NegativeTestChars2(new UnicodeEncoding(), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S3"); }

        public bool Test101() { return NegativeTestChars2(new UnicodeEncoding(true, false), null, 0, 0, typeof(ArgumentNullException), "00P4"); }
        public bool Test102() { return NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, -1, 1, typeof(ArgumentOutOfRangeException), "00P4"); }
        public bool Test103() { return NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 1, -1, typeof(ArgumentOutOfRangeException), "00Q4"); }
        public bool Test104() { return NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 0, 10, typeof(ArgumentOutOfRangeException), "00R4"); }
        public bool Test105() { return NegativeTestChars2(new UnicodeEncoding(true, false), new char[] { 't' }, 2, 0, typeof(ArgumentOutOfRangeException), "00S4"); }

        static byte[] output = new byte[20];
        public bool Test106() { return NegativeTestChars3(Encoding.UTF8, null, 0, 0, output, 0, typeof(ArgumentNullException), "00T"); }
        public bool Test107() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, 0, null, 0, typeof(ArgumentNullException), "00U"); }
        public bool Test108() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00V"); }
        public bool Test109() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00W"); }
        public bool Test110() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00X"); }
        public bool Test111() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Y"); }
        public bool Test112() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Z"); }
        public bool Test113() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, 1, output, 20, typeof(ArgumentException), "0A0"); }
        public bool Test114() { return NegativeTestChars3(Encoding.UTF8, new char[] { 't' }, 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A1"); }

    
        public bool Test133() { return NegativeTestChars3(Encoding.Unicode, null, 0, 0, output, 0, typeof(ArgumentNullException), "00T3"); }
        public bool Test134() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, 0, null, 0, typeof(ArgumentNullException), "00U3"); }
        public bool Test135() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00V3"); }
        public bool Test136() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00W3"); }
        public bool Test137() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00X3"); }
        public bool Test138() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Y3"); }
        public bool Test139() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Z3"); }
        public bool Test140() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, 1, output, 20, typeof(ArgumentException), "0A03"); }
        public bool Test141() { return NegativeTestChars3(Encoding.Unicode, new char[] { 't' }, 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A13"); }

        public bool Test142() { return NegativeTestChars3(Encoding.BigEndianUnicode, null, 0, 0, output, 0, typeof(ArgumentNullException), "00T4"); }
        public bool Test143() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, null, 0, typeof(ArgumentNullException), "00U4"); }
        public bool Test144() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00V4"); }
        public bool Test145() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00W4"); }
        public bool Test146() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00X4"); }
        public bool Test147() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Y4"); }
        public bool Test148() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Z4"); }
        public bool Test149() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, 1, output, 20, typeof(ArgumentException), "0A04"); }
        public bool Test150() { return NegativeTestChars3(Encoding.BigEndianUnicode, new char[] { 't' }, 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A14"); }

        public bool Test151() { return NegativeTestString1(Encoding.UTF8, null, 0, 0, output, 0, typeof(ArgumentNullException), "00Ta"); }
        public bool Test152() { return NegativeTestString1(Encoding.UTF8, "t", 0, 0, null, 0, typeof(ArgumentNullException), "00Ua"); }
        public bool Test153() { return NegativeTestString1(Encoding.UTF8, "t", -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00Va"); }
        public bool Test154() { return NegativeTestString1(Encoding.UTF8, "t", 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00Wa"); }
        public bool Test155() { return NegativeTestString1(Encoding.UTF8, "t", 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00Xa"); }
        public bool Test156() { return NegativeTestString1(Encoding.UTF8, "t", 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Ya"); }
        public bool Test157() { return NegativeTestString1(Encoding.UTF8, "t", 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Za"); }
        public bool Test158() { return NegativeTestString1(Encoding.UTF8, "t", 0, 1, output, 20, typeof(ArgumentException), "0A0a"); }
        public bool Test159() { return NegativeTestString1(Encoding.UTF8, "t", 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A1a"); }

     
        public bool Test178() { return NegativeTestString1(Encoding.Unicode, null, 0, 0, output, 0, typeof(ArgumentNullException), "00T3a"); }
        public bool Test179() { return NegativeTestString1(Encoding.Unicode, "t", 0, 0, null, 0, typeof(ArgumentNullException), "00U3a"); }
        public bool Test180() { return NegativeTestString1(Encoding.Unicode, "t", -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00V3a"); }
        public bool Test181() { return NegativeTestString1(Encoding.Unicode, "t", 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00W3a"); }
        public bool Test182() { return NegativeTestString1(Encoding.Unicode, "t", 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00X3a"); }
        public bool Test183() { return NegativeTestString1(Encoding.Unicode, "t", 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Y3a"); }
        public bool Test184() { return NegativeTestString1(Encoding.Unicode, "t", 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Z3a"); }
        public bool Test185() { return NegativeTestString1(Encoding.Unicode, "t", 0, 1, output, 20, typeof(ArgumentException), "0A03a"); }
        public bool Test186() { return NegativeTestString1(Encoding.Unicode, "t", 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A13a"); }

        public bool Test187() { return NegativeTestString1(Encoding.BigEndianUnicode, null, 0, 0, output, 0, typeof(ArgumentNullException), "00T4a"); }
        public bool Test188() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, 0, null, 0, typeof(ArgumentNullException), "00U4a"); }
        public bool Test189() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", -1, 0, output, 0, typeof(ArgumentOutOfRangeException), "00V4a"); }
        public bool Test190() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, 0, output, -1, typeof(ArgumentOutOfRangeException), "00W4a"); }
        public bool Test191() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 2, 0, output, 0, typeof(ArgumentOutOfRangeException), "00X4a"); }
        public bool Test192() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, 0, output, 21, typeof(ArgumentOutOfRangeException), "00Y4a"); }
        public bool Test193() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, 10, output, 0, typeof(ArgumentOutOfRangeException), "00Z4a"); }
        public bool Test194() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, 1, output, 20, typeof(ArgumentException), "0A04a"); }
        public bool Test195() { return NegativeTestString1(Encoding.BigEndianUnicode, "t", 0, -1, output, 0, typeof(ArgumentOutOfRangeException), "0A14a"); }

    public bool PositiveTestString(Encoding enc, string str, byte[] expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes for " + str + " with encoding " + enc.WebName);
        try
        {
            byte[] bytes = enc.GetBytes(str);
            if (!Utilities.CompareBytes(bytes, expected))
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected comparison result. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected: " + Utilities.ByteArrayToString(expected));
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool NegativeTestString(Encoding enc, string str, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes with encoding " + enc.WebName);
        try
        {
            byte[] bytes = enc.GetBytes(str);
            result = false;
            TestFramework.LogError("005", "Error in " + id + ", Expected exception not thrown. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("006", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool PositiveTestChars(Encoding enc, char[] chars, byte[] expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes for " + new string(chars) + " with encoding " + enc.WebName);
        try
        {
            byte[] bytes = enc.GetBytes(chars);
            if (!Utilities.CompareBytes(bytes, expected))
            {
                result = false;
                TestFramework.LogError("003", "Error in " + id + ", unexpected comparison result. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected: " + Utilities.ByteArrayToString(expected));
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("004", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool NegativeTestChars(Encoding enc, char[] str, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes with encoding " + enc.WebName);
        try
        {
            byte[] bytes = enc.GetBytes(str);
            result = false;
            TestFramework.LogError("007", "Error in " + id + ", Expected exception not thrown. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("008", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool NegativeTestChars2(Encoding enc, char[] str, int index, int count, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes with encoding " + enc.WebName);
        try
        {
            byte[] bytes = enc.GetBytes(str, index, count);
            result = false;
            TestFramework.LogError("009", "Error in " + id + ", Expected exception not thrown. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("010", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool NegativeTestChars3(Encoding enc, char[] str, int index, int count, byte[] bytes, int bIndex, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes with encoding " + enc.WebName);
        try
        {
            int output = enc.GetBytes(str, index, count, bytes, bIndex);
            result = false;
            TestFramework.LogError("011", "Error in " + id + ", Expected exception not thrown. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("012", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool NegativeTestString1(Encoding enc, string str, int index, int count, byte[] bytes, int bIndex, Type excType, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Getting bytes with encoding " + enc.WebName);
        try
        {
            int output = enc.GetBytes(str, index, count, bytes, bIndex);
            result = false;
            TestFramework.LogError("013", "Error in " + id + ", Expected exception not thrown. Actual bytes " + Utilities.ByteArrayToString(bytes) + ", Expected exception type: " + excType.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != excType)
            {
                result = false;
                TestFramework.LogError("014", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
            }
        }
        return result;
    }
}
