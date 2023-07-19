// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

namespace structinreg
{
    struct Test41
    {
        public long l1;
        public long l2;
    }

    struct Test42
    {
        public long l1;
        public int i1;
    }

    struct Test43
    {
        public long l1;
        public double d1;
    }

    struct Test44
    {
        public long l1;
        public float f1;
    }

    struct Offset1
    {
        #pragma warning disable 0169
        long a1; long a2; long a3; long a4; long a5; long a6; long a7; long a8; long a9; long a10;
        long a11; long a12; long a13; long a14; long a15; long a16; long a17; long a18; long a19; long a20;
        long a21; long a22; long a23; long a24; long a25; long a26; long a27; long a28; long a29; long a30;
        long a31; long a32; long a33; long a34; long a35; long a36; long a37; long a38; long a39; long a40;
        long a41; long a42; long a43; long a44; long a45; long a46; long a47; long a48; long a49; long a50;
        long a51; long a52; long a53; long a54; long a55; long a56; long a57; long a58; long a59; long a60;
        long a61; long a62; long a63; long a64; long a65; long a66; long a67; long a68; long a69; long a70;
        long a71; long a72; long a73; long a74; long a75; long a76; long a77; long a78; long a79; long a80;
        long a81; long a82; long a83; long a84; long a85; long a86; long a87; long a88; long a89; long a90;
        long a91; long a92; long a93; long a94; long a95; long a96; long a97; long a98; long a99; long a100;
        long a101; long a102; long a103; long a104; long a105; long a106; long a107; long a108; long a109; long a110;
        long a111; long a112; long a113; long a114; long a115; long a116; long a117; long a118; long a119; long a120;
        long a121; long a122; long a123; long a124; long a125; long a126; long a127; long a128; long a129; long a130;
        long a131; long a132; long a133; long a134; long a135; long a136; long a137; long a138; long a139; long a140;
        long a141; long a142; long a143; long a144; long a145; long a146; long a147; long a148; long a149; long a150;
        long a151; long a152; long a153; long a154; long a155; long a156; long a157; long a158; long a159; long a160;
        long a161; long a162; long a163; long a164; long a165; long a166; long a167; long a168; long a169; long a170;
        long a171; long a172; long a173; long a174; long a175; long a176; long a177; long a178; long a179; long a180;
        long a181; long a182; long a183; long a184; long a185; long a186; long a187; long a188; long a189; long a190;
        long a191; long a192; long a193; long a194; long a195; long a196; long a197; long a198; long a199; long a200;
        long a201; long a202; long a203; long a204; long a205; long a206; long a207; long a208; long a209; long a210;
        long a211; long a212; long a213; long a214; long a215; long a216; long a217; long a218; long a219; long a220;
        long a221; long a222; long a223; long a224; long a225; long a226; long a227; long a228; long a229; long a230;
        long a231; long a232; long a233; long a234; long a235; long a236; long a237; long a238; long a239; long a240;
        long a241; long a242; long a243; long a244; long a245; long a246;
        #pragma warning restore 0169
    }

