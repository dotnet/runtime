// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;

// Test derived from dotnet/corefx src\System.Numerics.Vectors\src\System\Numerics\Matrix4x4.cs, op_Multiply().
// This was an ARM32-specific bug for addressing local variables as floats. ARM32 floating-point instructions
// have a different offset range than integer instructions. If the local variable itself is a struct, but the
// instruction generated is a float local field, then we were computing the offset as integer, but the actual
// instruction was float. In certain frame layouts, the range will be out of range in this case, but we will
// not have allocated a "reserved" register which is used for generating large offsets.
// 
// The key functions in the JIT that are related are Compiler::compRsvdRegCheck() and Compiler::lvaFrameAddress().

public class Test
{
    public struct BigStruct
    {
        public float float1;
        public float float2;
        public float float3;
        public float float4;
        public float float5;
        public float float6;
        public float float7;
        public float float8;
        public float float9;
        public float float10;
        public float float11;
        public float float12;
        public float float13;
        public float float14;
        public float float15;
        public float float16;
        public float float17;
        public float float18;
        public float float19;
        public float float20;
        public float float21;
        public float float22;
        public float float23;
        public float float24;
        public float float25;
        public float float26;
        public float float27;
        public float float28;
        public float float29;
        public float float30;
        public float float31;
        public float float32;
        public float float33;
        public float float34;
        public float float35;
        public float float36;
        public float float37;
        public float float38;
        public float float39;
        public float float40;
        public float float41;
        public float float42;
        public float float43;
        public float float44;
        public float float45;
        public float float46;
        public float float47;
        public float float48;
        public float float49;
        public float float50;
        public float float51;
        public float float52;
        public float float53;
        public float float54;
        public float float55;
        public float float56;
        public float float57;
        public float float58;
        public float float59;
        public float float60;
        public float float61;
        public float float62;
        public float float63;
        public float float64;
        public float float65;
        public float float66;
        public float float67;
        public float float68;
        public float float69;
        public float float70;
        public float float71;
        public float float72;
        public float float73;
        public float float74;
        public float float75;
        public float float76;
        public float float77;
        public float float78;
        public float float79;
        public float float80;
        public float float81;
        public float float82;
        public float float83;
        public float float84;
        public float float85;
        public float float86;
        public float float87;
        public float float88;
        public float float89;
        public float float90;
        public float float91;
        public float float92;
        public float float93;
        public float float94;
        public float float95;
        public float float96;
        public float float97;
        public float float98;
        public float float99;
        public float float100;
        public float float101;
        public float float102;
        public float float103;
        public float float104;
        public float float105;
        public float float106;
        public float float107;
        public float float108;
        public float float109;
        public float float110;
        public float float111;
        public float float112;
        public float float113;
        public float float114;
        public float float115;
        public float float116;
        public float float117;
        public float float118;
        public float float119;
        public float float120;
        public float float121;
        public float float122;
        public float float123;
        public float float124;
        public float float125;
        public float float126;
        public float float127;
        public float float128;
        public float float129;
        public float float130;
        public float float131;
        public float float132;
        public float float133;
        public float float134;
        public float float135;
        public float float136;
        public float float137;
        public float float138;
        public float float139;
        public float float140;
        public float float141;
        public float float142;
        public float float143;
        public float float144;
        public float float145;
        public float float146;
        public float float147;
        public float float148;
        public float float149;
        public float float150;
        public float float151;
        public float float152;
        public float float153;
        public float float154;
        public float float155;
        public float float156;
        public float float157;
        public float float158;
        public float float159;
        public float float160;
        public float float161;
        public float float162;
        public float float163;
        public float float164;
        public float float165;
        public float float166;
        public float float167;
        public float float168;
        public float float169;
        public float float170;
        public float float171;
        public float float172;
        public float float173;
        public float float174;
        public float float175;
        public float float176;
        public float float177;
        public float float178;
        public float float179;
        public float float180;
        public float float181;
        public float float182;
        public float float183;
        public float float184;
        public float float185;
        public float float186;
        public float float187;
        public float float188;
        public float float189;
        public float float190;
        public float float191;
        public float float192;
        public float float193;
        public float float194;
        public float float195;
        public float float196;
        public float float197;
        public float float198;
        public float float199;
        public float float200;
        public float float201;
        public float float202;
        public float float203;
        public float float204;
        public float float205;
        public float float206;
        public float float207;
        public float float208;
        public float float209;
        public float float210;
        public float float211;
        public float float212;
        public float float213;
        public float float214;
        public float float215;
        public float float216;
        public float float217;
        public float float218;
        public float float219;
        public float float220;
        public float float221;
        public float float222;
        public float float223;
        public float float224;
        public float float225;
        public float float226;
        public float float227;
        public float float228;
        public float float229;
        public float float230;
        public float float231;
        public float float232;
        public float float233;
        public float float234;
        public float float235;
        public float float236;
        public float float237;
        public float float238;
        public float float239;
        public float float240;
        public float float241;
        public float float242;
        public float float243;
        public float float244;
        public float float245;
        public float float246;
        public float float247;
        public float float248;
        public float float249;
        public float float250;
        public float float251;
        public float float252;
        public float float253;
        public float float254;
        public float float255;
    }

