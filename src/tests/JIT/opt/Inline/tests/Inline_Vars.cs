// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

#pragma warning disable 219
public class Test_Inline_Vars
{
    public int RunTest0_Inline()
    {
        long a0 = 0;
        a0++;
        Console.WriteLine(a0);
        return 100;
    }
    public int RunTest1()
    {
        long a1 = 1;
        a1++;
        Console.WriteLine(a1);
        return 100;
    }
    public int RunTest2()
    {
        long a2 = 2;
        a2++;
        Console.WriteLine(a2);
        return 100;
    }
    public int RunTest3()
    {
        long a3 = 3;
        a3++;
        Console.WriteLine(a3);
        return 100;
    }
    public int RunTest4()
    {
        long a4 = 4;
        a4++;
        Console.WriteLine(a4);
        return 100;
    }
    public int RunTest5()
    {
        long a5 = 5;
        a5++;
        Console.WriteLine(a5);
        return 100;
    }
    public int RunTest6()
    {
        long a6 = 6;
        a6++;
        Console.WriteLine(a6);
        return 100;
    }
    public int RunTest7()
    {
        long a7 = 7;
        a7++;
        Console.WriteLine(a7);
        return 100;
    }
    public int RunTest8()
    {
        long a8 = 8;
        a8++;
        Console.WriteLine(a8);
        return 100;
    }
    public int RunTest9()
    {
        long a9 = 9;
        a9++;
        Console.WriteLine(a9);
        return 100;
    }
    public int RunTest10()
    {
        long a10 = 10;
        a10++;
        Console.WriteLine(a10);
        return 100;
    }
    public int RunTest11()
    {
        long a11 = 11;
        a11++;
        Console.WriteLine(a11);
        return 100;
    }
    public int RunTest12()
    {
        long a12 = 12;
        a12++;
        Console.WriteLine(a12);
        return 100;
    }
    public int RunTest13()
    {
        long a13 = 13;
        a13++;
        Console.WriteLine(a13);
        return 100;
    }
    public int RunTest14()
    {
        long a14 = 14;
        a14++;
        Console.WriteLine(a14);
        return 100;
    }
    public int RunTest15()
    {
        long a15 = 15;
        a15++;
        Console.WriteLine(a15);
        return 100;
    }
    public int RunTest16()
    {
        long a16 = 16;
        a16++;
        Console.WriteLine(a16);
        return 100;
    }
    public int RunTest17()
    {
        long a17 = 17;
        a17++;
        Console.WriteLine(a17);
        return 100;
    }
    public int RunTest18()
    {
        long a18 = 18;
        a18++;
        Console.WriteLine(a18);
        return 100;
    }
    public int RunTest19()
    {
        long a19 = 19;
        a19++;
        Console.WriteLine(a19);
        return 100;
    }
    public int RunTest20()
    {
        long a20 = 20;
        a20++;
        Console.WriteLine(a20);
        return 100;
    }
    public int RunTest21()
    {
        long a21 = 21;
        a21++;
        Console.WriteLine(a21);
        return 100;
    }
    public int RunTest22()
    {
        long a22 = 22;
        a22++;
        Console.WriteLine(a22);
        return 100;
    }
    public int RunTest23()
    {
        long a23 = 23;
        a23++;
        Console.WriteLine(a23);
        return 100;
    }
    public int RunTest24()
    {
        long a24 = 24;
        a24++;
        Console.WriteLine(a24);
        return 100;
    }
    public int RunTest25()
    {
        long a25 = 25;
        a25++;
        Console.WriteLine(a25);
        return 100;
    }
    public int RunTest26()
    {
        long a26 = 26;
        a26++;
        Console.WriteLine(a26);
        return 100;
    }
    public int RunTest27()
    {
        long a27 = 27;
        a27++;
        Console.WriteLine(a27);
        return 100;
    }
    public int RunTest28()
    {
        long a28 = 28;
        a28++;
        Console.WriteLine(a28);
        return 100;
    }
    public int RunTest29()
    {
        long a29 = 29;
        a29++;
        Console.WriteLine(a29);
        return 100;
    }
    public int RunTest30()
    {
        long a30 = 30;
        a30++;
        Console.WriteLine(a30);
        return 100;
    }
    public int RunTest31()
    {
        long a31 = 31;
        a31++;
        Console.WriteLine(a31);
        return 100;
    }
    public int RunTest32()
    {
        long a32 = 32;
        a32++;
        Console.WriteLine(a32);
        return 100;
    }
    public int RunTest33()
    {
        long a33 = 33;
        a33++;
        Console.WriteLine(a33);
        return 100;
    }
    public int RunTest34()
    {
        long a34 = 34;
        a34++;
        Console.WriteLine(a34);
        return 100;
    }
    public int RunTest35()
    {
        long a35 = 35;
        a35++;
        Console.WriteLine(a35);
        return 100;
    }
    public int RunTest36()
    {
        long a36 = 36;
        a36++;
        Console.WriteLine(a36);
        return 100;
    }
    public int RunTest37()
    {
        long a37 = 37;
        a37++;
        Console.WriteLine(a37);
        return 100;
    }
    public int RunTest38()
    {
        long a38 = 38;
        a38++;
        Console.WriteLine(a38);
        return 100;
    }
    public int RunTest39()
    {
        long a39 = 39;
        a39++;
        Console.WriteLine(a39);
        return 100;
    }
    public int RunTest40()
    {
        long a40 = 40;
        a40++;
        Console.WriteLine(a40);
        return 100;
    }
    public int RunTest41()
    {
        long a41 = 41;
        a41++;
        Console.WriteLine(a41);
        return 100;
    }
    public int RunTest42()
    {
        long a42 = 42;
        a42++;
        Console.WriteLine(a42);
        return 100;
    }
    public int RunTest43()
    {
        long a43 = 43;
        a43++;
        Console.WriteLine(a43);
        return 100;
    }
    public int RunTest44()
    {
        long a44 = 44;
        a44++;
        Console.WriteLine(a44);
        return 100;
    }
    public int RunTest45()
    {
        long a45 = 45;
        a45++;
        Console.WriteLine(a45);
        return 100;
    }
    public int RunTest46()
    {
        long a46 = 46;
        a46++;
        Console.WriteLine(a46);
        return 100;
    }
    public int RunTest47()
    {
        long a47 = 47;
        a47++;
        Console.WriteLine(a47);
        return 100;
    }
    public int RunTest48()
    {
        long a48 = 48;
        a48++;
        Console.WriteLine(a48);
        return 100;
    }
    public int RunTest49()
    {
        long a49 = 49;
        a49++;
        Console.WriteLine(a49);
        return 100;
    }
    public int RunTest50()
    {
        long a50 = 50;
        a50++;
        Console.WriteLine(a50);
        return 100;
    }
    public int RunTest51()
    {
        long a51 = 51;
        a51++;
        Console.WriteLine(a51);
        return 100;
    }
    public int RunTest52()
    {
        long a52 = 52;
        a52++;
        Console.WriteLine(a52);
        return 100;
    }
    public int RunTest53()
    {
        long a53 = 53;
        a53++;
        Console.WriteLine(a53);
        return 100;
    }
    public int RunTest54()
    {
        long a54 = 54;
        a54++;
        Console.WriteLine(a54);
        return 100;
    }
    public int RunTest55()
    {
        long a55 = 55;
        a55++;
        Console.WriteLine(a55);
        return 100;
    }
    public int RunTest56()
    {
        long a56 = 56;
        a56++;
        Console.WriteLine(a56);
        return 100;
    }
    public int RunTest57()
    {
        long a57 = 57;
        a57++;
        Console.WriteLine(a57);
        return 100;
    }
    public int RunTest58()
    {
        long a58 = 58;
        a58++;
        Console.WriteLine(a58);
        return 100;
    }
    public int RunTest59()
    {
        long a59 = 59;
        a59++;
        Console.WriteLine(a59);
        return 100;
    }
    public int RunTest60()
    {
        long a60 = 60;
        a60++;
        Console.WriteLine(a60);
        return 100;
    }
    public int RunTest61()
    {
        long a61 = 61;
        a61++;
        Console.WriteLine(a61);
        return 100;
    }
    public int RunTest62()
    {
        long a62 = 62;
        a62++;
        Console.WriteLine(a62);
        return 100;
    }
    public int RunTest63()
    {
        long a63 = 63;
        a63++;
        Console.WriteLine(a63);
        return 100;
    }
    public int RunTest64()
    {
        long a64 = 64;
        a64++;
        Console.WriteLine(a64);
        return 100;
    }
    public int RunTest65()
    {
        long a65 = 65;
        a65++;
        Console.WriteLine(a65);
        return 100;
    }
    public int RunTest66()
    {
        long a66 = 66;
        a66++;
        Console.WriteLine(a66);
        return 100;
    }
    public int RunTest67()
    {
        long a67 = 67;
        a67++;
        Console.WriteLine(a67);
        return 100;
    }
    public int RunTest68()
    {
        long a68 = 68;
        a68++;
        Console.WriteLine(a68);
        return 100;
    }
    public int RunTest69()
    {
        long a69 = 69;
        a69++;
        Console.WriteLine(a69);
        return 100;
    }
    public int RunTest70()
    {
        long a70 = 70;
        a70++;
        Console.WriteLine(a70);
        return 100;
    }
    public int RunTest71()
    {
        long a71 = 71;
        a71++;
        Console.WriteLine(a71);
        return 100;
    }
    public int RunTest72()
    {
        long a72 = 72;
        a72++;
        Console.WriteLine(a72);
        return 100;
    }
    public int RunTest73()
    {
        long a73 = 73;
        a73++;
        Console.WriteLine(a73);
        return 100;
    }
    public int RunTest74()
    {
        long a74 = 74;
        a74++;
        Console.WriteLine(a74);
        return 100;
    }
    public int RunTest75()
    {
        long a75 = 75;
        a75++;
        Console.WriteLine(a75);
        return 100;
    }
    public int RunTest76()
    {
        long a76 = 76;
        a76++;
        Console.WriteLine(a76);
        return 100;
    }
    public int RunTest77()
    {
        long a77 = 77;
        a77++;
        Console.WriteLine(a77);
        return 100;
    }
    public int RunTest78()
    {
        long a78 = 78;
        a78++;
        Console.WriteLine(a78);
        return 100;
    }
    public int RunTest79()
    {
        long a79 = 79;
        a79++;
        Console.WriteLine(a79);
        return 100;
    }
    public int RunTest80()
    {
        long a80 = 80;
        a80++;
        Console.WriteLine(a80);
        return 100;
    }
    public int RunTest81()
    {
        long a81 = 81;
        a81++;
        Console.WriteLine(a81);
        return 100;
    }
    public int RunTest82()
    {
        long a82 = 82;
        a82++;
        Console.WriteLine(a82);
        return 100;
    }
    public int RunTest83()
    {
        long a83 = 83;
        a83++;
        Console.WriteLine(a83);
        return 100;
    }
    public int RunTest84()
    {
        long a84 = 84;
        a84++;
        Console.WriteLine(a84);
        return 100;
    }
    public int RunTest85()
    {
        long a85 = 85;
        a85++;
        Console.WriteLine(a85);
        return 100;
    }
    public int RunTest86()
    {
        long a86 = 86;
        a86++;
        Console.WriteLine(a86);
        return 100;
    }
    public int RunTest87()
    {
        long a87 = 87;
        a87++;
        Console.WriteLine(a87);
        return 100;
    }
    public int RunTest88()
    {
        long a88 = 88;
        a88++;
        Console.WriteLine(a88);
        return 100;
    }
    public int RunTest89()
    {
        long a89 = 89;
        a89++;
        Console.WriteLine(a89);
        return 100;
    }
    public int RunTest90()
    {
        long a90 = 90;
        a90++;
        Console.WriteLine(a90);
        return 100;
    }
    public int RunTest91()
    {
        long a91 = 91;
        a91++;
        Console.WriteLine(a91);
        return 100;
    }
    public int RunTest92()
    {
        long a92 = 92;
        a92++;
        Console.WriteLine(a92);
        return 100;
    }
    public int RunTest93()
    {
        long a93 = 93;
        a93++;
        Console.WriteLine(a93);
        return 100;
    }
    public int RunTest94()
    {
        long a94 = 94;
        a94++;
        Console.WriteLine(a94);
        return 100;
    }
    public int RunTest95()
    {
        long a95 = 95;
        a95++;
        Console.WriteLine(a95);
        return 100;
    }
    public int RunTest96()
    {
        long a96 = 96;
        a96++;
        Console.WriteLine(a96);
        return 100;
    }
    public int RunTest97()
    {
        long a97 = 97;
        a97++;
        Console.WriteLine(a97);
        return 100;
    }
    public int RunTest98()
    {
        long a98 = 98;
        a98++;
        Console.WriteLine(a98);
        return 100;
    }
    public int RunTest99()
    {
        long a99 = 99;
        a99++;
        Console.WriteLine(a99);
        return 100;
    }
    public int RunTest100()
    {
        long a100 = 100;
        a100++;
        Console.WriteLine(a100);
        return 100;
    }
    public int RunTest101()
    {
        long a101 = 101;
        a101++;
        Console.WriteLine(a101);
        return 100;
    }
    public int RunTest102()
    {
        long a102 = 102;
        a102++;
        Console.WriteLine(a102);
        return 100;
    }
    public int RunTest103()
    {
        long a103 = 103;
        a103++;
        Console.WriteLine(a103);
        return 100;
    }
    public int RunTest104()
    {
        long a104 = 104;
        a104++;
        Console.WriteLine(a104);
        return 100;
    }
    public int RunTest105()
    {
        long a105 = 105;
        a105++;
        Console.WriteLine(a105);
        return 100;
    }
    public int RunTest106()
    {
        long a106 = 106;
        a106++;
        Console.WriteLine(a106);
        return 100;
    }
    public int RunTest107()
    {
        long a107 = 107;
        a107++;
        Console.WriteLine(a107);
        return 100;
    }
    public int RunTest108()
    {
        long a108 = 108;
        a108++;
        Console.WriteLine(a108);
        return 100;
    }
    public int RunTest109()
    {
        long a109 = 109;
        a109++;
        Console.WriteLine(a109);
        return 100;
    }
    public int RunTest110()
    {
        long a110 = 110;
        a110++;
        Console.WriteLine(a110);
        return 100;
    }
    public int RunTest111()
    {
        long a111 = 111;
        a111++;
        Console.WriteLine(a111);
        return 100;
    }
    public int RunTest112()
    {
        long a112 = 112;
        a112++;
        Console.WriteLine(a112);
        return 100;
    }
    public int RunTest113()
    {
        long a113 = 113;
        a113++;
        Console.WriteLine(a113);
        return 100;
    }
    public int RunTest114()
    {
        long a114 = 114;
        a114++;
        Console.WriteLine(a114);
        return 100;
    }
    public int RunTest115()
    {
        long a115 = 115;
        a115++;
        Console.WriteLine(a115);
        return 100;
    }
    public int RunTest116()
    {
        long a116 = 116;
        a116++;
        Console.WriteLine(a116);
        return 100;
    }
    public int RunTest117()
    {
        long a117 = 117;
        a117++;
        Console.WriteLine(a117);
        return 100;
    }
    public int RunTest118()
    {
        long a118 = 118;
        a118++;
        Console.WriteLine(a118);
        return 100;
    }
    public int RunTest119()
    {
        long a119 = 119;
        a119++;
        Console.WriteLine(a119);
        return 100;
    }
    public int RunTest120()
    {
        long a120 = 120;
        a120++;
        Console.WriteLine(a120);
        return 100;
    }
    public int RunTest121()
    {
        long a121 = 121;
        a121++;
        Console.WriteLine(a121);
        return 100;
    }
    public int RunTest122()
    {
        long a122 = 122;
        a122++;
        Console.WriteLine(a122);
        return 100;
    }
    public int RunTest123()
    {
        long a123 = 123;
        a123++;
        Console.WriteLine(a123);
        return 100;
    }
    public int RunTest124()
    {
        long a124 = 124;
        a124++;
        Console.WriteLine(a124);
        return 100;
    }
    public int RunTest125()
    {
        long a125 = 125;
        a125++;
        Console.WriteLine(a125);
        return 100;
    }
    public int RunTest126()
    {
        long a126 = 126;
        a126++;
        Console.WriteLine(a126);
        return 100;
    }
    public int RunTest127()
    {
        long a127 = 127;
        a127++;
        Console.WriteLine(a127);
        return 100;
    }
    public int RunTest128()
    {
        long a128 = 128;
        a128++;
        Console.WriteLine(a128);
        return 100;
    }
    public int RunTest129()
    {
        long a129 = 129;
        a129++;
        Console.WriteLine(a129);
        return 100;
    }
    public int RunTest130()
    {
        long a130 = 130;
        a130++;
        Console.WriteLine(a130);
        return 100;
    }
    public int RunTest131()
    {
        long a131 = 131;
        a131++;
        Console.WriteLine(a131);
        return 100;
    }
    public int RunTest132()
    {
        long a132 = 132;
        a132++;
        Console.WriteLine(a132);
        return 100;
    }
    public int RunTest133()
    {
        long a133 = 133;
        a133++;
        Console.WriteLine(a133);
        return 100;
    }
    public int RunTest134()
    {
        long a134 = 134;
        a134++;
        Console.WriteLine(a134);
        return 100;
    }
    public int RunTest135()
    {
        long a135 = 135;
        a135++;
        Console.WriteLine(a135);
        return 100;
    }
    public int RunTest136()
    {
        long a136 = 136;
        a136++;
        Console.WriteLine(a136);
        return 100;
    }
    public int RunTest137()
    {
        long a137 = 137;
        a137++;
        Console.WriteLine(a137);
        return 100;
    }
    public int RunTest138()
    {
        long a138 = 138;
        a138++;
        Console.WriteLine(a138);
        return 100;
    }
    public int RunTest139()
    {
        long a139 = 139;
        a139++;
        Console.WriteLine(a139);
        return 100;
    }
    public int RunTest140()
    {
        long a140 = 140;
        a140++;
        Console.WriteLine(a140);
        return 100;
    }
    public int RunTest141()
    {
        long a141 = 141;
        a141++;
        Console.WriteLine(a141);
        return 100;
    }
    public int RunTest142()
    {
        long a142 = 142;
        a142++;
        Console.WriteLine(a142);
        return 100;
    }
    public int RunTest143()
    {
        long a143 = 143;
        a143++;
        Console.WriteLine(a143);
        return 100;
    }
    public int RunTest144()
    {
        long a144 = 144;
        a144++;
        Console.WriteLine(a144);
        return 100;
    }
    public int RunTest145()
    {
        long a145 = 145;
        a145++;
        Console.WriteLine(a145);
        return 100;
    }
    public int RunTest146()
    {
        long a146 = 146;
        a146++;
        Console.WriteLine(a146);
        return 100;
    }
    public int RunTest147()
    {
        long a147 = 147;
        a147++;
        Console.WriteLine(a147);
        return 100;
    }
    public int RunTest148()
    {
        long a148 = 148;
        a148++;
        Console.WriteLine(a148);
        return 100;
    }
    public int RunTest149()
    {
        long a149 = 149;
        a149++;
        Console.WriteLine(a149);
        return 100;
    }
    public int RunTest150()
    {
        long a150 = 150;
        a150++;
        Console.WriteLine(a150);
        return 100;
    }
    public int RunTest151()
    {
        long a151 = 151;
        a151++;
        Console.WriteLine(a151);
        return 100;
    }
    public int RunTest152()
    {
        long a152 = 152;
        a152++;
        Console.WriteLine(a152);
        return 100;
    }
    public int RunTest153()
    {
        long a153 = 153;
        a153++;
        Console.WriteLine(a153);
        return 100;
    }
    public int RunTest154()
    {
        long a154 = 154;
        a154++;
        Console.WriteLine(a154);
        return 100;
    }
    public int RunTest155()
    {
        long a155 = 155;
        a155++;
        Console.WriteLine(a155);
        return 100;
    }
    public int RunTest156()
    {
        long a156 = 156;
        a156++;
        Console.WriteLine(a156);
        return 100;
    }
    public int RunTest157()
    {
        long a157 = 157;
        a157++;
        Console.WriteLine(a157);
        return 100;
    }
    public int RunTest158()
    {
        long a158 = 158;
        a158++;
        Console.WriteLine(a158);
        return 100;
    }
    public int RunTest159()
    {
        long a159 = 159;
        a159++;
        Console.WriteLine(a159);
        return 100;
    }
    public int RunTest160()
    {
        long a160 = 160;
        a160++;
        Console.WriteLine(a160);
        return 100;
    }
    public int RunTest161()
    {
        long a161 = 161;
        a161++;
        Console.WriteLine(a161);
        return 100;
    }
    public int RunTest162()
    {
        long a162 = 162;
        a162++;
        Console.WriteLine(a162);
        return 100;
    }
    public int RunTest163()
    {
        long a163 = 163;
        a163++;
        Console.WriteLine(a163);
        return 100;
    }
    public int RunTest164()
    {
        long a164 = 164;
        a164++;
        Console.WriteLine(a164);
        return 100;
    }
    public int RunTest165()
    {
        long a165 = 165;
        a165++;
        Console.WriteLine(a165);
        return 100;
    }
    public int RunTest166()
    {
        long a166 = 166;
        a166++;
        Console.WriteLine(a166);
        return 100;
    }
    public int RunTest167()
    {
        long a167 = 167;
        a167++;
        Console.WriteLine(a167);
        return 100;
    }
    public int RunTest168()
    {
        long a168 = 168;
        a168++;
        Console.WriteLine(a168);
        return 100;
    }
    public int RunTest169()
    {
        long a169 = 169;
        a169++;
        Console.WriteLine(a169);
        return 100;
    }
    public int RunTest170()
    {
        long a170 = 170;
        a170++;
        Console.WriteLine(a170);
        return 100;
    }
    public int RunTest171()
    {
        long a171 = 171;
        a171++;
        Console.WriteLine(a171);
        return 100;
    }
    public int RunTest172()
    {
        long a172 = 172;
        a172++;
        Console.WriteLine(a172);
        return 100;
    }
    public int RunTest173()
    {
        long a173 = 173;
        a173++;
        Console.WriteLine(a173);
        return 100;
    }
    public int RunTest174()
    {
        long a174 = 174;
        a174++;
        Console.WriteLine(a174);
        return 100;
    }
    public int RunTest175()
    {
        long a175 = 175;
        a175++;
        Console.WriteLine(a175);
        return 100;
    }
    public int RunTest176()
    {
        long a176 = 176;
        a176++;
        Console.WriteLine(a176);
        return 100;
    }
    public int RunTest177()
    {
        long a177 = 177;
        a177++;
        Console.WriteLine(a177);
        return 100;
    }
    public int RunTest178()
    {
        long a178 = 178;
        a178++;
        Console.WriteLine(a178);
        return 100;
    }
    public int RunTest179()
    {
        long a179 = 179;
        a179++;
        Console.WriteLine(a179);
        return 100;
    }
    public int RunTest180()
    {
        long a180 = 180;
        a180++;
        Console.WriteLine(a180);
        return 100;
    }
    public int RunTest181()
    {
        long a181 = 181;
        a181++;
        Console.WriteLine(a181);
        return 100;
    }
    public int RunTest182()
    {
        long a182 = 182;
        a182++;
        Console.WriteLine(a182);
        return 100;
    }
    public int RunTest183()
    {
        long a183 = 183;
        a183++;
        Console.WriteLine(a183);
        return 100;
    }
    public int RunTest184()
    {
        long a184 = 184;
        a184++;
        Console.WriteLine(a184);
        return 100;
    }
    public int RunTest185()
    {
        long a185 = 185;
        a185++;
        Console.WriteLine(a185);
        return 100;
    }
    public int RunTest186()
    {
        long a186 = 186;
        a186++;
        Console.WriteLine(a186);
        return 100;
    }
    public int RunTest187()
    {
        long a187 = 187;
        a187++;
        Console.WriteLine(a187);
        return 100;
    }
    public int RunTest188()
    {
        long a188 = 188;
        a188++;
        Console.WriteLine(a188);
        return 100;
    }
    public int RunTest189()
    {
        long a189 = 189;
        a189++;
        Console.WriteLine(a189);
        return 100;
    }
    public int RunTest190()
    {
        long a190 = 190;
        a190++;
        Console.WriteLine(a190);
        return 100;
    }
    public int RunTest191()
    {
        long a191 = 191;
        a191++;
        Console.WriteLine(a191);
        return 100;
    }
    public int RunTest192()
    {
        long a192 = 192;
        a192++;
        Console.WriteLine(a192);
        return 100;
    }
    public int RunTest193()
    {
        long a193 = 193;
        a193++;
        Console.WriteLine(a193);
        return 100;
    }
    public int RunTest194()
    {
        long a194 = 194;
        a194++;
        Console.WriteLine(a194);
        return 100;
    }
    public int RunTest195()
    {
        long a195 = 195;
        a195++;
        Console.WriteLine(a195);
        return 100;
    }
    public int RunTest196()
    {
        long a196 = 196;
        a196++;
        Console.WriteLine(a196);
        return 100;
    }
    public int RunTest197()
    {
        long a197 = 197;
        a197++;
        Console.WriteLine(a197);
        return 100;
    }
    public int RunTest198()
    {
        long a198 = 198;
        a198++;
        Console.WriteLine(a198);
        return 100;
    }
    public int RunTest199()
    {
        long a199 = 199;
        a199++;
        Console.WriteLine(a199);
        return 100;
    }
    public int RunTest200()
    {
        long a200 = 200;
        a200++;
        Console.WriteLine(a200);
        return 100;
    }
    public int RunTest201()
    {
        long a201 = 201;
        a201++;
        Console.WriteLine(a201);
        return 100;
    }
    public int RunTest202()
    {
        long a202 = 202;
        a202++;
        Console.WriteLine(a202);
        return 100;
    }
    public int RunTest203()
    {
        long a203 = 203;
        a203++;
        Console.WriteLine(a203);
        return 100;
    }
    public int RunTest204()
    {
        long a204 = 204;
        a204++;
        Console.WriteLine(a204);
        return 100;
    }
    public int RunTest205()
    {
        long a205 = 205;
        a205++;
        Console.WriteLine(a205);
        return 100;
    }
    public int RunTest206()
    {
        long a206 = 206;
        a206++;
        Console.WriteLine(a206);
        return 100;
    }
    public int RunTest207()
    {
        long a207 = 207;
        a207++;
        Console.WriteLine(a207);
        return 100;
    }
    public int RunTest208()
    {
        long a208 = 208;
        a208++;
        Console.WriteLine(a208);
        return 100;
    }
    public int RunTest209()
    {
        long a209 = 209;
        a209++;
        Console.WriteLine(a209);
        return 100;
    }
    public int RunTest210()
    {
        long a210 = 210;
        a210++;
        Console.WriteLine(a210);
        return 100;
    }
    public int RunTest211()
    {
        long a211 = 211;
        a211++;
        Console.WriteLine(a211);
        return 100;
    }
    public int RunTest212()
    {
        long a212 = 212;
        a212++;
        Console.WriteLine(a212);
        return 100;
    }
    public int RunTest213()
    {
        long a213 = 213;
        a213++;
        Console.WriteLine(a213);
        return 100;
    }
    public int RunTest214()
    {
        long a214 = 214;
        a214++;
        Console.WriteLine(a214);
        return 100;
    }
    public int RunTest215()
    {
        long a215 = 215;
        a215++;
        Console.WriteLine(a215);
        return 100;
    }
    public int RunTest216()
    {
        long a216 = 216;
        a216++;
        Console.WriteLine(a216);
        return 100;
    }
    public int RunTest217()
    {
        long a217 = 217;
        a217++;
        Console.WriteLine(a217);
        return 100;
    }
    public int RunTest218()
    {
        long a218 = 218;
        a218++;
        Console.WriteLine(a218);
        return 100;
    }
    public int RunTest219()
    {
        long a219 = 219;
        a219++;
        Console.WriteLine(a219);
        return 100;
    }
    public int RunTest220()
    {
        long a220 = 220;
        a220++;
        Console.WriteLine(a220);
        return 100;
    }
    public int RunTest221()
    {
        long a221 = 221;
        a221++;
        Console.WriteLine(a221);
        return 100;
    }
    public int RunTest222()
    {
        long a222 = 222;
        a222++;
        Console.WriteLine(a222);
        return 100;
    }
    public int RunTest223()
    {
        long a223 = 223;
        a223++;
        Console.WriteLine(a223);
        return 100;
    }
    public int RunTest224()
    {
        long a224 = 224;
        a224++;
        Console.WriteLine(a224);
        return 100;
    }
    public int RunTest225()
    {
        long a225 = 225;
        a225++;
        Console.WriteLine(a225);
        return 100;
    }
    public int RunTest226()
    {
        long a226 = 226;
        a226++;
        Console.WriteLine(a226);
        return 100;
    }
    public int RunTest227()
    {
        long a227 = 227;
        a227++;
        Console.WriteLine(a227);
        return 100;
    }
    public int RunTest228()
    {
        long a228 = 228;
        a228++;
        Console.WriteLine(a228);
        return 100;
    }
    public int RunTest229()
    {
        long a229 = 229;
        a229++;
        Console.WriteLine(a229);
        return 100;
    }
    public int RunTest230()
    {
        long a230 = 230;
        a230++;
        Console.WriteLine(a230);
        return 100;
    }
    public int RunTest231()
    {
        long a231 = 231;
        a231++;
        Console.WriteLine(a231);
        return 100;
    }
    public int RunTest232()
    {
        long a232 = 232;
        a232++;
        Console.WriteLine(a232);
        return 100;
    }
    public int RunTest233()
    {
        long a233 = 233;
        a233++;
        Console.WriteLine(a233);
        return 100;
    }
    public int RunTest234()
    {
        long a234 = 234;
        a234++;
        Console.WriteLine(a234);
        return 100;
    }
    public int RunTest235()
    {
        long a235 = 235;
        a235++;
        Console.WriteLine(a235);
        return 100;
    }
    public int RunTest236()
    {
        long a236 = 236;
        a236++;
        Console.WriteLine(a236);
        return 100;
    }
    public int RunTest237()
    {
        long a237 = 237;
        a237++;
        Console.WriteLine(a237);
        return 100;
    }
    public int RunTest238()
    {
        long a238 = 238;
        a238++;
        Console.WriteLine(a238);
        return 100;
    }
    public int RunTest239()
    {
        long a239 = 239;
        a239++;
        Console.WriteLine(a239);
        return 100;
    }
    public int RunTest240()
    {
        long a240 = 240;
        a240++;
        Console.WriteLine(a240);
        return 100;
    }
    public int RunTest241()
    {
        long a241 = 241;
        a241++;
        Console.WriteLine(a241);
        return 100;
    }
    public int RunTest242()
    {
        long a242 = 242;
        a242++;
        Console.WriteLine(a242);
        return 100;
    }
    public int RunTest243()
    {
        long a243 = 243;
        a243++;
        Console.WriteLine(a243);
        return 100;
    }
    public int RunTest244()
    {
        long a244 = 244;
        a244++;
        Console.WriteLine(a244);
        return 100;
    }
    public int RunTest245()
    {
        long a245 = 245;
        a245++;
        Console.WriteLine(a245);
        return 100;
    }
    public int RunTest246()
    {
        long a246 = 246;
        a246++;
        Console.WriteLine(a246);
        return 100;
    }
    public int RunTest247()
    {
        long a247 = 247;
        a247++;
        Console.WriteLine(a247);
        return 100;
    }
    public int RunTest248()
    {
        long a248 = 248;
        a248++;
        Console.WriteLine(a248);
        return 100;
    }
    public int RunTest249()
    {
        long a249 = 249;
        a249++;
        Console.WriteLine(a249);
        return 100;
    }
    public int RunTest250()
    {
        long a250 = 250;
        a250++;
        Console.WriteLine(a250);
        return 100;
    }
    public int RunTest251()
    {
        long a251 = 251;
        a251++;
        Console.WriteLine(a251);
        return 100;
    }
    public int RunTest252()
    {
        long a252 = 252;
        a252++;
        Console.WriteLine(a252);
        return 100;
    }
    public int RunTest253()
    {
        long a253 = 253;
        a253++;
        Console.WriteLine(a253);
        return 100;
    }
    public int RunTest254()
    {
        long a254 = 254;
        a254++;
        Console.WriteLine(a254);
        return 100;
    }
    public int RunTest255()
    {
        long a255 = 255;
        a255++;
        Console.WriteLine(a255);
        return 100;
    }
    public int RunTest256()
    {
        long a256 = 256;
        a256++;
        Console.WriteLine(a256);
        return 100;
    }
    public int RunTest257()
    {
        long a257 = 257;
        a257++;
        Console.WriteLine(a257);
        return 100;
    }
    public int RunTest258()
    {
        long a258 = 258;
        a258++;
        Console.WriteLine(a258);
        return 100;
    }
    public int RunTest259()
    {
        long a259 = 259;
        a259++;
        Console.WriteLine(a259);
        return 100;
    }
    public int RunTest260()
    {
        long a260 = 260;
        a260++;
        Console.WriteLine(a260);
        return 100;
    }
    public int RunTest261()
    {
        long a261 = 261;
        a261++;
        Console.WriteLine(a261);
        return 100;
    }
    public int RunTest262()
    {
        long a262 = 262;
        a262++;
        Console.WriteLine(a262);
        return 100;
    }
    public int RunTest263()
    {
        long a263 = 263;
        a263++;
        Console.WriteLine(a263);
        return 100;
    }
    public int RunTest264()
    {
        long a264 = 264;
        a264++;
        Console.WriteLine(a264);
        return 100;
    }
    public int RunTest265()
    {
        long a265 = 265;
        a265++;
        Console.WriteLine(a265);
        return 100;
    }
    public int RunTest266()
    {
        long a266 = 266;
        a266++;
        Console.WriteLine(a266);
        return 100;
    }
    public int RunTest267()
    {
        long a267 = 267;
        a267++;
        Console.WriteLine(a267);
        return 100;
    }
    public int RunTest268()
    {
        long a268 = 268;
        a268++;
        Console.WriteLine(a268);
        return 100;
    }
    public int RunTest269()
    {
        long a269 = 269;
        a269++;
        Console.WriteLine(a269);
        return 100;
    }
    public int RunTest270()
    {
        long a270 = 270;
        a270++;
        Console.WriteLine(a270);
        return 100;
    }
    public int RunTest271()
    {
        long a271 = 271;
        a271++;
        Console.WriteLine(a271);
        return 100;
    }
    public int RunTest272()
    {
        long a272 = 272;
        a272++;
        Console.WriteLine(a272);
        return 100;
    }
    public int RunTest273()
    {
        long a273 = 273;
        a273++;
        Console.WriteLine(a273);
        return 100;
    }
    public int RunTest274()
    {
        long a274 = 274;
        a274++;
        Console.WriteLine(a274);
        return 100;
    }
    public int RunTest275()
    {
        long a275 = 275;
        a275++;
        Console.WriteLine(a275);
        return 100;
    }
    public int RunTest276()
    {
        long a276 = 276;
        a276++;
        Console.WriteLine(a276);
        return 100;
    }
    public int RunTest277()
    {
        long a277 = 277;
        a277++;
        Console.WriteLine(a277);
        return 100;
    }
    public int RunTest278()
    {
        long a278 = 278;
        a278++;
        Console.WriteLine(a278);
        return 100;
    }
    public int RunTest279()
    {
        long a279 = 279;
        a279++;
        Console.WriteLine(a279);
        return 100;
    }
    public int RunTest280()
    {
        long a280 = 280;
        a280++;
        Console.WriteLine(a280);
        return 100;
    }
    public int RunTest281()
    {
        long a281 = 281;
        a281++;
        Console.WriteLine(a281);
        return 100;
    }
    public int RunTest282()
    {
        long a282 = 282;
        a282++;
        Console.WriteLine(a282);
        return 100;
    }
    public int RunTest283()
    {
        long a283 = 283;
        a283++;
        Console.WriteLine(a283);
        return 100;
    }
    public int RunTest284()
    {
        long a284 = 284;
        a284++;
        Console.WriteLine(a284);
        return 100;
    }
    public int RunTest285()
    {
        long a285 = 285;
        a285++;
        Console.WriteLine(a285);
        return 100;
    }
    public int RunTest286()
    {
        long a286 = 286;
        a286++;
        Console.WriteLine(a286);
        return 100;
    }
    public int RunTest287()
    {
        long a287 = 287;
        a287++;
        Console.WriteLine(a287);
        return 100;
    }
    public int RunTest288()
    {
        long a288 = 288;
        a288++;
        Console.WriteLine(a288);
        return 100;
    }
    public int RunTest289()
    {
        long a289 = 289;
        a289++;
        Console.WriteLine(a289);
        return 100;
    }
    public int RunTest290()
    {
        long a290 = 290;
        a290++;
        Console.WriteLine(a290);
        return 100;
    }
    public int RunTest291()
    {
        long a291 = 291;
        a291++;
        Console.WriteLine(a291);
        return 100;
    }
    public int RunTest292()
    {
        long a292 = 292;
        a292++;
        Console.WriteLine(a292);
        return 100;
    }
    public int RunTest293()
    {
        long a293 = 293;
        a293++;
        Console.WriteLine(a293);
        return 100;
    }
    public int RunTest294()
    {
        long a294 = 294;
        a294++;
        Console.WriteLine(a294);
        return 100;
    }
    public int RunTest295()
    {
        long a295 = 295;
        a295++;
        Console.WriteLine(a295);
        return 100;
    }
    public int RunTest296()
    {
        long a296 = 296;
        a296++;
        Console.WriteLine(a296);
        return 100;
    }
    public int RunTest297()
    {
        long a297 = 297;
        a297++;
        Console.WriteLine(a297);
        return 100;
    }
    public int RunTest298()
    {
        long a298 = 298;
        a298++;
        Console.WriteLine(a298);
        return 100;
    }
    public int RunTest299()
    {
        long a299 = 299;
        a299++;
        Console.WriteLine(a299);
        return 100;
    }
    public int RunTest300()
    {
        long a300 = 300;
        a300++;
        Console.WriteLine(a300);
        return 100;
    }
    public int RunTest301()
    {
        long a301 = 301;
        a301++;
        Console.WriteLine(a301);
        return 100;
    }
    public int RunTest302()
    {
        long a302 = 302;
        a302++;
        Console.WriteLine(a302);
        return 100;
    }
    public int RunTest303()
    {
        long a303 = 303;
        a303++;
        Console.WriteLine(a303);
        return 100;
    }
    public int RunTest304()
    {
        long a304 = 304;
        a304++;
        Console.WriteLine(a304);
        return 100;
    }
    public int RunTest305()
    {
        long a305 = 305;
        a305++;
        Console.WriteLine(a305);
        return 100;
    }
    public int RunTest306()
    {
        long a306 = 306;
        a306++;
        Console.WriteLine(a306);
        return 100;
    }
    public int RunTest307()
    {
        long a307 = 307;
        a307++;
        Console.WriteLine(a307);
        return 100;
    }
    public int RunTest308()
    {
        long a308 = 308;
        a308++;
        Console.WriteLine(a308);
        return 100;
    }
    public int RunTest309()
    {
        long a309 = 309;
        a309++;
        Console.WriteLine(a309);
        return 100;
    }
    public int RunTest310()
    {
        long a310 = 310;
        a310++;
        Console.WriteLine(a310);
        return 100;
    }
    public int RunTest311()
    {
        long a311 = 311;
        a311++;
        Console.WriteLine(a311);
        return 100;
    }
    public int RunTest312()
    {
        long a312 = 312;
        a312++;
        Console.WriteLine(a312);
        return 100;
    }
    public int RunTest313()
    {
        long a313 = 313;
        a313++;
        Console.WriteLine(a313);
        return 100;
    }
    public int RunTest314()
    {
        long a314 = 314;
        a314++;
        Console.WriteLine(a314);
        return 100;
    }
    public int RunTest315()
    {
        long a315 = 315;
        a315++;
        Console.WriteLine(a315);
        return 100;
    }
    public int RunTest316()
    {
        long a316 = 316;
        a316++;
        Console.WriteLine(a316);
        return 100;
    }
    public int RunTest317()
    {
        long a317 = 317;
        a317++;
        Console.WriteLine(a317);
        return 100;
    }
    public int RunTest318()
    {
        long a318 = 318;
        a318++;
        Console.WriteLine(a318);
        return 100;
    }
    public int RunTest319()
    {
        long a319 = 319;
        a319++;
        Console.WriteLine(a319);
        return 100;
    }
    public int RunTest320()
    {
        long a320 = 320;
        a320++;
        Console.WriteLine(a320);
        return 100;
    }
    public int RunTest321()
    {
        long a321 = 321;
        a321++;
        Console.WriteLine(a321);
        return 100;
    }
    public int RunTest322()
    {
        long a322 = 322;
        a322++;
        Console.WriteLine(a322);
        return 100;
    }
    public int RunTest323()
    {
        long a323 = 323;
        a323++;
        Console.WriteLine(a323);
        return 100;
    }
    public int RunTest324()
    {
        long a324 = 324;
        a324++;
        Console.WriteLine(a324);
        return 100;
    }
    public int RunTest325()
    {
        long a325 = 325;
        a325++;
        Console.WriteLine(a325);
        return 100;
    }
    public int RunTest326()
    {
        long a326 = 326;
        a326++;
        Console.WriteLine(a326);
        return 100;
    }
    public int RunTest327()
    {
        long a327 = 327;
        a327++;
        Console.WriteLine(a327);
        return 100;
    }
    public int RunTest328()
    {
        long a328 = 328;
        a328++;
        Console.WriteLine(a328);
        return 100;
    }
    public int RunTest329()
    {
        long a329 = 329;
        a329++;
        Console.WriteLine(a329);
        return 100;
    }
    public int RunTest330()
    {
        long a330 = 330;
        a330++;
        Console.WriteLine(a330);
        return 100;
    }
    public int RunTest331()
    {
        long a331 = 331;
        a331++;
        Console.WriteLine(a331);
        return 100;
    }
    public int RunTest332()
    {
        long a332 = 332;
        a332++;
        Console.WriteLine(a332);
        return 100;
    }
    public int RunTest333()
    {
        long a333 = 333;
        a333++;
        Console.WriteLine(a333);
        return 100;
    }
    public int RunTest334()
    {
        long a334 = 334;
        a334++;
        Console.WriteLine(a334);
        return 100;
    }
    public int RunTest335()
    {
        long a335 = 335;
        a335++;
        Console.WriteLine(a335);
        return 100;
    }
    public int RunTest336()
    {
        long a336 = 336;
        a336++;
        Console.WriteLine(a336);
        return 100;
    }
    public int RunTest337()
    {
        long a337 = 337;
        a337++;
        Console.WriteLine(a337);
        return 100;
    }
    public int RunTest338()
    {
        long a338 = 338;
        a338++;
        Console.WriteLine(a338);
        return 100;
    }
    public int RunTest339()
    {
        long a339 = 339;
        a339++;
        Console.WriteLine(a339);
        return 100;
    }
    public int RunTest340()
    {
        long a340 = 340;
        a340++;
        Console.WriteLine(a340);
        return 100;
    }
    public int RunTest341()
    {
        long a341 = 341;
        a341++;
        Console.WriteLine(a341);
        return 100;
    }
    public int RunTest342()
    {
        long a342 = 342;
        a342++;
        Console.WriteLine(a342);
        return 100;
    }
    public int RunTest343()
    {
        long a343 = 343;
        a343++;
        Console.WriteLine(a343);
        return 100;
    }
    public int RunTest344()
    {
        long a344 = 344;
        a344++;
        Console.WriteLine(a344);
        return 100;
    }
    public int RunTest345()
    {
        long a345 = 345;
        a345++;
        Console.WriteLine(a345);
        return 100;
    }
    public int RunTest346()
    {
        long a346 = 346;
        a346++;
        Console.WriteLine(a346);
        return 100;
    }
    public int RunTest347()
    {
        long a347 = 347;
        a347++;
        Console.WriteLine(a347);
        return 100;
    }
    public int RunTest348()
    {
        long a348 = 348;
        a348++;
        Console.WriteLine(a348);
        return 100;
    }
    public int RunTest349()
    {
        long a349 = 349;
        a349++;
        Console.WriteLine(a349);
        return 100;
    }
    public int RunTest350()
    {
        long a350 = 350;
        a350++;
        Console.WriteLine(a350);
        return 100;
    }
    public int RunTest351()
    {
        long a351 = 351;
        a351++;
        Console.WriteLine(a351);
        return 100;
    }
    public int RunTest352()
    {
        long a352 = 352;
        a352++;
        Console.WriteLine(a352);
        return 100;
    }
    public int RunTest353()
    {
        long a353 = 353;
        a353++;
        Console.WriteLine(a353);
        return 100;
    }
    public int RunTest354()
    {
        long a354 = 354;
        a354++;
        Console.WriteLine(a354);
        return 100;
    }
    public int RunTest355()
    {
        long a355 = 355;
        a355++;
        Console.WriteLine(a355);
        return 100;
    }
    public int RunTest356()
    {
        long a356 = 356;
        a356++;
        Console.WriteLine(a356);
        return 100;
    }
    public int RunTest357()
    {
        long a357 = 357;
        a357++;
        Console.WriteLine(a357);
        return 100;
    }
    public int RunTest358()
    {
        long a358 = 358;
        a358++;
        Console.WriteLine(a358);
        return 100;
    }
    public int RunTest359()
    {
        long a359 = 359;
        a359++;
        Console.WriteLine(a359);
        return 100;
    }
    public int RunTest360()
    {
        long a360 = 360;
        a360++;
        Console.WriteLine(a360);
        return 100;
    }
    public int RunTest361()
    {
        long a361 = 361;
        a361++;
        Console.WriteLine(a361);
        return 100;
    }
    public int RunTest362()
    {
        long a362 = 362;
        a362++;
        Console.WriteLine(a362);
        return 100;
    }
    public int RunTest363()
    {
        long a363 = 363;
        a363++;
        Console.WriteLine(a363);
        return 100;
    }
    public int RunTest364()
    {
        long a364 = 364;
        a364++;
        Console.WriteLine(a364);
        return 100;
    }
    public int RunTest365()
    {
        long a365 = 365;
        a365++;
        Console.WriteLine(a365);
        return 100;
    }
    public int RunTest366()
    {
        long a366 = 366;
        a366++;
        Console.WriteLine(a366);
        return 100;
    }
    public int RunTest367()
    {
        long a367 = 367;
        a367++;
        Console.WriteLine(a367);
        return 100;
    }
    public int RunTest368()
    {
        long a368 = 368;
        a368++;
        Console.WriteLine(a368);
        return 100;
    }
    public int RunTest369()
    {
        long a369 = 369;
        a369++;
        Console.WriteLine(a369);
        return 100;
    }
    public int RunTest370()
    {
        long a370 = 370;
        a370++;
        Console.WriteLine(a370);
        return 100;
    }
    public int RunTest371()
    {
        long a371 = 371;
        a371++;
        Console.WriteLine(a371);
        return 100;
    }
    public int RunTest372()
    {
        long a372 = 372;
        a372++;
        Console.WriteLine(a372);
        return 100;
    }
    public int RunTest373()
    {
        long a373 = 373;
        a373++;
        Console.WriteLine(a373);
        return 100;
    }
    public int RunTest374()
    {
        long a374 = 374;
        a374++;
        Console.WriteLine(a374);
        return 100;
    }
    public int RunTest375()
    {
        long a375 = 375;
        a375++;
        Console.WriteLine(a375);
        return 100;
    }
    public int RunTest376()
    {
        long a376 = 376;
        a376++;
        Console.WriteLine(a376);
        return 100;
    }
    public int RunTest377()
    {
        long a377 = 377;
        a377++;
        Console.WriteLine(a377);
        return 100;
    }
    public int RunTest378()
    {
        long a378 = 378;
        a378++;
        Console.WriteLine(a378);
        return 100;
    }
    public int RunTest379()
    {
        long a379 = 379;
        a379++;
        Console.WriteLine(a379);
        return 100;
    }
    public int RunTest380()
    {
        long a380 = 380;
        a380++;
        Console.WriteLine(a380);
        return 100;
    }
    public int RunTest381()
    {
        long a381 = 381;
        a381++;
        Console.WriteLine(a381);
        return 100;
    }
    public int RunTest382()
    {
        long a382 = 382;
        a382++;
        Console.WriteLine(a382);
        return 100;
    }
    public int RunTest383()
    {
        long a383 = 383;
        a383++;
        Console.WriteLine(a383);
        return 100;
    }
    public int RunTest384()
    {
        long a384 = 384;
        a384++;
        Console.WriteLine(a384);
        return 100;
    }
    public int RunTest385()
    {
        long a385 = 385;
        a385++;
        Console.WriteLine(a385);
        return 100;
    }
    public int RunTest386()
    {
        long a386 = 386;
        a386++;
        Console.WriteLine(a386);
        return 100;
    }
    public int RunTest387()
    {
        long a387 = 387;
        a387++;
        Console.WriteLine(a387);
        return 100;
    }
    public int RunTest388()
    {
        long a388 = 388;
        a388++;
        Console.WriteLine(a388);
        return 100;
    }
    public int RunTest389()
    {
        long a389 = 389;
        a389++;
        Console.WriteLine(a389);
        return 100;
    }
    public int RunTest390()
    {
        long a390 = 390;
        a390++;
        Console.WriteLine(a390);
        return 100;
    }
    public int RunTest391()
    {
        long a391 = 391;
        a391++;
        Console.WriteLine(a391);
        return 100;
    }
    public int RunTest392()
    {
        long a392 = 392;
        a392++;
        Console.WriteLine(a392);
        return 100;
    }
    public int RunTest393()
    {
        long a393 = 393;
        a393++;
        Console.WriteLine(a393);
        return 100;
    }
    public int RunTest394()
    {
        long a394 = 394;
        a394++;
        Console.WriteLine(a394);
        return 100;
    }
    public int RunTest395()
    {
        long a395 = 395;
        a395++;
        Console.WriteLine(a395);
        return 100;
    }
    public int RunTest396()
    {
        long a396 = 396;
        a396++;
        Console.WriteLine(a396);
        return 100;
    }
    public int RunTest397()
    {
        long a397 = 397;
        a397++;
        Console.WriteLine(a397);
        return 100;
    }
    public int RunTest398()
    {
        long a398 = 398;
        a398++;
        Console.WriteLine(a398);
        return 100;
    }
    public int RunTest399()
    {
        long a399 = 399;
        a399++;
        Console.WriteLine(a399);
        return 100;
    }
    public int RunTest400_NoInline()
    {
        long a400 = 400;
        a400++;
        Console.WriteLine(a400);
        return 100;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        (new Test_Inline_Vars()).RunTest0_Inline();

        (new Test_Inline_Vars()).RunTest1();

        (new Test_Inline_Vars()).RunTest2();

        (new Test_Inline_Vars()).RunTest3();

        (new Test_Inline_Vars()).RunTest4();

        (new Test_Inline_Vars()).RunTest5();

        (new Test_Inline_Vars()).RunTest6();

        (new Test_Inline_Vars()).RunTest7();

        (new Test_Inline_Vars()).RunTest8();

        (new Test_Inline_Vars()).RunTest9();

        (new Test_Inline_Vars()).RunTest10();

        (new Test_Inline_Vars()).RunTest11();

        (new Test_Inline_Vars()).RunTest12();

        (new Test_Inline_Vars()).RunTest13();

        (new Test_Inline_Vars()).RunTest14();

        (new Test_Inline_Vars()).RunTest15();

        (new Test_Inline_Vars()).RunTest16();

        (new Test_Inline_Vars()).RunTest17();

        (new Test_Inline_Vars()).RunTest18();

        (new Test_Inline_Vars()).RunTest19();

        (new Test_Inline_Vars()).RunTest20();

        (new Test_Inline_Vars()).RunTest21();

        (new Test_Inline_Vars()).RunTest22();

        (new Test_Inline_Vars()).RunTest23();

        (new Test_Inline_Vars()).RunTest24();

        (new Test_Inline_Vars()).RunTest25();

        (new Test_Inline_Vars()).RunTest26();

        (new Test_Inline_Vars()).RunTest27();

        (new Test_Inline_Vars()).RunTest28();

        (new Test_Inline_Vars()).RunTest29();

        (new Test_Inline_Vars()).RunTest30();

        (new Test_Inline_Vars()).RunTest31();

        (new Test_Inline_Vars()).RunTest32();

        (new Test_Inline_Vars()).RunTest33();

        (new Test_Inline_Vars()).RunTest34();

        (new Test_Inline_Vars()).RunTest35();

        (new Test_Inline_Vars()).RunTest36();

        (new Test_Inline_Vars()).RunTest37();

        (new Test_Inline_Vars()).RunTest38();

        (new Test_Inline_Vars()).RunTest39();

        (new Test_Inline_Vars()).RunTest40();

        (new Test_Inline_Vars()).RunTest41();

        (new Test_Inline_Vars()).RunTest42();

        (new Test_Inline_Vars()).RunTest43();

        (new Test_Inline_Vars()).RunTest44();

        (new Test_Inline_Vars()).RunTest45();

        (new Test_Inline_Vars()).RunTest46();

        (new Test_Inline_Vars()).RunTest47();

        (new Test_Inline_Vars()).RunTest48();

        (new Test_Inline_Vars()).RunTest49();

        (new Test_Inline_Vars()).RunTest50();

        (new Test_Inline_Vars()).RunTest51();

        (new Test_Inline_Vars()).RunTest52();

        (new Test_Inline_Vars()).RunTest53();

        (new Test_Inline_Vars()).RunTest54();

        (new Test_Inline_Vars()).RunTest55();

        (new Test_Inline_Vars()).RunTest56();

        (new Test_Inline_Vars()).RunTest57();

        (new Test_Inline_Vars()).RunTest58();

        (new Test_Inline_Vars()).RunTest59();

        (new Test_Inline_Vars()).RunTest60();

        (new Test_Inline_Vars()).RunTest61();

        (new Test_Inline_Vars()).RunTest62();

        (new Test_Inline_Vars()).RunTest63();

        (new Test_Inline_Vars()).RunTest64();

        (new Test_Inline_Vars()).RunTest65();

        (new Test_Inline_Vars()).RunTest66();

        (new Test_Inline_Vars()).RunTest67();

        (new Test_Inline_Vars()).RunTest68();

        (new Test_Inline_Vars()).RunTest69();

        (new Test_Inline_Vars()).RunTest70();

        (new Test_Inline_Vars()).RunTest71();

        (new Test_Inline_Vars()).RunTest72();

        (new Test_Inline_Vars()).RunTest73();

        (new Test_Inline_Vars()).RunTest74();

        (new Test_Inline_Vars()).RunTest75();

        (new Test_Inline_Vars()).RunTest76();

        (new Test_Inline_Vars()).RunTest77();

        (new Test_Inline_Vars()).RunTest78();

        (new Test_Inline_Vars()).RunTest79();

        (new Test_Inline_Vars()).RunTest80();

        (new Test_Inline_Vars()).RunTest81();

        (new Test_Inline_Vars()).RunTest82();

        (new Test_Inline_Vars()).RunTest83();

        (new Test_Inline_Vars()).RunTest84();

        (new Test_Inline_Vars()).RunTest85();

        (new Test_Inline_Vars()).RunTest86();

        (new Test_Inline_Vars()).RunTest87();

        (new Test_Inline_Vars()).RunTest88();

        (new Test_Inline_Vars()).RunTest89();

        (new Test_Inline_Vars()).RunTest90();

        (new Test_Inline_Vars()).RunTest91();

        (new Test_Inline_Vars()).RunTest92();

        (new Test_Inline_Vars()).RunTest93();

        (new Test_Inline_Vars()).RunTest94();

        (new Test_Inline_Vars()).RunTest95();

        (new Test_Inline_Vars()).RunTest96();

        (new Test_Inline_Vars()).RunTest97();

        (new Test_Inline_Vars()).RunTest98();

        (new Test_Inline_Vars()).RunTest99();

        (new Test_Inline_Vars()).RunTest100();

        (new Test_Inline_Vars()).RunTest101();

        (new Test_Inline_Vars()).RunTest102();

        (new Test_Inline_Vars()).RunTest103();

        (new Test_Inline_Vars()).RunTest104();

        (new Test_Inline_Vars()).RunTest105();

        (new Test_Inline_Vars()).RunTest106();

        (new Test_Inline_Vars()).RunTest107();

        (new Test_Inline_Vars()).RunTest108();

        (new Test_Inline_Vars()).RunTest109();

        (new Test_Inline_Vars()).RunTest110();

        (new Test_Inline_Vars()).RunTest111();

        (new Test_Inline_Vars()).RunTest112();

        (new Test_Inline_Vars()).RunTest113();

        (new Test_Inline_Vars()).RunTest114();

        (new Test_Inline_Vars()).RunTest115();

        (new Test_Inline_Vars()).RunTest116();

        (new Test_Inline_Vars()).RunTest117();

        (new Test_Inline_Vars()).RunTest118();

        (new Test_Inline_Vars()).RunTest119();

        (new Test_Inline_Vars()).RunTest120();

        (new Test_Inline_Vars()).RunTest121();

        (new Test_Inline_Vars()).RunTest122();

        (new Test_Inline_Vars()).RunTest123();

        (new Test_Inline_Vars()).RunTest124();

        (new Test_Inline_Vars()).RunTest125();

        (new Test_Inline_Vars()).RunTest126();

        (new Test_Inline_Vars()).RunTest127();

        (new Test_Inline_Vars()).RunTest128();

        (new Test_Inline_Vars()).RunTest129();

        (new Test_Inline_Vars()).RunTest130();

        (new Test_Inline_Vars()).RunTest131();

        (new Test_Inline_Vars()).RunTest132();

        (new Test_Inline_Vars()).RunTest133();

        (new Test_Inline_Vars()).RunTest134();

        (new Test_Inline_Vars()).RunTest135();

        (new Test_Inline_Vars()).RunTest136();

        (new Test_Inline_Vars()).RunTest137();

        (new Test_Inline_Vars()).RunTest138();

        (new Test_Inline_Vars()).RunTest139();

        (new Test_Inline_Vars()).RunTest140();

        (new Test_Inline_Vars()).RunTest141();

        (new Test_Inline_Vars()).RunTest142();

        (new Test_Inline_Vars()).RunTest143();

        (new Test_Inline_Vars()).RunTest144();

        (new Test_Inline_Vars()).RunTest145();

        (new Test_Inline_Vars()).RunTest146();

        (new Test_Inline_Vars()).RunTest147();

        (new Test_Inline_Vars()).RunTest148();

        (new Test_Inline_Vars()).RunTest149();

        (new Test_Inline_Vars()).RunTest150();

        (new Test_Inline_Vars()).RunTest151();

        (new Test_Inline_Vars()).RunTest152();

        (new Test_Inline_Vars()).RunTest153();

        (new Test_Inline_Vars()).RunTest154();

        (new Test_Inline_Vars()).RunTest155();

        (new Test_Inline_Vars()).RunTest156();

        (new Test_Inline_Vars()).RunTest157();

        (new Test_Inline_Vars()).RunTest158();

        (new Test_Inline_Vars()).RunTest159();

        (new Test_Inline_Vars()).RunTest160();

        (new Test_Inline_Vars()).RunTest161();

        (new Test_Inline_Vars()).RunTest162();

        (new Test_Inline_Vars()).RunTest163();

        (new Test_Inline_Vars()).RunTest164();

        (new Test_Inline_Vars()).RunTest165();

        (new Test_Inline_Vars()).RunTest166();

        (new Test_Inline_Vars()).RunTest167();

        (new Test_Inline_Vars()).RunTest168();

        (new Test_Inline_Vars()).RunTest169();

        (new Test_Inline_Vars()).RunTest170();

        (new Test_Inline_Vars()).RunTest171();

        (new Test_Inline_Vars()).RunTest172();

        (new Test_Inline_Vars()).RunTest173();

        (new Test_Inline_Vars()).RunTest174();

        (new Test_Inline_Vars()).RunTest175();

        (new Test_Inline_Vars()).RunTest176();

        (new Test_Inline_Vars()).RunTest177();

        (new Test_Inline_Vars()).RunTest178();

        (new Test_Inline_Vars()).RunTest179();

        (new Test_Inline_Vars()).RunTest180();

        (new Test_Inline_Vars()).RunTest181();

        (new Test_Inline_Vars()).RunTest182();

        (new Test_Inline_Vars()).RunTest183();

        (new Test_Inline_Vars()).RunTest184();

        (new Test_Inline_Vars()).RunTest185();

        (new Test_Inline_Vars()).RunTest186();

        (new Test_Inline_Vars()).RunTest187();

        (new Test_Inline_Vars()).RunTest188();

        (new Test_Inline_Vars()).RunTest189();

        (new Test_Inline_Vars()).RunTest190();

        (new Test_Inline_Vars()).RunTest191();

        (new Test_Inline_Vars()).RunTest192();

        (new Test_Inline_Vars()).RunTest193();

        (new Test_Inline_Vars()).RunTest194();

        (new Test_Inline_Vars()).RunTest195();

        (new Test_Inline_Vars()).RunTest196();

        (new Test_Inline_Vars()).RunTest197();

        (new Test_Inline_Vars()).RunTest198();

        (new Test_Inline_Vars()).RunTest199();

        (new Test_Inline_Vars()).RunTest200();

        (new Test_Inline_Vars()).RunTest201();

        (new Test_Inline_Vars()).RunTest202();

        (new Test_Inline_Vars()).RunTest203();

        (new Test_Inline_Vars()).RunTest204();

        (new Test_Inline_Vars()).RunTest205();

        (new Test_Inline_Vars()).RunTest206();

        (new Test_Inline_Vars()).RunTest207();

        (new Test_Inline_Vars()).RunTest208();

        (new Test_Inline_Vars()).RunTest209();

        (new Test_Inline_Vars()).RunTest210();

        (new Test_Inline_Vars()).RunTest211();

        (new Test_Inline_Vars()).RunTest212();

        (new Test_Inline_Vars()).RunTest213();

        (new Test_Inline_Vars()).RunTest214();

        (new Test_Inline_Vars()).RunTest215();

        (new Test_Inline_Vars()).RunTest216();

        (new Test_Inline_Vars()).RunTest217();

        (new Test_Inline_Vars()).RunTest218();

        (new Test_Inline_Vars()).RunTest219();

        (new Test_Inline_Vars()).RunTest220();

        (new Test_Inline_Vars()).RunTest221();

        (new Test_Inline_Vars()).RunTest222();

        (new Test_Inline_Vars()).RunTest223();

        (new Test_Inline_Vars()).RunTest224();

        (new Test_Inline_Vars()).RunTest225();

        (new Test_Inline_Vars()).RunTest226();

        (new Test_Inline_Vars()).RunTest227();

        (new Test_Inline_Vars()).RunTest228();

        (new Test_Inline_Vars()).RunTest229();

        (new Test_Inline_Vars()).RunTest230();

        (new Test_Inline_Vars()).RunTest231();

        (new Test_Inline_Vars()).RunTest232();

        (new Test_Inline_Vars()).RunTest233();

        (new Test_Inline_Vars()).RunTest234();

        (new Test_Inline_Vars()).RunTest235();

        (new Test_Inline_Vars()).RunTest236();

        (new Test_Inline_Vars()).RunTest237();

        (new Test_Inline_Vars()).RunTest238();

        (new Test_Inline_Vars()).RunTest239();

        (new Test_Inline_Vars()).RunTest240();

        (new Test_Inline_Vars()).RunTest241();

        (new Test_Inline_Vars()).RunTest242();

        (new Test_Inline_Vars()).RunTest243();

        (new Test_Inline_Vars()).RunTest244();

        (new Test_Inline_Vars()).RunTest245();

        (new Test_Inline_Vars()).RunTest246();

        (new Test_Inline_Vars()).RunTest247();

        (new Test_Inline_Vars()).RunTest248();

        (new Test_Inline_Vars()).RunTest249();

        (new Test_Inline_Vars()).RunTest250();

        (new Test_Inline_Vars()).RunTest251();

        (new Test_Inline_Vars()).RunTest252();

        (new Test_Inline_Vars()).RunTest253();

        (new Test_Inline_Vars()).RunTest254();

        (new Test_Inline_Vars()).RunTest255();

        (new Test_Inline_Vars()).RunTest256();

        (new Test_Inline_Vars()).RunTest257();

        (new Test_Inline_Vars()).RunTest258();

        (new Test_Inline_Vars()).RunTest259();

        (new Test_Inline_Vars()).RunTest260();

        (new Test_Inline_Vars()).RunTest261();

        (new Test_Inline_Vars()).RunTest262();

        (new Test_Inline_Vars()).RunTest263();

        (new Test_Inline_Vars()).RunTest264();

        (new Test_Inline_Vars()).RunTest265();

        (new Test_Inline_Vars()).RunTest266();

        (new Test_Inline_Vars()).RunTest267();

        (new Test_Inline_Vars()).RunTest268();

        (new Test_Inline_Vars()).RunTest269();

        (new Test_Inline_Vars()).RunTest270();

        (new Test_Inline_Vars()).RunTest271();

        (new Test_Inline_Vars()).RunTest272();

        (new Test_Inline_Vars()).RunTest273();

        (new Test_Inline_Vars()).RunTest274();

        (new Test_Inline_Vars()).RunTest275();

        (new Test_Inline_Vars()).RunTest276();

        (new Test_Inline_Vars()).RunTest277();

        (new Test_Inline_Vars()).RunTest278();

        (new Test_Inline_Vars()).RunTest279();

        (new Test_Inline_Vars()).RunTest280();

        (new Test_Inline_Vars()).RunTest281();

        (new Test_Inline_Vars()).RunTest282();

        (new Test_Inline_Vars()).RunTest283();

        (new Test_Inline_Vars()).RunTest284();

        (new Test_Inline_Vars()).RunTest285();

        (new Test_Inline_Vars()).RunTest286();

        (new Test_Inline_Vars()).RunTest287();

        (new Test_Inline_Vars()).RunTest288();

        (new Test_Inline_Vars()).RunTest289();

        (new Test_Inline_Vars()).RunTest290();

        (new Test_Inline_Vars()).RunTest291();

        (new Test_Inline_Vars()).RunTest292();

        (new Test_Inline_Vars()).RunTest293();

        (new Test_Inline_Vars()).RunTest294();

        (new Test_Inline_Vars()).RunTest295();

        (new Test_Inline_Vars()).RunTest296();

        (new Test_Inline_Vars()).RunTest297();

        (new Test_Inline_Vars()).RunTest298();

        (new Test_Inline_Vars()).RunTest299();

        (new Test_Inline_Vars()).RunTest300();

        (new Test_Inline_Vars()).RunTest301();

        (new Test_Inline_Vars()).RunTest302();

        (new Test_Inline_Vars()).RunTest303();

        (new Test_Inline_Vars()).RunTest304();

        (new Test_Inline_Vars()).RunTest305();

        (new Test_Inline_Vars()).RunTest306();

        (new Test_Inline_Vars()).RunTest307();

        (new Test_Inline_Vars()).RunTest308();

        (new Test_Inline_Vars()).RunTest309();

        (new Test_Inline_Vars()).RunTest310();

        (new Test_Inline_Vars()).RunTest311();

        (new Test_Inline_Vars()).RunTest312();

        (new Test_Inline_Vars()).RunTest313();

        (new Test_Inline_Vars()).RunTest314();

        (new Test_Inline_Vars()).RunTest315();

        (new Test_Inline_Vars()).RunTest316();

        (new Test_Inline_Vars()).RunTest317();

        (new Test_Inline_Vars()).RunTest318();

        (new Test_Inline_Vars()).RunTest319();

        (new Test_Inline_Vars()).RunTest320();

        (new Test_Inline_Vars()).RunTest321();

        (new Test_Inline_Vars()).RunTest322();

        (new Test_Inline_Vars()).RunTest323();

        (new Test_Inline_Vars()).RunTest324();

        (new Test_Inline_Vars()).RunTest325();

        (new Test_Inline_Vars()).RunTest326();

        (new Test_Inline_Vars()).RunTest327();

        (new Test_Inline_Vars()).RunTest328();

        (new Test_Inline_Vars()).RunTest329();

        (new Test_Inline_Vars()).RunTest330();

        (new Test_Inline_Vars()).RunTest331();

        (new Test_Inline_Vars()).RunTest332();

        (new Test_Inline_Vars()).RunTest333();

        (new Test_Inline_Vars()).RunTest334();

        (new Test_Inline_Vars()).RunTest335();

        (new Test_Inline_Vars()).RunTest336();

        (new Test_Inline_Vars()).RunTest337();

        (new Test_Inline_Vars()).RunTest338();

        (new Test_Inline_Vars()).RunTest339();

        (new Test_Inline_Vars()).RunTest340();

        (new Test_Inline_Vars()).RunTest341();

        (new Test_Inline_Vars()).RunTest342();

        (new Test_Inline_Vars()).RunTest343();

        (new Test_Inline_Vars()).RunTest344();

        (new Test_Inline_Vars()).RunTest345();

        (new Test_Inline_Vars()).RunTest346();

        (new Test_Inline_Vars()).RunTest347();

        (new Test_Inline_Vars()).RunTest348();

        (new Test_Inline_Vars()).RunTest349();

        (new Test_Inline_Vars()).RunTest350();

        (new Test_Inline_Vars()).RunTest351();

        (new Test_Inline_Vars()).RunTest352();

        (new Test_Inline_Vars()).RunTest353();

        (new Test_Inline_Vars()).RunTest354();

        (new Test_Inline_Vars()).RunTest355();

        (new Test_Inline_Vars()).RunTest356();

        (new Test_Inline_Vars()).RunTest357();

        (new Test_Inline_Vars()).RunTest358();

        (new Test_Inline_Vars()).RunTest359();

        (new Test_Inline_Vars()).RunTest360();

        (new Test_Inline_Vars()).RunTest361();

        (new Test_Inline_Vars()).RunTest362();

        (new Test_Inline_Vars()).RunTest363();

        (new Test_Inline_Vars()).RunTest364();

        (new Test_Inline_Vars()).RunTest365();

        (new Test_Inline_Vars()).RunTest366();

        (new Test_Inline_Vars()).RunTest367();

        (new Test_Inline_Vars()).RunTest368();

        (new Test_Inline_Vars()).RunTest369();

        (new Test_Inline_Vars()).RunTest370();

        (new Test_Inline_Vars()).RunTest371();

        (new Test_Inline_Vars()).RunTest372();

        (new Test_Inline_Vars()).RunTest373();

        (new Test_Inline_Vars()).RunTest374();

        (new Test_Inline_Vars()).RunTest375();

        (new Test_Inline_Vars()).RunTest376();

        (new Test_Inline_Vars()).RunTest377();

        (new Test_Inline_Vars()).RunTest378();

        (new Test_Inline_Vars()).RunTest379();

        (new Test_Inline_Vars()).RunTest380();

        (new Test_Inline_Vars()).RunTest381();

        (new Test_Inline_Vars()).RunTest382();

        (new Test_Inline_Vars()).RunTest383();

        (new Test_Inline_Vars()).RunTest384();

        (new Test_Inline_Vars()).RunTest385();

        (new Test_Inline_Vars()).RunTest386();

        (new Test_Inline_Vars()).RunTest387();

        (new Test_Inline_Vars()).RunTest388();

        (new Test_Inline_Vars()).RunTest389();

        (new Test_Inline_Vars()).RunTest390();

        (new Test_Inline_Vars()).RunTest391();

        (new Test_Inline_Vars()).RunTest392();

        (new Test_Inline_Vars()).RunTest393();

        (new Test_Inline_Vars()).RunTest394();

        (new Test_Inline_Vars()).RunTest395();

        (new Test_Inline_Vars()).RunTest396();

        (new Test_Inline_Vars()).RunTest397();

        (new Test_Inline_Vars()).RunTest398();

        (new Test_Inline_Vars()).RunTest399();

        (new Test_Inline_Vars()).RunTest400_NoInline();
        return 100;
    }
}