    struct Offset2
    {
        #pragma warning disable 0169
        long a1; long a2; long a3; long a4; long a5; long a6; long a7; long a8; long a9; long a10;
        long a11; long a12; long a13; long a14; long a15; long a16; long a17; long a18; long a19; long a20;
        long a21; long a22; long a23; long a24; long a25; long a26; long a27; long a28; long a29; long a30;
        long a31; long a32; long a33; long a34; long a35; long a36; long a37; long a38; long a39; long a40;
        long a41; long a42; long a43; long a44; long a45; long a46; long a47; long a48; long a49; long a50;
        long a51; long a52; long a53; long a54; long a55; long a56; long a57; long a58; long a59; long a60;
        long a61; long a62; long a63; long a64; long a65; long a66; long a67; long a68; long a69; long a70;
        long a71; long a72; long a73; long a74; long a75; long a76; long a77; long a78; long a79; long a80;
        long a81; long a82; long a83; long a84; long a85; long a86; long a87; long a88; long a89; long a90;
        long a91; long a92; long a93; long a94; long a95; long a96; long a97; long a98; long a99; long a100;
        long a101; long a102; long a103; long a104; long a105; long a106; long a107; long a108; long a109; long a110;
        long a111; long a112; long a113; long a114; long a115; long a116; long a117; long a118; long a119; long a120;
        long a121; long a122; long a123; long a124; long a125; long a126; long a127; long a128; long a129; long a130;
        long a131; long a132; long a133; long a134; long a135; long a136; long a137; long a138; long a139; long a140;
        long a141; long a142; long a143; long a144; long a145; long a146; long a147; long a148; long a149; long a150;
        long a151; long a152; long a153; long a154; long a155; long a156; long a157; long a158; long a159; long a160;
        long a161; long a162; long a163; long a164; long a165; long a166; long a167; long a168; long a169; long a170;
        long a171; long a172; long a173; long a174; long a175; long a176; long a177; long a178; long a179; long a180;
        long a181; long a182; long a183; long a184; long a185; long a186; long a187; long a188; long a189; long a190;
        long a191; long a192; long a193; long a194; long a195; long a196; long a197; long a198; long a199; long a200;
        long a201; long a202; long a203; long a204; long a205; long a206; long a207; long a208; long a209; long a210;
        long a211; long a212; long a213; long a214; long a215; long a216; long a217; long a218; long a219; long a220;
        long a221; long a222; long a223; long a224; long a225; long a226; long a227; long a228; long a229; long a230;
        long a231; long a232; long a233; long a234; long a235; long a236; long a237; long a238; long a239; long a240;
        long a241; long a242; long a243; long a244; long a245; long a246; long a247; long a248; long a249; long a250;
        #pragma warning restore 0169
    }