    public struct Matrix4x4
    {
        public float M11;
        public float M12;
        public float M13;
        public float M14;

        public float M21;
        public float M22;
        public float M23;
        public float M24;

        public float M31;
        public float M32;
        public float M33;
        public float M34;

        public float M41;
        public float M42;
        public float M43;
        public float M44;

        /// <summary>
        /// Constructs a Matrix4x4 from the given components.
        /// </summary>
        public Matrix4x4(float m11, float m12, float m13, float m14,
                         float m21, float m22, float m23, float m24,
                         float m31, float m32, float m33, float m34,
                         float m41, float m42, float m43, float m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = m14;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = m24;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = m34;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = m44;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are equal; False otherwise.</returns>
        public static bool Equals(Matrix4x4 value1, Matrix4x4 value2)
        {
            return (value1.M11 == value2.M11 && value1.M22 == value2.M22 && value1.M33 == value2.M33 && value1.M44 == value2.M44 && // Check diagonal element first for early out.
                    value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M14 == value2.M14 && value1.M21 == value2.M21 && 
                    value1.M23 == value2.M23 && value1.M24 == value2.M24 && value1.M31 == value2.M31 && value1.M32 == value2.M32 && 
                    value1.M34 == value2.M34 && value1.M41 == value2.M41 && value1.M42 == value2.M42 && value1.M43 == value2.M43);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AddHelper(ref BigStruct b)
        {
            b.float1 += 1.0F;
            b.float255 += 2.0F;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Matrix4x4 AddTest(Matrix4x4 value1, Matrix4x4 value2)
        {
            BigStruct b = new BigStruct();

            b.float1   = value1.M11 + value2.M11;
            b.float255 = value1.M12 + value2.M12;

            AddHelper(ref b);

            Matrix4x4 m;

            m = value1;
            m.M11 = b.float1 + b.float255;
            m.M12 = b.float1 - b.float255;

            return m;
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return string.Format("{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
                                 M11, M12, M13, M14,
                                 M21, M22, M23, M24,
                                 M31, M32, M33, M34,
                                 M41, M42, M43, M44);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Matrix4x4 m1 = new Matrix4x4(1.0F,2.0F,3.0F,4.0F,
                                         5.0F,6.0F,7.0F,8.0F,
                                         9.0F,10.0F,11.0F,12.0F,
                                         13.0F,14.0F,15.0F,16.0F);
            Matrix4x4 m2 = new Matrix4x4(13.0F,14.0F,15.0F,16.0F,
                                         9.0F,10.0F,11.0F,12.0F,
                                         5.0F,6.0F,7.0F,8.0F,
                                         1.0F,2.0F,3.0F,4.0F);

            Matrix4x4 m3 = AddTest(m1,m2);

            Matrix4x4 mresult = new Matrix4x4(33.0F,-3.0F,3.0F,4.0F,
                                              5.0F,6.0F,7.0F,8.0F,
                                              9.0F,10.0F,11.0F,12.0F,
                                              13.0F,14.0F,15.0F,16.0F);
            if (Equals(m3,mresult))
            {
                Console.WriteLine("PASS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAIL: matrices don't match");
                Console.WriteLine(" m3      = {0}", m3.ToString());
                Console.WriteLine(" mresult = {0}", mresult.ToString());
                return 1;
            }
        }

    }
}

