// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// coverage for mscorjit!emitter::emitLclVarAddr::setVarNum

// The JIT32 only supports up to 32767 variables

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

//Disable the warning about having variables that are not used
#pragma warning disable 219
public class Test_Inline_Vars2
{
    public int RunTest0_Inline()
    {
        long a0 = 0;
        a0++;
        Console.WriteLine(a0);
        return 100;
    }
    public int RunTest1_Inline()
    {
        long a1 = 1;
        a1++;
        Console.WriteLine(a1);
        return 100;
    }
    public int RunTest2_Inline()
    {
        long a2 = 2;
        a2++;
        Console.WriteLine(a2);
        return 100;
    }
    public int RunTest3_Inline()
    {
        long a3 = 3;
        a3++;
        Console.WriteLine(a3);
        return 100;
    }
    public int RunTest4_Inline()
    {
        long a4 = 4;
        a4++;
        Console.WriteLine(a4);
        return 100;
    }
    public int RunTest5_Inline()
    {
        long a5 = 5;
        a5++;
        Console.WriteLine(a5);
        return 100;
    }
    public int RunTest6_Inline()
    {
        long a6 = 6;
        a6++;
        Console.WriteLine(a6);
        return 100;
    }
    public int RunTest7_Inline()
    {
        long a7 = 7;
        a7++;
        Console.WriteLine(a7);
        return 100;
    }
    public int RunTest8_Inline()
    {
        long a8 = 8;
        a8++;
        Console.WriteLine(a8);
        return 100;
    }
    public int RunTest9_Inline()
    {
        long a9 = 9;
        a9++;
        Console.WriteLine(a9);
        return 100;
    }
    public int RunTest10_Inline()
    {
        long a10 = 10;
        a10++;
        Console.WriteLine(a10);
        return 100;
    }
    public int RunTest11_Inline()
    {
        long a11 = 11;
        a11++;
        Console.WriteLine(a11);
        return 100;
    }
    public int RunTest12_Inline()
    {
        long a12 = 12;
        a12++;
        Console.WriteLine(a12);
        return 100;
    }
    public int RunTest13_Inline()
    {
        long a13 = 13;
        a13++;
        Console.WriteLine(a13);
        return 100;
    }
    public int RunTest14_Inline()
    {
        long a14 = 14;
        a14++;
        Console.WriteLine(a14);
        return 100;
    }
    public int RunTest15_Inline()
    {
        long a15 = 15;
        a15++;
        Console.WriteLine(a15);
        return 100;
    }
    public int RunTest16_Inline()
    {
        long a16 = 16;
        a16++;
        Console.WriteLine(a16);
        return 100;
    }
    public int RunTest17_Inline()
    {
        long a17 = 17;
        a17++;
        Console.WriteLine(a17);
        return 100;
    }
    public int RunTest18_Inline()
    {
        long a18 = 18;
        a18++;
        Console.WriteLine(a18);
        return 100;
    }
    public int RunTest19_Inline()
    {
        long a19 = 19;
        a19++;
        Console.WriteLine(a19);
        return 100;
    }
    public int RunTest20_Inline()
    {
        long a20 = 20;
        a20++;
        Console.WriteLine(a20);
        return 100;
    }
    public int RunTest21_Inline()
    {
        long a21 = 21;
        a21++;
        Console.WriteLine(a21);
        return 100;
    }
    public int RunTest22_Inline()
    {
        long a22 = 22;
        a22++;
        Console.WriteLine(a22);
        return 100;
    }
    public int RunTest23_Inline()
    {
        long a23 = 23;
        a23++;
        Console.WriteLine(a23);
        return 100;
    }
    public int RunTest24_Inline()
    {
        long a24 = 24;
        a24++;
        Console.WriteLine(a24);
        return 100;
    }
    public int RunTest25_Inline()
    {
        long a25 = 25;
        a25++;
        Console.WriteLine(a25);
        return 100;
    }
    public int RunTest26_Inline()
    {
        long a26 = 26;
        a26++;
        Console.WriteLine(a26);
        return 100;
    }
    public int RunTest27_Inline()
    {
        long a27 = 27;
        a27++;
        Console.WriteLine(a27);
        return 100;
    }
    public int RunTest28_Inline()
    {
        long a28 = 28;
        a28++;
        Console.WriteLine(a28);
        return 100;
    }
    public int RunTest29_Inline()
    {
        long a29 = 29;
        a29++;
        Console.WriteLine(a29);
        return 100;
    }
    public int RunTest30_Inline()
    {
        long a30 = 30;
        a30++;
        Console.WriteLine(a30);
        return 100;
    }
    public int RunTest31_Inline()
    {
        long a31 = 31;
        a31++;
        Console.WriteLine(a31);
        return 100;
    }
    public int RunTest32_Inline()
    {
        long a32 = 32;
        a32++;
        Console.WriteLine(a32);
        return 100;
    }
    public int RunTest33_Inline()
    {
        long a33 = 33;
        a33++;
        Console.WriteLine(a33);
        return 100;
    }
    public int RunTest34_Inline()
    {
        long a34 = 34;
        a34++;
        Console.WriteLine(a34);
        return 100;
    }
    public int RunTest35_Inline()
    {
        long a35 = 35;
        a35++;
        Console.WriteLine(a35);
        return 100;
    }
    public int RunTest36_Inline()
    {
        long a36 = 36;
        a36++;
        Console.WriteLine(a36);
        return 100;
    }
    public int RunTest37_Inline()
    {
        long a37 = 37;
        a37++;
        Console.WriteLine(a37);
        return 100;
    }
    public int RunTest38_Inline()
    {
        long a38 = 38;
        a38++;
        Console.WriteLine(a38);
        return 100;
    }
    public int RunTest39_Inline()
    {
        long a39 = 39;
        a39++;
        Console.WriteLine(a39);
        return 100;
    }
    public int RunTest40_Inline()
    {
        long a40 = 40;
        a40++;
        Console.WriteLine(a40);
        return 100;
    }
    public int RunTest41_Inline()
    {
        long a41 = 41;
        a41++;
        Console.WriteLine(a41);
        return 100;
    }
    public int RunTest42_Inline()
    {
        long a42 = 42;
        a42++;
        Console.WriteLine(a42);
        return 100;
    }
    public int RunTest43_Inline()
    {
        long a43 = 43;
        a43++;
        Console.WriteLine(a43);
        return 100;
    }
    public int RunTest44_Inline()
    {
        long a44 = 44;
        a44++;
        Console.WriteLine(a44);
        return 100;
    }
    public int RunTest45_Inline()
    {
        long a45 = 45;
        a45++;
        Console.WriteLine(a45);
        return 100;
    }
    public int RunTest46_Inline()
    {
        long a46 = 46;
        a46++;
        Console.WriteLine(a46);
        return 100;
    }
    public int RunTest47_Inline()
    {
        long a47 = 47;
        a47++;
        Console.WriteLine(a47);
        return 100;
    }
    public int RunTest48_Inline()
    {
        long a48 = 48;
        a48++;
        Console.WriteLine(a48);
        return 100;
    }
    public int RunTest49_Inline()
    {
        long a49 = 49;
        a49++;
        Console.WriteLine(a49);
        return 100;
    }
    public int RunTest50_Inline()
    {
        long a50 = 50;
        a50++;
        Console.WriteLine(a50);
        return 100;
    }
    public int RunTest51_Inline()
    {
        long a51 = 51;
        a51++;
        Console.WriteLine(a51);
        return 100;
    }
    public int RunTest52_Inline()
    {
        long a52 = 52;
        a52++;
        Console.WriteLine(a52);
        return 100;
    }
    public int RunTest53_Inline()
    {
        long a53 = 53;
        a53++;
        Console.WriteLine(a53);
        return 100;
    }
    public int RunTest54_Inline()
    {
        long a54 = 54;
        a54++;
        Console.WriteLine(a54);
        return 100;
    }
    public int RunTest55_Inline()
    {
        long a55 = 55;
        a55++;
        Console.WriteLine(a55);
        return 100;
    }
    public int RunTest56_Inline()
    {
        long a56 = 56;
        a56++;
        Console.WriteLine(a56);
        return 100;
    }
    public int RunTest57_Inline()
    {
        long a57 = 57;
        a57++;
        Console.WriteLine(a57);
        return 100;
    }
    public int RunTest58_Inline()
    {
        long a58 = 58;
        a58++;
        Console.WriteLine(a58);
        return 100;
    }
    public int RunTest59_Inline()
    {
        long a59 = 59;
        a59++;
        Console.WriteLine(a59);
        return 100;
    }
    public int RunTest60_Inline()
    {
        long a60 = 60;
        a60++;
        Console.WriteLine(a60);
        return 100;
    }
    public int RunTest61_Inline()
    {
        long a61 = 61;
        a61++;
        Console.WriteLine(a61);
        return 100;
    }
    public int RunTest62_Inline()
    {
        long a62 = 62;
        a62++;
        Console.WriteLine(a62);
        return 100;
    }
    public int RunTest63_Inline()
    {
        long a63 = 63;
        a63++;
        Console.WriteLine(a63);
        return 100;
    }
    public int RunTest64_Inline()
    {
        long a64 = 64;
        a64++;
        Console.WriteLine(a64);
        return 100;
    }
    public int RunTest65_Inline()
    {
        long a65 = 65;
        a65++;
        Console.WriteLine(a65);
        return 100;
    }
    public int RunTest66_Inline()
    {
        long a66 = 66;
        a66++;
        Console.WriteLine(a66);
        return 100;
    }
    public int RunTest67_Inline()
    {
        long a67 = 67;
        a67++;
        Console.WriteLine(a67);
        return 100;
    }
    public int RunTest68_Inline()
    {
        long a68 = 68;
        a68++;
        Console.WriteLine(a68);
        return 100;
    }
    public int RunTest69_Inline()
    {
        long a69 = 69;
        a69++;
        Console.WriteLine(a69);
        return 100;
    }
    public int RunTest70_Inline()
    {
        long a70 = 70;
        a70++;
        Console.WriteLine(a70);
        return 100;
    }
    public int RunTest71_Inline()
    {
        long a71 = 71;
        a71++;
        Console.WriteLine(a71);
        return 100;
    }
    public int RunTest72_Inline()
    {
        long a72 = 72;
        a72++;
        Console.WriteLine(a72);
        return 100;
    }
    public int RunTest73_Inline()
    {
        long a73 = 73;
        a73++;
        Console.WriteLine(a73);
        return 100;
    }
    public int RunTest74_Inline()
    {
        long a74 = 74;
        a74++;
        Console.WriteLine(a74);
        return 100;
    }
    public int RunTest75_Inline()
    {
        long a75 = 75;
        a75++;
        Console.WriteLine(a75);
        return 100;
    }
    public int RunTest76_Inline()
    {
        long a76 = 76;
        a76++;
        Console.WriteLine(a76);
        return 100;
    }
    public int RunTest77_Inline()
    {
        long a77 = 77;
        a77++;
        Console.WriteLine(a77);
        return 100;
    }
    public int RunTest78_Inline()
    {
        long a78 = 78;
        a78++;
        Console.WriteLine(a78);
        return 100;
    }
    public int RunTest79_Inline()
    {
        long a79 = 79;
        a79++;
        Console.WriteLine(a79);
        return 100;
    }
    public int RunTest80_Inline()
    {
        long a80 = 80;
        a80++;
        Console.WriteLine(a80);
        return 100;
    }
    public int RunTest81_Inline()
    {
        long a81 = 81;
        a81++;
        Console.WriteLine(a81);
        return 100;
    }
    public int RunTest82_Inline()
    {
        long a82 = 82;
        a82++;
        Console.WriteLine(a82);
        return 100;
    }
    public int RunTest83_Inline()
    {
        long a83 = 83;
        a83++;
        Console.WriteLine(a83);
        return 100;
    }
    public int RunTest84_Inline()
    {
        long a84 = 84;
        a84++;
        Console.WriteLine(a84);
        return 100;
    }
    public int RunTest85_Inline()
    {
        long a85 = 85;
        a85++;
        Console.WriteLine(a85);
        return 100;
    }
    public int RunTest86_Inline()
    {
        long a86 = 86;
        a86++;
        Console.WriteLine(a86);
        return 100;
    }
    public int RunTest87_Inline()
    {
        long a87 = 87;
        a87++;
        Console.WriteLine(a87);
        return 100;
    }
    public int RunTest88_Inline()
    {
        long a88 = 88;
        a88++;
        Console.WriteLine(a88);
        return 100;
    }
    public int RunTest89_Inline()
    {
        long a89 = 89;
        a89++;
        Console.WriteLine(a89);
        return 100;
    }
    public int RunTest90_Inline()
    {
        long a90 = 90;
        a90++;
        Console.WriteLine(a90);
        return 100;
    }
    public int RunTest91_Inline()
    {
        long a91 = 91;
        a91++;
        Console.WriteLine(a91);
        return 100;
    }
    public int RunTest92_Inline()
    {
        long a92 = 92;
        a92++;
        Console.WriteLine(a92);
        return 100;
    }
    public int RunTest93_Inline()
    {
        long a93 = 93;
        a93++;
        Console.WriteLine(a93);
        return 100;
    }
    public int RunTest94_Inline()
    {
        long a94 = 94;
        a94++;
        Console.WriteLine(a94);
        return 100;
    }
    public int RunTest95_Inline()
    {
        long a95 = 95;
        a95++;
        Console.WriteLine(a95);
        return 100;
    }
    public int RunTest96_Inline()
    {
        long a96 = 96;
        a96++;
        Console.WriteLine(a96);
        return 100;
    }
    public int RunTest97_Inline()
    {
        long a97 = 97;
        a97++;
        Console.WriteLine(a97);
        return 100;
    }
    public int RunTest98_Inline()
    {
        long a98 = 98;
        a98++;
        Console.WriteLine(a98);
        return 100;
    }
    public int RunTest99_Inline()
    {
        long a99 = 99;
        a99++;
        Console.WriteLine(a99);
        return 100;
    }
    public int RunTest100_Inline()
    {
        long a100 = 100;
        a100++;
        Console.WriteLine(a100);
        return 100;
    }
    public int RunTest101_Inline()
    {
        long a101 = 101;
        a101++;
        Console.WriteLine(a101);
        return 100;
    }
    public int RunTest102_Inline()
    {
        long a102 = 102;
        a102++;
        Console.WriteLine(a102);
        return 100;
    }
    public int RunTest103_Inline()
    {
        long a103 = 103;
        a103++;
        Console.WriteLine(a103);
        return 100;
    }
    public int RunTest104_Inline()
    {
        long a104 = 104;
        a104++;
        Console.WriteLine(a104);
        return 100;
    }
    public int RunTest105_Inline()
    {
        long a105 = 105;
        a105++;
        Console.WriteLine(a105);
        return 100;
    }
    public int RunTest106_Inline()
    {
        long a106 = 106;
        a106++;
        Console.WriteLine(a106);
        return 100;
    }
    public int RunTest107_Inline()
    {
        long a107 = 107;
        a107++;
        Console.WriteLine(a107);
        return 100;
    }
    public int RunTest108_Inline()
    {
        long a108 = 108;
        a108++;
        Console.WriteLine(a108);
        return 100;
    }
    public int RunTest109_Inline()
    {
        long a109 = 109;
        a109++;
        Console.WriteLine(a109);
        return 100;
    }
    public int RunTest110_Inline()
    {
        long a110 = 110;
        a110++;
        Console.WriteLine(a110);
        return 100;
    }
    public int RunTest111_Inline()
    {
        long a111 = 111;
        a111++;
        Console.WriteLine(a111);
        return 100;
    }
    public int RunTest112_Inline()
    {
        long a112 = 112;
        a112++;
        Console.WriteLine(a112);
        return 100;
    }
    public int RunTest113_Inline()
    {
        long a113 = 113;
        a113++;
        Console.WriteLine(a113);
        return 100;
    }
    public int RunTest114_Inline()
    {
        long a114 = 114;
        a114++;
        Console.WriteLine(a114);
        return 100;
    }
    public int RunTest115_Inline()
    {
        long a115 = 115;
        a115++;
        Console.WriteLine(a115);
        return 100;
    }
    public int RunTest116_Inline()
    {
        long a116 = 116;
        a116++;
        Console.WriteLine(a116);
        return 100;
    }
    public int RunTest117_Inline()
    {
        long a117 = 117;
        a117++;
        Console.WriteLine(a117);
        return 100;
    }
    public int RunTest118_Inline()
    {
        long a118 = 118;
        a118++;
        Console.WriteLine(a118);
        return 100;
    }
    public int RunTest119_Inline()
    {
        long a119 = 119;
        a119++;
        Console.WriteLine(a119);
        return 100;
    }
    public int RunTest120_Inline()
    {
        long a120 = 120;
        a120++;
        Console.WriteLine(a120);
        return 100;
    }
    public int RunTest121_Inline()
    {
        long a121 = 121;
        a121++;
        Console.WriteLine(a121);
        return 100;
    }
    public int RunTest122_Inline()
    {
        long a122 = 122;
        a122++;
        Console.WriteLine(a122);
        return 100;
    }
    public int RunTest123_Inline()
    {
        long a123 = 123;
        a123++;
        Console.WriteLine(a123);
        return 100;
    }
    public int RunTest124_Inline()
    {
        long a124 = 124;
        a124++;
        Console.WriteLine(a124);
        return 100;
    }
    public int RunTest125_Inline()
    {
        long a125 = 125;
        a125++;
        Console.WriteLine(a125);
        return 100;
    }
    public int RunTest126_Inline()
    {
        long a126 = 126;
        a126++;
        Console.WriteLine(a126);
        return 100;
    }
    public int RunTest127_Inline()
    {
        long a127 = 127;
        a127++;
        Console.WriteLine(a127);
        return 100;
    }
    public int RunTest128_Inline()
    {
        long a128 = 128;
        a128++;
        Console.WriteLine(a128);
        return 100;
    }
    public int RunTest129_Inline()
    {
        long a129 = 129;
        a129++;
        Console.WriteLine(a129);
        return 100;
    }
    public int RunTest130_Inline()
    {
        long a130 = 130;
        a130++;
        Console.WriteLine(a130);
        return 100;
    }
    public int RunTest131_Inline()
    {
        long a131 = 131;
        a131++;
        Console.WriteLine(a131);
        return 100;
    }
    public int RunTest132_Inline()
    {
        long a132 = 132;
        a132++;
        Console.WriteLine(a132);
        return 100;
    }
    public int RunTest133_Inline()
    {
        long a133 = 133;
        a133++;
        Console.WriteLine(a133);
        return 100;
    }
    public int RunTest134_Inline()
    {
        long a134 = 134;
        a134++;
        Console.WriteLine(a134);
        return 100;
    }
    public int RunTest135_Inline()
    {
        long a135 = 135;
        a135++;
        Console.WriteLine(a135);
        return 100;
    }
    public int RunTest136_Inline()
    {
        long a136 = 136;
        a136++;
        Console.WriteLine(a136);
        return 100;
    }
    public int RunTest137_Inline()
    {
        long a137 = 137;
        a137++;
        Console.WriteLine(a137);
        return 100;
    }
    public int RunTest138_Inline()
    {
        long a138 = 138;
        a138++;
        Console.WriteLine(a138);
        return 100;
    }
    public int RunTest139_Inline()
    {
        long a139 = 139;
        a139++;
        Console.WriteLine(a139);
        return 100;
    }
    public int RunTest140_Inline()
    {
        long a140 = 140;
        a140++;
        Console.WriteLine(a140);
        return 100;
    }
    public int RunTest141_Inline()
    {
        long a141 = 141;
        a141++;
        Console.WriteLine(a141);
        return 100;
    }
    public int RunTest142_Inline()
    {
        long a142 = 142;
        a142++;
        Console.WriteLine(a142);
        return 100;
    }
    public int RunTest143_Inline()
    {
        long a143 = 143;
        a143++;
        Console.WriteLine(a143);
        return 100;
    }
    public int RunTest144_Inline()
    {
        long a144 = 144;
        a144++;
        Console.WriteLine(a144);
        return 100;
    }
    public int RunTest145_Inline()
    {
        long a145 = 145;
        a145++;
        Console.WriteLine(a145);
        return 100;
    }
    public int RunTest146_Inline()
    {
        long a146 = 146;
        a146++;
        Console.WriteLine(a146);
        return 100;
    }
    public int RunTest147_Inline()
    {
        long a147 = 147;
        a147++;
        Console.WriteLine(a147);
        return 100;
    }
    public int RunTest148_Inline()
    {
        long a148 = 148;
        a148++;
        Console.WriteLine(a148);
        return 100;
    }
    public int RunTest149_Inline()
    {
        long a149 = 149;
        a149++;
        Console.WriteLine(a149);
        return 100;
    }
    public int RunTest150_Inline()
    {
        long a150 = 150;
        a150++;
        Console.WriteLine(a150);
        return 100;
    }
    public int RunTest151_Inline()
    {
        long a151 = 151;
        a151++;
        Console.WriteLine(a151);
        return 100;
    }
    public int RunTest152_Inline()
    {
        long a152 = 152;
        a152++;
        Console.WriteLine(a152);
        return 100;
    }
    public int RunTest153_Inline()
    {
        long a153 = 153;
        a153++;
        Console.WriteLine(a153);
        return 100;
    }
    public int RunTest154_Inline()
    {
        long a154 = 154;
        a154++;
        Console.WriteLine(a154);
        return 100;
    }
    public int RunTest155_Inline()
    {
        long a155 = 155;
        a155++;
        Console.WriteLine(a155);
        return 100;
    }
    public int RunTest156_Inline()
    {
        long a156 = 156;
        a156++;
        Console.WriteLine(a156);
        return 100;
    }
    public int RunTest157_Inline()
    {
        long a157 = 157;
        a157++;
        Console.WriteLine(a157);
        return 100;
    }
    public int RunTest158_Inline()
    {
        long a158 = 158;
        a158++;
        Console.WriteLine(a158);
        return 100;
    }
    public int RunTest159_Inline()
    {
        long a159 = 159;
        a159++;
        Console.WriteLine(a159);
        return 100;
    }
    public int RunTest160_Inline()
    {
        long a160 = 160;
        a160++;
        Console.WriteLine(a160);
        return 100;
    }
    public int RunTest161_Inline()
    {
        long a161 = 161;
        a161++;
        Console.WriteLine(a161);
        return 100;
    }
    public int RunTest162_Inline()
    {
        long a162 = 162;
        a162++;
        Console.WriteLine(a162);
        return 100;
    }
    public int RunTest163_Inline()
    {
        long a163 = 163;
        a163++;
        Console.WriteLine(a163);
        return 100;
    }
    public int RunTest164_Inline()
    {
        long a164 = 164;
        a164++;
        Console.WriteLine(a164);
        return 100;
    }
    public int RunTest165_Inline()
    {
        long a165 = 165;
        a165++;
        Console.WriteLine(a165);
        return 100;
    }
    public int RunTest166_Inline()
    {
        long a166 = 166;
        a166++;
        Console.WriteLine(a166);
        return 100;
    }
    public int RunTest167_Inline()
    {
        long a167 = 167;
        a167++;
        Console.WriteLine(a167);
        return 100;
    }
    public int RunTest168_Inline()
    {
        long a168 = 168;
        a168++;
        Console.WriteLine(a168);
        return 100;
    }
    public int RunTest169_Inline()
    {
        long a169 = 169;
        a169++;
        Console.WriteLine(a169);
        return 100;
    }
    public int RunTest170_Inline()
    {
        long a170 = 170;
        a170++;
        Console.WriteLine(a170);
        return 100;
    }
    public int RunTest171_Inline()
    {
        long a171 = 171;
        a171++;
        Console.WriteLine(a171);
        return 100;
    }
    public int RunTest172_Inline()
    {
        long a172 = 172;
        a172++;
        Console.WriteLine(a172);
        return 100;
    }
    public int RunTest173_Inline()
    {
        long a173 = 173;
        a173++;
        Console.WriteLine(a173);
        return 100;
    }
    public int RunTest174_Inline()
    {
        long a174 = 174;
        a174++;
        Console.WriteLine(a174);
        return 100;
    }
    public int RunTest175_Inline()
    {
        long a175 = 175;
        a175++;
        Console.WriteLine(a175);
        return 100;
    }
    public int RunTest176_Inline()
    {
        long a176 = 176;
        a176++;
        Console.WriteLine(a176);
        return 100;
    }
    public int RunTest177_Inline()
    {
        long a177 = 177;
        a177++;
        Console.WriteLine(a177);
        return 100;
    }
    public int RunTest178_Inline()
    {
        long a178 = 178;
        a178++;
        Console.WriteLine(a178);
        return 100;
    }
    public int RunTest179_Inline()
    {
        long a179 = 179;
        a179++;
        Console.WriteLine(a179);
        return 100;
    }
    public int RunTest180_Inline()
    {
        long a180 = 180;
        a180++;
        Console.WriteLine(a180);
        return 100;
    }
    public int RunTest181_Inline()
    {
        long a181 = 181;
        a181++;
        Console.WriteLine(a181);
        return 100;
    }
    public int RunTest182_Inline()
    {
        long a182 = 182;
        a182++;
        Console.WriteLine(a182);
        return 100;
    }
    public int RunTest183_Inline()
    {
        long a183 = 183;
        a183++;
        Console.WriteLine(a183);
        return 100;
    }
    public int RunTest184_Inline()
    {
        long a184 = 184;
        a184++;
        Console.WriteLine(a184);
        return 100;
    }
    public int RunTest185_Inline()
    {
        long a185 = 185;
        a185++;
        Console.WriteLine(a185);
        return 100;
    }
    public int RunTest186_Inline()
    {
        long a186 = 186;
        a186++;
        Console.WriteLine(a186);
        return 100;
    }
    public int RunTest187_Inline()
    {
        long a187 = 187;
        a187++;
        Console.WriteLine(a187);
        return 100;
    }
    public int RunTest188_Inline()
    {
        long a188 = 188;
        a188++;
        Console.WriteLine(a188);
        return 100;
    }
    public int RunTest189_Inline()
    {
        long a189 = 189;
        a189++;
        Console.WriteLine(a189);
        return 100;
    }
    public int RunTest190_Inline()
    {
        long a190 = 190;
        a190++;
        Console.WriteLine(a190);
        return 100;
    }
    public int RunTest191_Inline()
    {
        long a191 = 191;
        a191++;
        Console.WriteLine(a191);
        return 100;
    }
    public int RunTest192_Inline()
    {
        long a192 = 192;
        a192++;
        Console.WriteLine(a192);
        return 100;
    }
    public int RunTest193_Inline()
    {
        long a193 = 193;
        a193++;
        Console.WriteLine(a193);
        return 100;
    }
    public int RunTest194_Inline()
    {
        long a194 = 194;
        a194++;
        Console.WriteLine(a194);
        return 100;
    }
    public int RunTest195_Inline()
    {
        long a195 = 195;
        a195++;
        Console.WriteLine(a195);
        return 100;
    }
    public int RunTest196_Inline()
    {
        long a196 = 196;
        a196++;
        Console.WriteLine(a196);
        return 100;
    }
    public int RunTest197_Inline()
    {
        long a197 = 197;
        a197++;
        Console.WriteLine(a197);
        return 100;
    }
    public int RunTest198_Inline()
    {
        long a198 = 198;
        a198++;
        Console.WriteLine(a198);
        return 100;
    }
    public int RunTest199_Inline()
    {
        long a199 = 199;
        a199++;
        Console.WriteLine(a199);
        return 100;
    }
    public int RunTest200_Inline()
    {
        long a200 = 200;
        a200++;
        Console.WriteLine(a200);
        return 100;
    }
    public int RunTest201_Inline()
    {
        long a201 = 201;
        a201++;
        Console.WriteLine(a201);
        return 100;
    }
    public int RunTest202_Inline()
    {
        long a202 = 202;
        a202++;
        Console.WriteLine(a202);
        return 100;
    }
    public int RunTest203_Inline()
    {
        long a203 = 203;
        a203++;
        Console.WriteLine(a203);
        return 100;
    }
    public int RunTest204_Inline()
    {
        long a204 = 204;
        a204++;
        Console.WriteLine(a204);
        return 100;
    }
    public int RunTest205_Inline()
    {
        long a205 = 205;
        a205++;
        Console.WriteLine(a205);
        return 100;
    }
    public int RunTest206_Inline()
    {
        long a206 = 206;
        a206++;
        Console.WriteLine(a206);
        return 100;
    }
    public int RunTest207_Inline()
    {
        long a207 = 207;
        a207++;
        Console.WriteLine(a207);
        return 100;
    }
    public int RunTest208_Inline()
    {
        long a208 = 208;
        a208++;
        Console.WriteLine(a208);
        return 100;
    }
    public int RunTest209_Inline()
    {
        long a209 = 209;
        a209++;
        Console.WriteLine(a209);
        return 100;
    }
    public int RunTest210_Inline()
    {
        long a210 = 210;
        a210++;
        Console.WriteLine(a210);
        return 100;
    }
    public int RunTest211_Inline()
    {
        long a211 = 211;
        a211++;
        Console.WriteLine(a211);
        return 100;
    }
    public int RunTest212_Inline()
    {
        long a212 = 212;
        a212++;
        Console.WriteLine(a212);
        return 100;
    }
    public int RunTest213_Inline()
    {
        long a213 = 213;
        a213++;
        Console.WriteLine(a213);
        return 100;
    }
    public int RunTest214_Inline()
    {
        long a214 = 214;
        a214++;
        Console.WriteLine(a214);
        return 100;
    }
    public int RunTest215_Inline()
    {
        long a215 = 215;
        a215++;
        Console.WriteLine(a215);
        return 100;
    }
    public int RunTest216_Inline()
    {
        long a216 = 216;
        a216++;
        Console.WriteLine(a216);
        return 100;
    }
    public int RunTest217_Inline()
    {
        long a217 = 217;
        a217++;
        Console.WriteLine(a217);
        return 100;
    }
    public int RunTest218_Inline()
    {
        long a218 = 218;
        a218++;
        Console.WriteLine(a218);
        return 100;
    }
    public int RunTest219_Inline()
    {
        long a219 = 219;
        a219++;
        Console.WriteLine(a219);
        return 100;
    }
    public int RunTest220_Inline()
    {
        long a220 = 220;
        a220++;
        Console.WriteLine(a220);
        return 100;
    }
    public int RunTest221_Inline()
    {
        long a221 = 221;
        a221++;
        Console.WriteLine(a221);
        return 100;
    }
    public int RunTest222_Inline()
    {
        long a222 = 222;
        a222++;
        Console.WriteLine(a222);
        return 100;
    }
    public int RunTest223_Inline()
    {
        long a223 = 223;
        a223++;
        Console.WriteLine(a223);
        return 100;
    }
    public int RunTest224_Inline()
    {
        long a224 = 224;
        a224++;
        Console.WriteLine(a224);
        return 100;
    }
    public int RunTest225_Inline()
    {
        long a225 = 225;
        a225++;
        Console.WriteLine(a225);
        return 100;
    }
    public int RunTest226_Inline()
    {
        long a226 = 226;
        a226++;
        Console.WriteLine(a226);
        return 100;
    }
    public int RunTest227_Inline()
    {
        long a227 = 227;
        a227++;
        Console.WriteLine(a227);
        return 100;
    }
    public int RunTest228_Inline()
    {
        long a228 = 228;
        a228++;
        Console.WriteLine(a228);
        return 100;
    }
    public int RunTest229_Inline()
    {
        long a229 = 229;
        a229++;
        Console.WriteLine(a229);
        return 100;
    }
    public int RunTest230_Inline()
    {
        long a230 = 230;
        a230++;
        Console.WriteLine(a230);
        return 100;
    }
    public int RunTest231_Inline()
    {
        long a231 = 231;
        a231++;
        Console.WriteLine(a231);
        return 100;
    }
    public int RunTest232_Inline()
    {
        long a232 = 232;
        a232++;
        Console.WriteLine(a232);
        return 100;
    }
    public int RunTest233_Inline()
    {
        long a233 = 233;
        a233++;
        Console.WriteLine(a233);
        return 100;
    }
    public int RunTest234_Inline()
    {
        long a234 = 234;
        a234++;
        Console.WriteLine(a234);
        return 100;
    }
    public int RunTest235_Inline()
    {
        long a235 = 235;
        a235++;
        Console.WriteLine(a235);
        return 100;
    }
    public int RunTest236_Inline()
    {
        long a236 = 236;
        a236++;
        Console.WriteLine(a236);
        return 100;
    }
    public int RunTest237_Inline()
    {
        long a237 = 237;
        a237++;
        Console.WriteLine(a237);
        return 100;
    }
    public int RunTest238_Inline()
    {
        long a238 = 238;
        a238++;
        Console.WriteLine(a238);
        return 100;
    }
    public int RunTest239_Inline()
    {
        long a239 = 239;
        a239++;
        Console.WriteLine(a239);
        return 100;
    }
    public int RunTest240_Inline()
    {
        long a240 = 240;
        a240++;
        Console.WriteLine(a240);
        return 100;
    }
    public int RunTest241_Inline()
    {
        long a241 = 241;
        a241++;
        Console.WriteLine(a241);
        return 100;
    }
    public int RunTest242_Inline()
    {
        long a242 = 242;
        a242++;
        Console.WriteLine(a242);
        return 100;
    }
    public int RunTest243_Inline()
    {
        long a243 = 243;
        a243++;
        Console.WriteLine(a243);
        return 100;
    }
    public int RunTest244_Inline()
    {
        long a244 = 244;
        a244++;
        Console.WriteLine(a244);
        return 100;
    }
    public int RunTest245_Inline()
    {
        long a245 = 245;
        a245++;
        Console.WriteLine(a245);
        return 100;
    }
    public int RunTest246_Inline()
    {
        long a246 = 246;
        a246++;
        Console.WriteLine(a246);
        return 100;
    }
    public int RunTest247_Inline()
    {
        long a247 = 247;
        a247++;
        Console.WriteLine(a247);
        return 100;
    }
    public int RunTest248_Inline()
    {
        long a248 = 248;
        a248++;
        Console.WriteLine(a248);
        return 100;
    }
    public int RunTest249_Inline()
    {
        long a249 = 249;
        a249++;
        Console.WriteLine(a249);
        return 100;
    }
    public int RunTest250_Inline()
    {
        long a250 = 250;
        a250++;
        Console.WriteLine(a250);
        return 100;
    }
    public int RunTest251_Inline()
    {
        long a251 = 251;
        a251++;
        Console.WriteLine(a251);
        return 100;
    }
    public int RunTest252_Inline()
    {
        long a252 = 252;
        a252++;
        Console.WriteLine(a252);
        return 100;
    }
    public int RunTest253_Inline()
    {
        long a253 = 253;
        a253++;
        Console.WriteLine(a253);
        return 100;
    }
    public int RunTest254_Inline()
    {
        long a254 = 254;
        a254++;
        Console.WriteLine(a254);
        return 100;
    }
    public int RunTest255_Inline()
    {
        long a255 = 255;
        a255++;
        Console.WriteLine(a255);
        return 100;
    }
    public int RunTest256_Inline()
    {
        long a256 = 256;
        a256++;
        Console.WriteLine(a256);
        return 100;
    }
    public int RunTest257_Inline()
    {
        long a257 = 257;
        a257++;
        Console.WriteLine(a257);
        return 100;
    }
    public int RunTest258_Inline()
    {
        long a258 = 258;
        a258++;
        Console.WriteLine(a258);
        return 100;
    }
    public int RunTest259_Inline()
    {
        long a259 = 259;
        a259++;
        Console.WriteLine(a259);
        return 100;
    }
    public int RunTest260_Inline()
    {
        long a260 = 260;
        a260++;
        Console.WriteLine(a260);
        return 100;
    }
    public int RunTest261_Inline()
    {
        long a261 = 261;
        a261++;
        Console.WriteLine(a261);
        return 100;
    }
    public int RunTest262_Inline()
    {
        long a262 = 262;
        a262++;
        Console.WriteLine(a262);
        return 100;
    }
    public int RunTest263_Inline()
    {
        long a263 = 263;
        a263++;
        Console.WriteLine(a263);
        return 100;
    }
    public int RunTest264_Inline()
    {
        long a264 = 264;
        a264++;
        Console.WriteLine(a264);
        return 100;
    }
    public int RunTest265_Inline()
    {
        long a265 = 265;
        a265++;
        Console.WriteLine(a265);
        return 100;
    }
    public int RunTest266_Inline()
    {
        long a266 = 266;
        a266++;
        Console.WriteLine(a266);
        return 100;
    }
    public int RunTest267_Inline()
    {
        long a267 = 267;
        a267++;
        Console.WriteLine(a267);
        return 100;
    }
    public int RunTest268_Inline()
    {
        long a268 = 268;
        a268++;
        Console.WriteLine(a268);
        return 100;
    }
    public int RunTest269_Inline()
    {
        long a269 = 269;
        a269++;
        Console.WriteLine(a269);
        return 100;
    }
    public int RunTest270_Inline()
    {
        long a270 = 270;
        a270++;
        Console.WriteLine(a270);
        return 100;
    }
    public int RunTest271_Inline()
    {
        long a271 = 271;
        a271++;
        Console.WriteLine(a271);
        return 100;
    }
    public int RunTest272_Inline()
    {
        long a272 = 272;
        a272++;
        Console.WriteLine(a272);
        return 100;
    }
    public int RunTest273_Inline()
    {
        long a273 = 273;
        a273++;
        Console.WriteLine(a273);
        return 100;
    }
    public int RunTest274_Inline()
    {
        long a274 = 274;
        a274++;
        Console.WriteLine(a274);
        return 100;
    }
    public int RunTest275_Inline()
    {
        long a275 = 275;
        a275++;
        Console.WriteLine(a275);
        return 100;
    }
    public int RunTest276_Inline()
    {
        long a276 = 276;
        a276++;
        Console.WriteLine(a276);
        return 100;
    }
    public int RunTest277_Inline()
    {
        long a277 = 277;
        a277++;
        Console.WriteLine(a277);
        return 100;
    }
    public int RunTest278_Inline()
    {
        long a278 = 278;
        a278++;
        Console.WriteLine(a278);
        return 100;
    }
    public int RunTest279_Inline()
    {
        long a279 = 279;
        a279++;
        Console.WriteLine(a279);
        return 100;
    }
    public int RunTest280_Inline()
    {
        long a280 = 280;
        a280++;
        Console.WriteLine(a280);
        return 100;
    }
    public int RunTest281_Inline()
    {
        long a281 = 281;
        a281++;
        Console.WriteLine(a281);
        return 100;
    }
    public int RunTest282_Inline()
    {
        long a282 = 282;
        a282++;
        Console.WriteLine(a282);
        return 100;
    }
    public int RunTest283_Inline()
    {
        long a283 = 283;
        a283++;
        Console.WriteLine(a283);
        return 100;
    }
    public int RunTest284_Inline()
    {
        long a284 = 284;
        a284++;
        Console.WriteLine(a284);
        return 100;
    }
    public int RunTest285_Inline()
    {
        long a285 = 285;
        a285++;
        Console.WriteLine(a285);
        return 100;
    }
    public int RunTest286_Inline()
    {
        long a286 = 286;
        a286++;
        Console.WriteLine(a286);
        return 100;
    }
    public int RunTest287_Inline()
    {
        long a287 = 287;
        a287++;
        Console.WriteLine(a287);
        return 100;
    }
    public int RunTest288_Inline()
    {
        long a288 = 288;
        a288++;
        Console.WriteLine(a288);
        return 100;
    }
    public int RunTest289_Inline()
    {
        long a289 = 289;
        a289++;
        Console.WriteLine(a289);
        return 100;
    }
    public int RunTest290_Inline()
    {
        long a290 = 290;
        a290++;
        Console.WriteLine(a290);
        return 100;
    }
    public int RunTest291_Inline()
    {
        long a291 = 291;
        a291++;
        Console.WriteLine(a291);
        return 100;
    }
    public int RunTest292_Inline()
    {
        long a292 = 292;
        a292++;
        Console.WriteLine(a292);
        return 100;
    }
    public int RunTest293_Inline()
    {
        long a293 = 293;
        a293++;
        Console.WriteLine(a293);
        return 100;
    }
    public int RunTest294_Inline()
    {
        long a294 = 294;
        a294++;
        Console.WriteLine(a294);
        return 100;
    }
    public int RunTest295_Inline()
    {
        long a295 = 295;
        a295++;
        Console.WriteLine(a295);
        return 100;
    }
    public int RunTest296_Inline()
    {
        long a296 = 296;
        a296++;
        Console.WriteLine(a296);
        return 100;
    }
    public int RunTest297_Inline()
    {
        long a297 = 297;
        a297++;
        Console.WriteLine(a297);
        return 100;
    }
    public int RunTest298_Inline()
    {
        long a298 = 298;
        a298++;
        Console.WriteLine(a298);
        return 100;
    }
    public int RunTest299_Inline()
    {
        long a299 = 299;
        a299++;
        Console.WriteLine(a299);
        return 100;
    }
    public int RunTest300_Inline()
    {
        long a300 = 300;
        a300++;
        Console.WriteLine(a300);
        return 100;
    }
    public int RunTest301_Inline()
    {
        long a301 = 301;
        a301++;
        Console.WriteLine(a301);
        return 100;
    }
    public int RunTest302_Inline()
    {
        long a302 = 302;
        a302++;
        Console.WriteLine(a302);
        return 100;
    }
    public int RunTest303_Inline()
    {
        long a303 = 303;
        a303++;
        Console.WriteLine(a303);
        return 100;
    }
    public int RunTest304_Inline()
    {
        long a304 = 304;
        a304++;
        Console.WriteLine(a304);
        return 100;
    }
    public int RunTest305_Inline()
    {
        long a305 = 305;
        a305++;
        Console.WriteLine(a305);
        return 100;
    }
    public int RunTest306_Inline()
    {
        long a306 = 306;
        a306++;
        Console.WriteLine(a306);
        return 100;
    }
    public int RunTest307_Inline()
    {
        long a307 = 307;
        a307++;
        Console.WriteLine(a307);
        return 100;
    }
    public int RunTest308_Inline()
    {
        long a308 = 308;
        a308++;
        Console.WriteLine(a308);
        return 100;
    }
    public int RunTest309_Inline()
    {
        long a309 = 309;
        a309++;
        Console.WriteLine(a309);
        return 100;
    }
    public int RunTest310_Inline()
    {
        long a310 = 310;
        a310++;
        Console.WriteLine(a310);
        return 100;
    }
    public int RunTest311_Inline()
    {
        long a311 = 311;
        a311++;
        Console.WriteLine(a311);
        return 100;
    }
    public int RunTest312_Inline()
    {
        long a312 = 312;
        a312++;
        Console.WriteLine(a312);
        return 100;
    }
    public int RunTest313_Inline()
    {
        long a313 = 313;
        a313++;
        Console.WriteLine(a313);
        return 100;
    }
    public int RunTest314_Inline()
    {
        long a314 = 314;
        a314++;
        Console.WriteLine(a314);
        return 100;
    }
    public int RunTest315_Inline()
    {
        long a315 = 315;
        a315++;
        Console.WriteLine(a315);
        return 100;
    }
    public int RunTest316_Inline()
    {
        long a316 = 316;
        a316++;
        Console.WriteLine(a316);
        return 100;
    }
    public int RunTest317_Inline()
    {
        long a317 = 317;
        a317++;
        Console.WriteLine(a317);
        return 100;
    }
    public int RunTest318_Inline()
    {
        long a318 = 318;
        a318++;
        Console.WriteLine(a318);
        return 100;
    }
    public int RunTest319_Inline()
    {
        long a319 = 319;
        a319++;
        Console.WriteLine(a319);
        return 100;
    }
    public int RunTest320_Inline()
    {
        long a320 = 320;
        a320++;
        Console.WriteLine(a320);
        return 100;
    }
    public int RunTest321_Inline()
    {
        long a321 = 321;
        a321++;
        Console.WriteLine(a321);
        return 100;
    }
    public int RunTest322_Inline()
    {
        long a322 = 322;
        a322++;
        Console.WriteLine(a322);
        return 100;
    }
    public int RunTest323_Inline()
    {
        long a323 = 323;
        a323++;
        Console.WriteLine(a323);
        return 100;
    }
    public int RunTest324_Inline()
    {
        long a324 = 324;
        a324++;
        Console.WriteLine(a324);
        return 100;
    }
    public int RunTest325_Inline()
    {
        long a325 = 325;
        a325++;
        Console.WriteLine(a325);
        return 100;
    }
    public int RunTest326_Inline()
    {
        long a326 = 326;
        a326++;
        Console.WriteLine(a326);
        return 100;
    }
    public int RunTest327_Inline()
    {
        long a327 = 327;
        a327++;
        Console.WriteLine(a327);
        return 100;
    }
    public int RunTest328_Inline()
    {
        long a328 = 328;
        a328++;
        Console.WriteLine(a328);
        return 100;
    }
    public int RunTest329_Inline()
    {
        long a329 = 329;
        a329++;
        Console.WriteLine(a329);
        return 100;
    }
    public int RunTest330_Inline()
    {
        long a330 = 330;
        a330++;
        Console.WriteLine(a330);
        return 100;
    }
    public int RunTest331_Inline()
    {
        long a331 = 331;
        a331++;
        Console.WriteLine(a331);
        return 100;
    }
    public int RunTest332_Inline()
    {
        long a332 = 332;
        a332++;
        Console.WriteLine(a332);
        return 100;
    }
    public int RunTest333_Inline()
    {
        long a333 = 333;
        a333++;
        Console.WriteLine(a333);
        return 100;
    }
    public int RunTest334_Inline()
    {
        long a334 = 334;
        a334++;
        Console.WriteLine(a334);
        return 100;
    }
    public int RunTest335_Inline()
    {
        long a335 = 335;
        a335++;
        Console.WriteLine(a335);
        return 100;
    }
    public int RunTest336_Inline()
    {
        long a336 = 336;
        a336++;
        Console.WriteLine(a336);
        return 100;
    }
    public int RunTest337_Inline()
    {
        long a337 = 337;
        a337++;
        Console.WriteLine(a337);
        return 100;
    }
    public int RunTest338_Inline()
    {
        long a338 = 338;
        a338++;
        Console.WriteLine(a338);
        return 100;
    }
    public int RunTest339_Inline()
    {
        long a339 = 339;
        a339++;
        Console.WriteLine(a339);
        return 100;
    }
    public int RunTest340_Inline()
    {
        long a340 = 340;
        a340++;
        Console.WriteLine(a340);
        return 100;
    }
    public int RunTest341_Inline()
    {
        long a341 = 341;
        a341++;
        Console.WriteLine(a341);
        return 100;
    }
    public int RunTest342_Inline()
    {
        long a342 = 342;
        a342++;
        Console.WriteLine(a342);
        return 100;
    }
    public int RunTest343_Inline()
    {
        long a343 = 343;
        a343++;
        Console.WriteLine(a343);
        return 100;
    }
    public int RunTest344_Inline()
    {
        long a344 = 344;
        a344++;
        Console.WriteLine(a344);
        return 100;
    }
    public int RunTest345_Inline()
    {
        long a345 = 345;
        a345++;
        Console.WriteLine(a345);
        return 100;
    }
    public int RunTest346_Inline()
    {
        long a346 = 346;
        a346++;
        Console.WriteLine(a346);
        return 100;
    }
    public int RunTest347_Inline()
    {
        long a347 = 347;
        a347++;
        Console.WriteLine(a347);
        return 100;
    }
    public int RunTest348_Inline()
    {
        long a348 = 348;
        a348++;
        Console.WriteLine(a348);
        return 100;
    }
    public int RunTest349_Inline()
    {
        long a349 = 349;
        a349++;
        Console.WriteLine(a349);
        return 100;
    }
    public int RunTest350_Inline()
    {
        long a350 = 350;
        a350++;
        Console.WriteLine(a350);
        return 100;
    }
    public int RunTest351_Inline()
    {
        long a351 = 351;
        a351++;
        Console.WriteLine(a351);
        return 100;
    }
    public int RunTest352_Inline()
    {
        long a352 = 352;
        a352++;
        Console.WriteLine(a352);
        return 100;
    }
    public int RunTest353_Inline()
    {
        long a353 = 353;
        a353++;
        Console.WriteLine(a353);
        return 100;
    }
    public int RunTest354_Inline()
    {
        long a354 = 354;
        a354++;
        Console.WriteLine(a354);
        return 100;
    }
    public int RunTest355_Inline()
    {
        long a355 = 355;
        a355++;
        Console.WriteLine(a355);
        return 100;
    }
    public int RunTest356_Inline()
    {
        long a356 = 356;
        a356++;
        Console.WriteLine(a356);
        return 100;
    }
    public int RunTest357_Inline()
    {
        long a357 = 357;
        a357++;
        Console.WriteLine(a357);
        return 100;
    }
    public int RunTest358_Inline()
    {
        long a358 = 358;
        a358++;
        Console.WriteLine(a358);
        return 100;
    }
    public int RunTest359_Inline()
    {
        long a359 = 359;
        a359++;
        Console.WriteLine(a359);
        return 100;
    }
    public int RunTest360_Inline()
    {
        long a360 = 360;
        a360++;
        Console.WriteLine(a360);
        return 100;
    }
    public int RunTest361_Inline()
    {
        long a361 = 361;
        a361++;
        Console.WriteLine(a361);
        return 100;
    }
    public int RunTest362_Inline()
    {
        long a362 = 362;
        a362++;
        Console.WriteLine(a362);
        return 100;
    }
    public int RunTest363_Inline()
    {
        long a363 = 363;
        a363++;
        Console.WriteLine(a363);
        return 100;
    }
    public int RunTest364_Inline()
    {
        long a364 = 364;
        a364++;
        Console.WriteLine(a364);
        return 100;
    }
    public int RunTest365_Inline()
    {
        long a365 = 365;
        a365++;
        Console.WriteLine(a365);
        return 100;
    }
    public int RunTest366_Inline()
    {
        long a366 = 366;
        a366++;
        Console.WriteLine(a366);
        return 100;
    }
    public int RunTest367_Inline()
    {
        long a367 = 367;
        a367++;
        Console.WriteLine(a367);
        return 100;
    }
    public int RunTest368_Inline()
    {
        long a368 = 368;
        a368++;
        Console.WriteLine(a368);
        return 100;
    }
    public int RunTest369_Inline()
    {
        long a369 = 369;
        a369++;
        Console.WriteLine(a369);
        return 100;
    }
    public int RunTest370_Inline()
    {
        long a370 = 370;
        a370++;
        Console.WriteLine(a370);
        return 100;
    }
    public int RunTest371_Inline()
    {
        long a371 = 371;
        a371++;
        Console.WriteLine(a371);
        return 100;
    }
    public int RunTest372_Inline()
    {
        long a372 = 372;
        a372++;
        Console.WriteLine(a372);
        return 100;
    }
    public int RunTest373_Inline()
    {
        long a373 = 373;
        a373++;
        Console.WriteLine(a373);
        return 100;
    }
    public int RunTest374_Inline()
    {
        long a374 = 374;
        a374++;
        Console.WriteLine(a374);
        return 100;
    }
    public int RunTest375_Inline()
    {
        long a375 = 375;
        a375++;
        Console.WriteLine(a375);
        return 100;
    }
    public int RunTest376_Inline()
    {
        long a376 = 376;
        a376++;
        Console.WriteLine(a376);
        return 100;
    }
    public int RunTest377_Inline()
    {
        long a377 = 377;
        a377++;
        Console.WriteLine(a377);
        return 100;
    }
    public int RunTest378_Inline()
    {
        long a378 = 378;
        a378++;
        Console.WriteLine(a378);
        return 100;
    }
    public int RunTest379_Inline()
    {
        long a379 = 379;
        a379++;
        Console.WriteLine(a379);
        return 100;
    }
    public int RunTest380_Inline()
    {
        long a380 = 380;
        a380++;
        Console.WriteLine(a380);
        return 100;
    }
    public int RunTest381_Inline()
    {
        long a381 = 381;
        a381++;
        Console.WriteLine(a381);
        return 100;
    }
    public int RunTest382_Inline()
    {
        long a382 = 382;
        a382++;
        Console.WriteLine(a382);
        return 100;
    }
    public int RunTest383_Inline()
    {
        long a383 = 383;
        a383++;
        Console.WriteLine(a383);
        return 100;
    }
    public int RunTest384_Inline()
    {
        long a384 = 384;
        a384++;
        Console.WriteLine(a384);
        return 100;
    }
    public int RunTest385_Inline()
    {
        long a385 = 385;
        a385++;
        Console.WriteLine(a385);
        return 100;
    }
    public int RunTest386_Inline()
    {
        long a386 = 386;
        a386++;
        Console.WriteLine(a386);
        return 100;
    }
    public int RunTest387_Inline()
    {
        long a387 = 387;
        a387++;
        Console.WriteLine(a387);
        return 100;
    }
    public int RunTest388_Inline()
    {
        long a388 = 388;
        a388++;
        Console.WriteLine(a388);
        return 100;
    }
    public int RunTest389_Inline()
    {
        long a389 = 389;
        a389++;
        Console.WriteLine(a389);
        return 100;
    }
    public int RunTest390_Inline()
    {
        long a390 = 390;
        a390++;
        Console.WriteLine(a390);
        return 100;
    }
    public int RunTest391_Inline()
    {
        long a391 = 391;
        a391++;
        Console.WriteLine(a391);
        return 100;
    }
    public int RunTest392_Inline()
    {
        long a392 = 392;
        a392++;
        Console.WriteLine(a392);
        return 100;
    }
    public int RunTest393_Inline()
    {
        long a393 = 393;
        a393++;
        Console.WriteLine(a393);
        return 100;
    }
    public int RunTest394_Inline()
    {
        long a394 = 394;
        a394++;
        Console.WriteLine(a394);
        return 100;
    }
    public int RunTest395_Inline()
    {
        long a395 = 395;
        a395++;
        Console.WriteLine(a395);
        return 100;
    }
    public int RunTest396_Inline()
    {
        long a396 = 396;
        a396++;
        Console.WriteLine(a396);
        return 100;
    }
    public int RunTest397_Inline()
    {
        long a397 = 397;
        a397++;
        Console.WriteLine(a397);
        return 100;
    }
    public int RunTest398_Inline()
    {
        long a398 = 398;
        a398++;
        Console.WriteLine(a398);
        return 100;
    }
    public int RunTest399_Inline()
    {
        long a399 = 399;
        a399++;
        Console.WriteLine(a399);
        return 100;
    }
    public int RunTest400_Inline()
    {
        long a400 = 400;
        a400++;
        Console.WriteLine(a400);
        return 100;
    }
    [Fact]
    public static int TestEntryPoint()
    {

        (new Test_Inline_Vars2()).RunTest0_Inline();

        (new Test_Inline_Vars2()).RunTest1_Inline();

        (new Test_Inline_Vars2()).RunTest2_Inline();

        (new Test_Inline_Vars2()).RunTest3_Inline();

        (new Test_Inline_Vars2()).RunTest4_Inline();

        (new Test_Inline_Vars2()).RunTest5_Inline();

        (new Test_Inline_Vars2()).RunTest6_Inline();

        (new Test_Inline_Vars2()).RunTest7_Inline();

        (new Test_Inline_Vars2()).RunTest8_Inline();

        (new Test_Inline_Vars2()).RunTest9_Inline();

        (new Test_Inline_Vars2()).RunTest10_Inline();

        (new Test_Inline_Vars2()).RunTest11_Inline();

        (new Test_Inline_Vars2()).RunTest12_Inline();

        (new Test_Inline_Vars2()).RunTest13_Inline();

        (new Test_Inline_Vars2()).RunTest14_Inline();

        (new Test_Inline_Vars2()).RunTest15_Inline();

        (new Test_Inline_Vars2()).RunTest16_Inline();

        (new Test_Inline_Vars2()).RunTest17_Inline();

        (new Test_Inline_Vars2()).RunTest18_Inline();

        (new Test_Inline_Vars2()).RunTest19_Inline();

        (new Test_Inline_Vars2()).RunTest20_Inline();

        (new Test_Inline_Vars2()).RunTest21_Inline();

        (new Test_Inline_Vars2()).RunTest22_Inline();

        (new Test_Inline_Vars2()).RunTest23_Inline();

        (new Test_Inline_Vars2()).RunTest24_Inline();

        (new Test_Inline_Vars2()).RunTest25_Inline();

        (new Test_Inline_Vars2()).RunTest26_Inline();

        (new Test_Inline_Vars2()).RunTest27_Inline();

        (new Test_Inline_Vars2()).RunTest28_Inline();

        (new Test_Inline_Vars2()).RunTest29_Inline();

        (new Test_Inline_Vars2()).RunTest30_Inline();

        (new Test_Inline_Vars2()).RunTest31_Inline();

        (new Test_Inline_Vars2()).RunTest32_Inline();

        (new Test_Inline_Vars2()).RunTest33_Inline();

        (new Test_Inline_Vars2()).RunTest34_Inline();

        (new Test_Inline_Vars2()).RunTest35_Inline();

        (new Test_Inline_Vars2()).RunTest36_Inline();

        (new Test_Inline_Vars2()).RunTest37_Inline();

        (new Test_Inline_Vars2()).RunTest38_Inline();

        (new Test_Inline_Vars2()).RunTest39_Inline();

        (new Test_Inline_Vars2()).RunTest40_Inline();

        (new Test_Inline_Vars2()).RunTest41_Inline();

        (new Test_Inline_Vars2()).RunTest42_Inline();

        (new Test_Inline_Vars2()).RunTest43_Inline();

        (new Test_Inline_Vars2()).RunTest44_Inline();

        (new Test_Inline_Vars2()).RunTest45_Inline();

        (new Test_Inline_Vars2()).RunTest46_Inline();

        (new Test_Inline_Vars2()).RunTest47_Inline();

        (new Test_Inline_Vars2()).RunTest48_Inline();

        (new Test_Inline_Vars2()).RunTest49_Inline();

        (new Test_Inline_Vars2()).RunTest50_Inline();

        (new Test_Inline_Vars2()).RunTest51_Inline();

        (new Test_Inline_Vars2()).RunTest52_Inline();

        (new Test_Inline_Vars2()).RunTest53_Inline();

        (new Test_Inline_Vars2()).RunTest54_Inline();

        (new Test_Inline_Vars2()).RunTest55_Inline();

        (new Test_Inline_Vars2()).RunTest56_Inline();

        (new Test_Inline_Vars2()).RunTest57_Inline();

        (new Test_Inline_Vars2()).RunTest58_Inline();

        (new Test_Inline_Vars2()).RunTest59_Inline();

        (new Test_Inline_Vars2()).RunTest60_Inline();

        (new Test_Inline_Vars2()).RunTest61_Inline();

        (new Test_Inline_Vars2()).RunTest62_Inline();

        (new Test_Inline_Vars2()).RunTest63_Inline();

        (new Test_Inline_Vars2()).RunTest64_Inline();

        (new Test_Inline_Vars2()).RunTest65_Inline();

        (new Test_Inline_Vars2()).RunTest66_Inline();

        (new Test_Inline_Vars2()).RunTest67_Inline();

        (new Test_Inline_Vars2()).RunTest68_Inline();

        (new Test_Inline_Vars2()).RunTest69_Inline();

        (new Test_Inline_Vars2()).RunTest70_Inline();

        (new Test_Inline_Vars2()).RunTest71_Inline();

        (new Test_Inline_Vars2()).RunTest72_Inline();

        (new Test_Inline_Vars2()).RunTest73_Inline();

        (new Test_Inline_Vars2()).RunTest74_Inline();

        (new Test_Inline_Vars2()).RunTest75_Inline();

        (new Test_Inline_Vars2()).RunTest76_Inline();

        (new Test_Inline_Vars2()).RunTest77_Inline();

        (new Test_Inline_Vars2()).RunTest78_Inline();

        (new Test_Inline_Vars2()).RunTest79_Inline();

        (new Test_Inline_Vars2()).RunTest80_Inline();

        (new Test_Inline_Vars2()).RunTest81_Inline();

        (new Test_Inline_Vars2()).RunTest82_Inline();

        (new Test_Inline_Vars2()).RunTest83_Inline();

        (new Test_Inline_Vars2()).RunTest84_Inline();

        (new Test_Inline_Vars2()).RunTest85_Inline();

        (new Test_Inline_Vars2()).RunTest86_Inline();

        (new Test_Inline_Vars2()).RunTest87_Inline();

        (new Test_Inline_Vars2()).RunTest88_Inline();

        (new Test_Inline_Vars2()).RunTest89_Inline();

        (new Test_Inline_Vars2()).RunTest90_Inline();

        (new Test_Inline_Vars2()).RunTest91_Inline();

        (new Test_Inline_Vars2()).RunTest92_Inline();

        (new Test_Inline_Vars2()).RunTest93_Inline();

        (new Test_Inline_Vars2()).RunTest94_Inline();

        (new Test_Inline_Vars2()).RunTest95_Inline();

        (new Test_Inline_Vars2()).RunTest96_Inline();

        (new Test_Inline_Vars2()).RunTest97_Inline();

        (new Test_Inline_Vars2()).RunTest98_Inline();

        (new Test_Inline_Vars2()).RunTest99_Inline();

        (new Test_Inline_Vars2()).RunTest100_Inline();

        (new Test_Inline_Vars2()).RunTest101_Inline();

        (new Test_Inline_Vars2()).RunTest102_Inline();

        (new Test_Inline_Vars2()).RunTest103_Inline();

        (new Test_Inline_Vars2()).RunTest104_Inline();

        (new Test_Inline_Vars2()).RunTest105_Inline();

        (new Test_Inline_Vars2()).RunTest106_Inline();

        (new Test_Inline_Vars2()).RunTest107_Inline();

        (new Test_Inline_Vars2()).RunTest108_Inline();

        (new Test_Inline_Vars2()).RunTest109_Inline();

        (new Test_Inline_Vars2()).RunTest110_Inline();

        (new Test_Inline_Vars2()).RunTest111_Inline();

        (new Test_Inline_Vars2()).RunTest112_Inline();

        (new Test_Inline_Vars2()).RunTest113_Inline();

        (new Test_Inline_Vars2()).RunTest114_Inline();

        (new Test_Inline_Vars2()).RunTest115_Inline();

        (new Test_Inline_Vars2()).RunTest116_Inline();

        (new Test_Inline_Vars2()).RunTest117_Inline();

        (new Test_Inline_Vars2()).RunTest118_Inline();

        (new Test_Inline_Vars2()).RunTest119_Inline();

        (new Test_Inline_Vars2()).RunTest120_Inline();

        (new Test_Inline_Vars2()).RunTest121_Inline();

        (new Test_Inline_Vars2()).RunTest122_Inline();

        (new Test_Inline_Vars2()).RunTest123_Inline();

        (new Test_Inline_Vars2()).RunTest124_Inline();

        (new Test_Inline_Vars2()).RunTest125_Inline();

        (new Test_Inline_Vars2()).RunTest126_Inline();

        (new Test_Inline_Vars2()).RunTest127_Inline();

        (new Test_Inline_Vars2()).RunTest128_Inline();

        (new Test_Inline_Vars2()).RunTest129_Inline();

        (new Test_Inline_Vars2()).RunTest130_Inline();

        (new Test_Inline_Vars2()).RunTest131_Inline();

        (new Test_Inline_Vars2()).RunTest132_Inline();

        (new Test_Inline_Vars2()).RunTest133_Inline();

        (new Test_Inline_Vars2()).RunTest134_Inline();

        (new Test_Inline_Vars2()).RunTest135_Inline();

        (new Test_Inline_Vars2()).RunTest136_Inline();

        (new Test_Inline_Vars2()).RunTest137_Inline();

        (new Test_Inline_Vars2()).RunTest138_Inline();

        (new Test_Inline_Vars2()).RunTest139_Inline();

        (new Test_Inline_Vars2()).RunTest140_Inline();

        (new Test_Inline_Vars2()).RunTest141_Inline();

        (new Test_Inline_Vars2()).RunTest142_Inline();

        (new Test_Inline_Vars2()).RunTest143_Inline();

        (new Test_Inline_Vars2()).RunTest144_Inline();

        (new Test_Inline_Vars2()).RunTest145_Inline();

        (new Test_Inline_Vars2()).RunTest146_Inline();

        (new Test_Inline_Vars2()).RunTest147_Inline();

        (new Test_Inline_Vars2()).RunTest148_Inline();

        (new Test_Inline_Vars2()).RunTest149_Inline();

        (new Test_Inline_Vars2()).RunTest150_Inline();

        (new Test_Inline_Vars2()).RunTest151_Inline();

        (new Test_Inline_Vars2()).RunTest152_Inline();

        (new Test_Inline_Vars2()).RunTest153_Inline();

        (new Test_Inline_Vars2()).RunTest154_Inline();

        (new Test_Inline_Vars2()).RunTest155_Inline();

        (new Test_Inline_Vars2()).RunTest156_Inline();

        (new Test_Inline_Vars2()).RunTest157_Inline();

        (new Test_Inline_Vars2()).RunTest158_Inline();

        (new Test_Inline_Vars2()).RunTest159_Inline();

        (new Test_Inline_Vars2()).RunTest160_Inline();

        (new Test_Inline_Vars2()).RunTest161_Inline();

        (new Test_Inline_Vars2()).RunTest162_Inline();

        (new Test_Inline_Vars2()).RunTest163_Inline();

        (new Test_Inline_Vars2()).RunTest164_Inline();

        (new Test_Inline_Vars2()).RunTest165_Inline();

        (new Test_Inline_Vars2()).RunTest166_Inline();

        (new Test_Inline_Vars2()).RunTest167_Inline();

        (new Test_Inline_Vars2()).RunTest168_Inline();

        (new Test_Inline_Vars2()).RunTest169_Inline();

        (new Test_Inline_Vars2()).RunTest170_Inline();

        (new Test_Inline_Vars2()).RunTest171_Inline();

        (new Test_Inline_Vars2()).RunTest172_Inline();

        (new Test_Inline_Vars2()).RunTest173_Inline();

        (new Test_Inline_Vars2()).RunTest174_Inline();

        (new Test_Inline_Vars2()).RunTest175_Inline();

        (new Test_Inline_Vars2()).RunTest176_Inline();

        (new Test_Inline_Vars2()).RunTest177_Inline();

        (new Test_Inline_Vars2()).RunTest178_Inline();

        (new Test_Inline_Vars2()).RunTest179_Inline();

        (new Test_Inline_Vars2()).RunTest180_Inline();

        (new Test_Inline_Vars2()).RunTest181_Inline();

        (new Test_Inline_Vars2()).RunTest182_Inline();

        (new Test_Inline_Vars2()).RunTest183_Inline();

        (new Test_Inline_Vars2()).RunTest184_Inline();

        (new Test_Inline_Vars2()).RunTest185_Inline();

        (new Test_Inline_Vars2()).RunTest186_Inline();

        (new Test_Inline_Vars2()).RunTest187_Inline();

        (new Test_Inline_Vars2()).RunTest188_Inline();

        (new Test_Inline_Vars2()).RunTest189_Inline();

        (new Test_Inline_Vars2()).RunTest190_Inline();

        (new Test_Inline_Vars2()).RunTest191_Inline();

        (new Test_Inline_Vars2()).RunTest192_Inline();

        (new Test_Inline_Vars2()).RunTest193_Inline();

        (new Test_Inline_Vars2()).RunTest194_Inline();

        (new Test_Inline_Vars2()).RunTest195_Inline();

        (new Test_Inline_Vars2()).RunTest196_Inline();

        (new Test_Inline_Vars2()).RunTest197_Inline();

        (new Test_Inline_Vars2()).RunTest198_Inline();

        (new Test_Inline_Vars2()).RunTest199_Inline();

        (new Test_Inline_Vars2()).RunTest200_Inline();

        (new Test_Inline_Vars2()).RunTest201_Inline();

        (new Test_Inline_Vars2()).RunTest202_Inline();

        (new Test_Inline_Vars2()).RunTest203_Inline();

        (new Test_Inline_Vars2()).RunTest204_Inline();

        (new Test_Inline_Vars2()).RunTest205_Inline();

        (new Test_Inline_Vars2()).RunTest206_Inline();

        (new Test_Inline_Vars2()).RunTest207_Inline();

        (new Test_Inline_Vars2()).RunTest208_Inline();

        (new Test_Inline_Vars2()).RunTest209_Inline();

        (new Test_Inline_Vars2()).RunTest210_Inline();

        (new Test_Inline_Vars2()).RunTest211_Inline();

        (new Test_Inline_Vars2()).RunTest212_Inline();

        (new Test_Inline_Vars2()).RunTest213_Inline();

        (new Test_Inline_Vars2()).RunTest214_Inline();

        (new Test_Inline_Vars2()).RunTest215_Inline();

        (new Test_Inline_Vars2()).RunTest216_Inline();

        (new Test_Inline_Vars2()).RunTest217_Inline();

        (new Test_Inline_Vars2()).RunTest218_Inline();

        (new Test_Inline_Vars2()).RunTest219_Inline();

        (new Test_Inline_Vars2()).RunTest220_Inline();

        (new Test_Inline_Vars2()).RunTest221_Inline();

        (new Test_Inline_Vars2()).RunTest222_Inline();

        (new Test_Inline_Vars2()).RunTest223_Inline();

        (new Test_Inline_Vars2()).RunTest224_Inline();

        (new Test_Inline_Vars2()).RunTest225_Inline();

        (new Test_Inline_Vars2()).RunTest226_Inline();

        (new Test_Inline_Vars2()).RunTest227_Inline();

        (new Test_Inline_Vars2()).RunTest228_Inline();

        (new Test_Inline_Vars2()).RunTest229_Inline();

        (new Test_Inline_Vars2()).RunTest230_Inline();

        (new Test_Inline_Vars2()).RunTest231_Inline();

        (new Test_Inline_Vars2()).RunTest232_Inline();

        (new Test_Inline_Vars2()).RunTest233_Inline();

        (new Test_Inline_Vars2()).RunTest234_Inline();

        (new Test_Inline_Vars2()).RunTest235_Inline();

        (new Test_Inline_Vars2()).RunTest236_Inline();

        (new Test_Inline_Vars2()).RunTest237_Inline();

        (new Test_Inline_Vars2()).RunTest238_Inline();

        (new Test_Inline_Vars2()).RunTest239_Inline();

        (new Test_Inline_Vars2()).RunTest240_Inline();

        (new Test_Inline_Vars2()).RunTest241_Inline();

        (new Test_Inline_Vars2()).RunTest242_Inline();

        (new Test_Inline_Vars2()).RunTest243_Inline();

        (new Test_Inline_Vars2()).RunTest244_Inline();

        (new Test_Inline_Vars2()).RunTest245_Inline();

        (new Test_Inline_Vars2()).RunTest246_Inline();

        (new Test_Inline_Vars2()).RunTest247_Inline();

        (new Test_Inline_Vars2()).RunTest248_Inline();

        (new Test_Inline_Vars2()).RunTest249_Inline();

        (new Test_Inline_Vars2()).RunTest250_Inline();

        (new Test_Inline_Vars2()).RunTest251_Inline();

        (new Test_Inline_Vars2()).RunTest252_Inline();

        (new Test_Inline_Vars2()).RunTest253_Inline();

        (new Test_Inline_Vars2()).RunTest254_Inline();

        (new Test_Inline_Vars2()).RunTest255_Inline();

        (new Test_Inline_Vars2()).RunTest256_Inline();

        (new Test_Inline_Vars2()).RunTest257_Inline();

        (new Test_Inline_Vars2()).RunTest258_Inline();

        (new Test_Inline_Vars2()).RunTest259_Inline();

        (new Test_Inline_Vars2()).RunTest260_Inline();

        (new Test_Inline_Vars2()).RunTest261_Inline();

        (new Test_Inline_Vars2()).RunTest262_Inline();

        (new Test_Inline_Vars2()).RunTest263_Inline();

        (new Test_Inline_Vars2()).RunTest264_Inline();

        (new Test_Inline_Vars2()).RunTest265_Inline();

        (new Test_Inline_Vars2()).RunTest266_Inline();

        (new Test_Inline_Vars2()).RunTest267_Inline();

        (new Test_Inline_Vars2()).RunTest268_Inline();

        (new Test_Inline_Vars2()).RunTest269_Inline();

        (new Test_Inline_Vars2()).RunTest270_Inline();

        (new Test_Inline_Vars2()).RunTest271_Inline();

        (new Test_Inline_Vars2()).RunTest272_Inline();

        (new Test_Inline_Vars2()).RunTest273_Inline();

        (new Test_Inline_Vars2()).RunTest274_Inline();

        (new Test_Inline_Vars2()).RunTest275_Inline();

        (new Test_Inline_Vars2()).RunTest276_Inline();

        (new Test_Inline_Vars2()).RunTest277_Inline();

        (new Test_Inline_Vars2()).RunTest278_Inline();

        (new Test_Inline_Vars2()).RunTest279_Inline();

        (new Test_Inline_Vars2()).RunTest280_Inline();

        (new Test_Inline_Vars2()).RunTest281_Inline();

        (new Test_Inline_Vars2()).RunTest282_Inline();

        (new Test_Inline_Vars2()).RunTest283_Inline();

        (new Test_Inline_Vars2()).RunTest284_Inline();

        (new Test_Inline_Vars2()).RunTest285_Inline();

        (new Test_Inline_Vars2()).RunTest286_Inline();

        (new Test_Inline_Vars2()).RunTest287_Inline();

        (new Test_Inline_Vars2()).RunTest288_Inline();

        (new Test_Inline_Vars2()).RunTest289_Inline();

        (new Test_Inline_Vars2()).RunTest290_Inline();

        (new Test_Inline_Vars2()).RunTest291_Inline();

        (new Test_Inline_Vars2()).RunTest292_Inline();

        (new Test_Inline_Vars2()).RunTest293_Inline();

        (new Test_Inline_Vars2()).RunTest294_Inline();

        (new Test_Inline_Vars2()).RunTest295_Inline();

        (new Test_Inline_Vars2()).RunTest296_Inline();

        (new Test_Inline_Vars2()).RunTest297_Inline();

        (new Test_Inline_Vars2()).RunTest298_Inline();

        (new Test_Inline_Vars2()).RunTest299_Inline();

        (new Test_Inline_Vars2()).RunTest300_Inline();

        (new Test_Inline_Vars2()).RunTest301_Inline();

        (new Test_Inline_Vars2()).RunTest302_Inline();

        (new Test_Inline_Vars2()).RunTest303_Inline();

        (new Test_Inline_Vars2()).RunTest304_Inline();

        (new Test_Inline_Vars2()).RunTest305_Inline();

        (new Test_Inline_Vars2()).RunTest306_Inline();

        (new Test_Inline_Vars2()).RunTest307_Inline();

        (new Test_Inline_Vars2()).RunTest308_Inline();

        (new Test_Inline_Vars2()).RunTest309_Inline();

        (new Test_Inline_Vars2()).RunTest310_Inline();

        (new Test_Inline_Vars2()).RunTest311_Inline();

        (new Test_Inline_Vars2()).RunTest312_Inline();

        (new Test_Inline_Vars2()).RunTest313_Inline();

        (new Test_Inline_Vars2()).RunTest314_Inline();

        (new Test_Inline_Vars2()).RunTest315_Inline();

        (new Test_Inline_Vars2()).RunTest316_Inline();

        (new Test_Inline_Vars2()).RunTest317_Inline();

        (new Test_Inline_Vars2()).RunTest318_Inline();

        (new Test_Inline_Vars2()).RunTest319_Inline();

        (new Test_Inline_Vars2()).RunTest320_Inline();

        (new Test_Inline_Vars2()).RunTest321_Inline();

        (new Test_Inline_Vars2()).RunTest322_Inline();

        (new Test_Inline_Vars2()).RunTest323_Inline();

        (new Test_Inline_Vars2()).RunTest324_Inline();

        (new Test_Inline_Vars2()).RunTest325_Inline();

        (new Test_Inline_Vars2()).RunTest326_Inline();

        (new Test_Inline_Vars2()).RunTest327_Inline();

        (new Test_Inline_Vars2()).RunTest328_Inline();

        (new Test_Inline_Vars2()).RunTest329_Inline();

        (new Test_Inline_Vars2()).RunTest330_Inline();

        (new Test_Inline_Vars2()).RunTest331_Inline();

        (new Test_Inline_Vars2()).RunTest332_Inline();

        (new Test_Inline_Vars2()).RunTest333_Inline();

        (new Test_Inline_Vars2()).RunTest334_Inline();

        (new Test_Inline_Vars2()).RunTest335_Inline();

        (new Test_Inline_Vars2()).RunTest336_Inline();

        (new Test_Inline_Vars2()).RunTest337_Inline();

        (new Test_Inline_Vars2()).RunTest338_Inline();

        (new Test_Inline_Vars2()).RunTest339_Inline();

        (new Test_Inline_Vars2()).RunTest340_Inline();

        (new Test_Inline_Vars2()).RunTest341_Inline();

        (new Test_Inline_Vars2()).RunTest342_Inline();

        (new Test_Inline_Vars2()).RunTest343_Inline();

        (new Test_Inline_Vars2()).RunTest344_Inline();

        (new Test_Inline_Vars2()).RunTest345_Inline();

        (new Test_Inline_Vars2()).RunTest346_Inline();

        (new Test_Inline_Vars2()).RunTest347_Inline();

        (new Test_Inline_Vars2()).RunTest348_Inline();

        (new Test_Inline_Vars2()).RunTest349_Inline();

        (new Test_Inline_Vars2()).RunTest350_Inline();

        (new Test_Inline_Vars2()).RunTest351_Inline();

        (new Test_Inline_Vars2()).RunTest352_Inline();

        (new Test_Inline_Vars2()).RunTest353_Inline();

        (new Test_Inline_Vars2()).RunTest354_Inline();

        (new Test_Inline_Vars2()).RunTest355_Inline();

        (new Test_Inline_Vars2()).RunTest356_Inline();

        (new Test_Inline_Vars2()).RunTest357_Inline();

        (new Test_Inline_Vars2()).RunTest358_Inline();

        (new Test_Inline_Vars2()).RunTest359_Inline();

        (new Test_Inline_Vars2()).RunTest360_Inline();

        (new Test_Inline_Vars2()).RunTest361_Inline();

        (new Test_Inline_Vars2()).RunTest362_Inline();

        (new Test_Inline_Vars2()).RunTest363_Inline();

        (new Test_Inline_Vars2()).RunTest364_Inline();

        (new Test_Inline_Vars2()).RunTest365_Inline();

        (new Test_Inline_Vars2()).RunTest366_Inline();

        (new Test_Inline_Vars2()).RunTest367_Inline();

        (new Test_Inline_Vars2()).RunTest368_Inline();

        (new Test_Inline_Vars2()).RunTest369_Inline();

        (new Test_Inline_Vars2()).RunTest370_Inline();

        (new Test_Inline_Vars2()).RunTest371_Inline();

        (new Test_Inline_Vars2()).RunTest372_Inline();

        (new Test_Inline_Vars2()).RunTest373_Inline();

        (new Test_Inline_Vars2()).RunTest374_Inline();

        (new Test_Inline_Vars2()).RunTest375_Inline();

        (new Test_Inline_Vars2()).RunTest376_Inline();

        (new Test_Inline_Vars2()).RunTest377_Inline();

        (new Test_Inline_Vars2()).RunTest378_Inline();

        (new Test_Inline_Vars2()).RunTest379_Inline();

        (new Test_Inline_Vars2()).RunTest380_Inline();

        (new Test_Inline_Vars2()).RunTest381_Inline();

        (new Test_Inline_Vars2()).RunTest382_Inline();

        (new Test_Inline_Vars2()).RunTest383_Inline();

        (new Test_Inline_Vars2()).RunTest384_Inline();

        (new Test_Inline_Vars2()).RunTest385_Inline();

        (new Test_Inline_Vars2()).RunTest386_Inline();

        (new Test_Inline_Vars2()).RunTest387_Inline();

        (new Test_Inline_Vars2()).RunTest388_Inline();

        (new Test_Inline_Vars2()).RunTest389_Inline();

        (new Test_Inline_Vars2()).RunTest390_Inline();

        (new Test_Inline_Vars2()).RunTest391_Inline();

        (new Test_Inline_Vars2()).RunTest392_Inline();

        (new Test_Inline_Vars2()).RunTest393_Inline();

        (new Test_Inline_Vars2()).RunTest394_Inline();

        (new Test_Inline_Vars2()).RunTest395_Inline();

        (new Test_Inline_Vars2()).RunTest396_Inline();

        (new Test_Inline_Vars2()).RunTest397_Inline();

        (new Test_Inline_Vars2()).RunTest398_Inline();

        (new Test_Inline_Vars2()).RunTest399_Inline();

        (new Test_Inline_Vars2()).RunTest400_Inline();
        return 100;

    }



}