    class Program4
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test1(long l0, Test41 a, Test41 b, Test41 c, Test41 d)
        {
            #pragma warning disable 0168
            Offset1 offset;
            #pragma warning restore 0168
            Console.WriteLine("test1: {0}", l0 + a.l1 + a.l2 + b.l1 + b.l2 + c.l1 + c.l2 + d.l1 + d.l2);
            return l0 + a.l1 + a.l2 + b.l1 + b.l2 + c.l1 + c.l2 + d.l1 + d.l2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test2(long l0, Test42 a, Test42 b, Test42 c, Test42 d)
        {
            #pragma warning disable 0168
            Offset1 offset;
            #pragma warning restore 0168
            Console.WriteLine("test2: {0}", l0 + a.l1 + a.i1 + b.l1 + b.i1 + c.l1 + c.i1 + d.l1 + d.i1);
            return l0 + a.l1 + a.i1 + b.l1 + b.i1 + c.l1 + c.i1 + d.l1 + d.i1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static double test3(long l0, Test43 a, Test43 b, Test43 c, Test43 d)
        {
            #pragma warning disable 0168
            Offset1 offset;
            #pragma warning restore 0168
            Console.WriteLine("test3: {0}", l0 + a.l1 + a.d1 + b.l1 + b.d1 + c.l1 + c.d1 + d.l1 + d.d1);
            return l0 + a.l1 + a.d1 + b.l1 + b.d1 + c.l1 + c.d1 + d.l1 + d.d1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static float test4(long l0, Test44 a, Test44 b, Test44 c, Test44 d)
        {
            #pragma warning disable 0168
            Offset1 offset;
            #pragma warning restore 0168
            Console.WriteLine("test4: {0}", l0 + a.l1 + a.f1 + b.l1 + b.f1 + c.l1 + c.f1 + d.l1 + d.f1);
            return l0 + a.l1 + a.f1 + b.l1 + b.f1 + c.l1 + c.f1 + d.l1 + d.f1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test5(long l0, Test41 a, Test41 b, Test41 c, Test41 d)
        {
            #pragma warning disable 0168
            Offset2 offset;
            #pragma warning restore 0168
            Console.WriteLine("test5: {0}", l0 + a.l1 + a.l2 + b.l1 + b.l2 + c.l1 + c.l2 + d.l1 + d.l2);
            return l0 + a.l1 + a.l2 + b.l1 + b.l2 + c.l1 + c.l2 + d.l1 + d.l2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test6(long l0, Test42 a, Test42 b, Test42 c, Test42 d)
        {
            #pragma warning disable 0168
            Offset2 offset;
            #pragma warning restore 0168
            Console.WriteLine("test6: {0}", l0 + a.l1 + a.i1 + b.l1 + b.i1 + c.l1 + c.i1 + d.l1 + d.i1);
            return l0 + a.l1 + a.i1 + b.l1 + b.i1 + c.l1 + c.i1 + d.l1 + d.i1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static double test7(long l0, Test43 a, Test43 b, Test43 c, Test43 d)
        {
            #pragma warning disable 0168
            Offset2 offset;
            #pragma warning restore 0168
            Console.WriteLine("test7: {0}", l0 + a.l1 + a.d1 + b.l1 + b.d1 + c.l1 + c.d1 + d.l1 + d.d1);
            return l0 + a.l1 + a.d1 + b.l1 + b.d1 + c.l1 + c.d1 + d.l1 + d.d1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static float test8(long l0, Test44 a, Test44 b, Test44 c, Test44 d)
        {
            #pragma warning disable 0168
            Offset2 offset;
            #pragma warning restore 0168
            Console.WriteLine("test8: {0}", l0 + a.l1 + a.f1 + b.l1 + b.f1 + c.l1 + c.f1 + d.l1 + d.f1);
            return l0 + a.l1 + a.f1 + b.l1 + b.f1 + c.l1 + c.f1 + d.l1 + d.f1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int Main1()
        {
            Test41 a1 = default(Test41);
            Test41 b1 = default(Test41);
            Test41 c1 = default(Test41);
            Test41 d1 = default(Test41);
            a1.l1 = 1;
            a1.l2 = 2;
            b1.l1 = 3;
            b1.l2 = 4;
            c1.l1 = 5;
            c1.l2 = 6;
            d1.l1 = 7;
            d1.l2 = 8;

            Test42 a2 = default(Test42);
            Test42 b2 = default(Test42);
            Test42 c2 = default(Test42);
            Test42 d2 = default(Test42);
            a2.l1 = 1;
            a2.i1 = 2;
            b2.l1 = 3;
            b2.i1 = 4;
            c2.l1 = 5;
            c2.i1 = 6;
            d2.l1 = 7;
            d2.i1 = 8;

            Test43 a3 = default(Test43);
            Test43 b3 = default(Test43);
            Test43 c3 = default(Test43);
            Test43 d3 = default(Test43);
            a3.l1 = 1;
            a3.d1 = 2;
            b3.l1 = 3;
            b3.d1 = 4;
            c3.l1 = 5;
            c3.d1 = 6;
            d3.l1 = 7;
            d3.d1 = 8;

            Test44 a4 = default(Test44);
            Test44 b4 = default(Test44);
            Test44 c4 = default(Test44);
            Test44 d4 = default(Test44);
            a4.l1 = 1;
            a4.f1 = 2;
            b4.l1 = 3;
            b4.f1 = 4;
            c4.l1 = 5;
            c4.f1 = 6;
            d4.l1 = 7;
            d4.f1 = 8;

            long t1Res = test1(9, a1, b1, c1, d1);
            Console.WriteLine("test1 Result: {0}", t1Res);
            if (t1Res != 45)
            {
                throw new Exception("Failed test1 test!");
            }

            long t2Res = test2(9, a2, b2, c2, d2);
            Console.WriteLine("test2 Result: {0}", t2Res);
            if (t1Res != 45)
            {
                throw new Exception("Failed test2 test!");
            }

            double t3Res = test3(9, a3, b3, c3, d3);
            Console.WriteLine("test3 Result: {0}", t3Res);
            if (t3Res != 45)
            {
                throw new Exception("Failed test3 test!");
            }

            float t4Res = test4(9, a4, b4, c4, d4);
            Console.WriteLine("test4 Result: {0}", t4Res);
            if (t4Res != 45)
            {
                throw new Exception("Failed test4 test!");
            }

            long t5Res = test5(9, a1, b1, c1, d1);
            Console.WriteLine("test5 Result: {0}", t5Res);
            if (t1Res != 45)
            {
                throw new Exception("Failed test1 test!");
            }

            long t6Res = test6(9, a2, b2, c2, d2);
            Console.WriteLine("test6 Result: {0}", t6Res);
            if (t6Res != 45)
            {
                throw new Exception("Failed test6 test!");
            }

            double t7Res = test7(9, a3, b3, c3, d3);
            Console.WriteLine("test7 Result: {0}", t7Res);
            if (t7Res != 45)
            {
                throw new Exception("Failed test7 test!");
            }

            float t8Res = test8(9, a4, b4, c4, d4);
            Console.WriteLine("test8 Result: {0}", t8Res);
            if (t8Res != 45)
            {
                throw new Exception("Failed test8 test!");
            }

            return 100;
        }
    }
}
