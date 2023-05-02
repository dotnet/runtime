// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
	public class TestClass
	{
		public struct S1
		{
			public float float_0;
		}
		public struct S2
		{
			public struct S2_D1_F1
			{
				public uint uint_1;
			}
		}
		static bool s_bool_2 = true;
		static byte s_byte_3 = 1;
		static char s_char_4 = 'M';
		static decimal s_decimal_5 = 2.0405405405405405405405405405m;
		static double s_double_6 = -1.8846153846153846;
		static short s_short_7 = -1;
		static int s_int_8 = -5;
		static long s_long_9 = -5;
		static sbyte s_sbyte_10 = 1;
		static float s_float_11 = -4.952381f;
		static string s_string_12 = "JZDP";
		static ushort s_ushort_13 = 1;
		static uint s_uint_14 = 5;
		static ulong s_ulong_15 = 2;
		static S1 s_s1_16 = new S1();
		static S2.S2_D1_F1 s_s2_s2_d1_f1_17 = new S2.S2_D1_F1();
		static S2 s_s2_18 = new S2();
		bool bool_19 = true;
		byte byte_20 = 0;
		char char_21 = 'W';
		decimal decimal_22 = 2.2307692307692307692307692308m;
		double double_23 = -1;
		short short_24 = 0;
		int int_25 = 0;
		long long_26 = 2;
		sbyte sbyte_27 = -2;
		float float_28 = 0.071428575f;
		string string_29 = "MNK";
		ushort ushort_30 = 2;
		uint uint_31 = 1;
		ulong ulong_32 = 31;
		S1 s1_33 = new S1();
		S2.S2_D1_F1 s2_s2_d1_f1_34 = new S2.S2_D1_F1();
		S2 s2_35 = new S2();
		static int s_loopInvariant = 3;
		private static List<string> toPrint = new List<string>();
		[MethodImpl(MethodImplOptions.NoInlining)]
		public bool LeafMethod0()
		{
			unchecked
			{
				return ((bool)(((ulong)(((ulong)(((int)(int_25 % ((int)((int_25) | 66)))) % ((int)((((int)(int_25 | s_int_8))) | 53)))) + ulong_32)) == ((ulong)(((ulong)(((ulong)(s_ulong_15 ^ s_ulong_15)) + ((ulong)(s_int_8 % ((int)((s_int_8) | 49)))))) * ((ulong)(((ulong)(ulong_32 * s_ulong_15)) | ulong_32))))));
			}
		}
		public byte LeafMethod1()
		{
			unchecked
			{
				return ((byte)(byte_20 - ((byte)(((int)(s_int_8 / ((int)((((int)(s_int_8 %= ((int)((-1) | 4))))) | 81)))) / ((int)((((int)(int_25 = ((int)(s_int_8 - 5))))) | 85))))));
			}
		}
		public char LeafMethod2()
		{
			unchecked
			{
				return s_char_4;
			}
		}
		public decimal LeafMethod3()
		{
			unchecked
			{
				return ((decimal)(((int)(((int)(((int)(s_int_8 &= s_int_8)) >> ((int)(int_25 >> s_int_8)))) ^ ((int)(int_25 / ((int)((((int)(1 * int_25))) | 83)))))) / ((int)((((int)(((int)(int_25 *= ((int)(int_25 % ((int)((int_25) | 1)))))) >> ((int)(((int)(s_int_8 / ((int)((s_int_8) | 18)))) & ((int)(31 & int_25))))))) | 20))));
			}
		}
		public double LeafMethod4()
		{
			unchecked
			{
				return ((double)(((double)(s_double_6 *= ((double)(((double)(s_int_8 % ((int)((s_int_8) | 4)))) * ((double)(s_double_6 += s_double_6)))))) + ((double)(((int)(s_int_8 <<= ((int)(s_int_8 | int_25)))) % ((int)((((int)(s_int_8 = -5))) | 16))))));
			}
		}
		public short LeafMethod5()
		{
			unchecked
			{
				return ((short)(s_short_7 + ((short)(short_24 |= ((short)(((short)(s_int_8 / ((int)((int_25) | 30)))) << ((int)(int_25 + 5))))))));
			}
		}
		public int LeafMethod6()
		{
			unchecked
			{
				return ((int)(int_25 <<= ((int)(((int)(((int)(s_int_8 * s_int_8)) << ((int)(int_25 - 31)))) & s_int_8))));
			}
		}
		public long LeafMethod7()
		{
			unchecked
			{
				return ((long)(((long)(s_long_9 += ((long)(((long)(s_long_9 >>= LeafMethod6())) >> s_int_8)))) - ((long)(long_26 = long_26))));
			}
		}
		public sbyte LeafMethod8()
		{
			unchecked
			{
				return ((sbyte)(s_sbyte_10 ^= ((sbyte)(s_sbyte_10 <<= ((int)(int_25 = ((int)(s_int_8 * int_25))))))));
			}
		}
		public float LeafMethod9()
		{
			unchecked
			{
				return ((float)(((float)(((float)(((float)(s_int_8 /= ((int)((LeafMethod6()) | 47)))) + ((float)(int_25 % ((int)((s_int_8) | 1)))))) * ((float)(s1_33.float_0 -= ((float)(int_25 / ((int)((int_25) | 15)))))))) - ((float)(s_int_8 /= ((int)((((int)(s_int_8 *= ((int)(s_int_8 <<= int_25))))) | 2))))));
			}
		}
		public ushort LeafMethod11()
		{
			unchecked
			{
				return ((ushort)(((int)(s_int_8 >> s_int_8)) / ((int)((((int)(s_int_8 /= ((int)((s_int_8) | 98))))) | 14))));
			}
		}
		public uint LeafMethod12()
		{
			unchecked
			{
				return ((uint)(((int)(int_25 | s_int_8)) % ((int)((((int)(int_25 &= ((int)(((int)(int_25 & int_25)) & int_25))))) | 27))));
			}
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		public ulong LeafMethod13()
		{
			unchecked
			{
				return ((ulong)(s_ulong_15 ^= ((ulong)(((ulong)(((ulong)(ulong_32 - 1)) & ((ulong)(ulong_32 + s_ulong_15)))) & ((ulong)(ulong_32 = ((ulong)(int_25 % ((int)((s_int_8) | 54))))))))));
			}
		}
		public S1 LeafMethod14()
		{
			unchecked
			{
				return s_s1_16;
			}
		}
		public S2.S2_D1_F1 LeafMethod15()
		{
			unchecked
			{
				return s2_s2_d1_f1_34;
			}
		}
		public S2 LeafMethod16()
		{
			unchecked
			{
				return s_s2_18;
			}
		}
		public ulong Method9(out S2 p_s2_258, out S2 p_s2_259, S2 p_s2_260, ref S2 p_s2_261, ref S1 p_s1_262, out S1 p_s1_263, S2 p_s2_264, float p_float_265, out S2.S2_D1_F1 p_s2_s2_d1_f1_266, ref S2.S2_D1_F1 p_s2_s2_d1_f1_267, byte p_byte_268, S1 p_s1_269, float p_float_270, ref S2.S2_D1_F1 p_s2_s2_d1_f1_271)
		{
			unchecked
			{
				bool bool_272 = true;
				byte byte_273 = 0;
				char char_274 = '6';
				decimal decimal_275 = 0.0240963855421686746987951807m;
				double double_276 = 0.03278688524590164;
				short short_277 = -2;
				int int_278 = -5;
				long long_279 = 5;
				sbyte sbyte_280 = 5;
				float float_281 = -1.949367f;
				string string_282 = "UJI";
				ushort ushort_283 = 31;
				uint uint_284 = 31;
				ulong ulong_285 = 2;
				S1 s1_286 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_287 = new S2.S2_D1_F1();
				S2 s2_288 = new S2();
				p_s1_263 = s1_33;
				p_s2_s2_d1_f1_266 = s2_s2_d1_f1_287;
				return ((ulong)(ulong_285 <<= ((int)(((int)(LeafMethod6() + ((int)(LeafMethod6() ^ LeafMethod6())))) ^ ((int)(((int)(int_25 - LeafMethod6())) | ((int)(0 * LeafMethod6()))))))));
			}
		}
		public uint Method11(ref S2 p_s2_316, ref int p_int_317, ref short p_short_318, ref decimal p_decimal_319, S1 p_s1_320, out bool p_bool_321, out S2 p_s2_322, S2 p_s2_323, S1 p_s1_324, out float p_float_325, out S2 p_s2_326, S2 p_s2_327)
		{
			unchecked
			{
				bool bool_328 = false;
				byte byte_329 = 2;
				char char_330 = 'S';
				decimal decimal_331 = -1.8571428571428571428571428571m;
				double double_332 = 1.0526315789473684;
				short short_333 = 31;
				int int_334 = -2;
				long long_335 = -1;
				sbyte sbyte_336 = -1;
				float float_337 = 0.2f;
				string string_338 = "";
				ushort ushort_339 = 0;
				uint uint_340 = 2;
				ulong ulong_341 = 1;
				S1 s1_342 = new S1();
				S1 s1_343 = s1_342;
				S2.S2_D1_F1 s2_s2_d1_f1_344 = new S2.S2_D1_F1();
				S2 s2_345 = new S2();
				S2 s2_346 = s2_345;
				p_bool_321 = LeafMethod0();
				p_float_325 = ((float)(((int)(((int)(s_int_8 = ((int)(s_int_8 / ((int)((s_int_8) | 70)))))) & LeafMethod6())) % ((int)((((int)(((int)(int_25 &= ((int)(LeafMethod6() + LeafMethod6())))) % ((int)((((int)(p_int_317 / ((int)((((int)(5 + LeafMethod6()))) | 40))))) | 99))))) | 35))));
				return ((uint)(s_s2_s2_d1_f1_17.uint_1 + ((uint)(LeafMethod6() % ((int)((((int)(((int)(-1 / ((int)((s_int_8) | 14)))) + ((int)(int_25 ^= int_25))))) | 11))))));
			}
		}
		public S1 Method23(out S1 p_s1_653, out S1 p_s1_654, ref int p_int_655, S2.S2_D1_F1 p_s2_s2_d1_f1_656, out S2 p_s2_657, ref S2.S2_D1_F1 p_s2_s2_d1_f1_658, float p_float_659)
		{
			unchecked
			{
				bool bool_660 = true;
				byte byte_661 = 2;
				char char_662 = 'Y';
				decimal decimal_663 = 1.0476190476190476190476190476m;
				double double_664 = -1.95;
				short short_665 = 31;
				int int_666 = 0;
				long long_667 = 31;
				sbyte sbyte_668 = 0;
				float float_669 = 31f;
				string string_670 = "3P5A58X";
				ushort ushort_671 = 5;
				uint uint_672 = 2;
				ulong ulong_673 = 2;
				S1 s1_674 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_675 = new S2.S2_D1_F1();
				S2 s2_676 = new S2();
				p_s1_653 = s1_674;
				p_s1_654 = s_s1_16;
				switch (((long)(((long)(s_long_9 *= ((long)(((int)(s_int_8 <<= int_25)) % ((int)((((int)(s_int_8 | s_int_8))) | 82)))))) ^ ((long)(((long)(int_25 /= ((int)((((int)(s_int_8 = LeafMethod6()))) | 38)))) + ((long)(long_26 + ((long)(-2 & LeafMethod7())))))))))
				{
					case -2147483648:
						{
							int __loopvar0 = s_loopInvariant;
							break;
						}
					case 31:
						{
							break;
						}
					default:
						{
							long_667 += ((long)(long_667 |= ((long)(long_667 & ((long)(((int)(LeafMethod6() % ((int)((p_int_655) | 24)))) % ((int)((int_666) | 2))))))));
							break;
						}
				}
				return s_s1_16;
			}
		}
		public S2 Method24(out S2.S2_D1_F1 p_s2_s2_d1_f1_677, ref S2 p_s2_678, ref S2 p_s2_679, out ushort p_ushort_680, ref S1 p_s1_681, int p_int_682, ref S1 p_s1_683, out int p_int_684, ref S1 p_s1_685, S2 p_s2_686, byte p_byte_687, S2.S2_D1_F1 p_s2_s2_d1_f1_688, decimal p_decimal_689, S2 p_s2_690, out S1 p_s1_691, S2.S2_D1_F1 p_s2_s2_d1_f1_692, int p_int_693)
		{
			unchecked
			{
				bool bool_694 = true;
				byte byte_695 = 5;
				char char_696 = '2';
				decimal decimal_697 = -0.9375m;
				double double_698 = -1;
				short short_699 = -1;
				int int_700 = -2;
				long long_701 = 5;
				sbyte sbyte_702 = 31;
				float float_703 = 2.0441177f;
				string string_704 = "B9GV08LWL";
				ushort ushort_705 = 1;
				uint uint_706 = 1;
				ulong ulong_707 = 2;
				S1 s1_708 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_709 = new S2.S2_D1_F1();
				S2 s2_710 = new S2();
				p_s2_s2_d1_f1_677 = s_s2_s2_d1_f1_17;
				p_ushort_680 = ((ushort)(((ushort)(((ushort)(((ushort)(p_int_682 % ((int)((int_25) | 9)))) * ((ushort)(ushort_30 ^ LeafMethod11())))) & ((ushort)(((ushort)(ushort_705 *= ushort_30)) * ((ushort)(0 + s_ushort_13)))))) * s_ushort_13));
				p_int_684 = p_int_693;
				p_s1_691 = p_s1_681;
				return s2_35;
			}
		}
		public int Method28(S2.S2_D1_F1 p_s2_s2_d1_f1_773, S2 p_s2_774, out S2 p_s2_775, ref S2 p_s2_776, long p_long_777, S2 p_s2_778, ref float p_float_779, out S2.S2_D1_F1 p_s2_s2_d1_f1_780, S1 p_s1_781, S2 p_s2_782)
		{
			unchecked
			{
				bool bool_783 = true;
				byte byte_784 = 0;
				char char_785 = '3';
				decimal decimal_786 = -0.9148936170212765957446808511m;
				double double_787 = 1.0217391304347827;
				short short_788 = 31;
				int int_789 = -5;
				long long_790 = -1;
				sbyte sbyte_791 = 1;
				float float_792 = 3.0416667f;
				string string_793 = "0IDXTC";
				ushort ushort_794 = 32767;
				uint uint_795 = 31;
				ulong ulong_796 = 1;
				S1 s1_797 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_798 = new S2.S2_D1_F1();
				S2 s2_799 = new S2();
				p_s2_s2_d1_f1_780 = s_s2_s2_d1_f1_17;
				int __loopvar0 = s_loopInvariant + 7;
				return int_25;
			}
		}
		public S2.S2_D1_F1 Method50(out long p_long_1402, out S2 p_s2_1403, out S2 p_s2_1404, ref S1 p_s1_1405, long p_long_1406, ref short p_short_1407, S2 p_s2_1408, S1 p_s1_1409, ref S1 p_s1_1410, ulong p_ulong_1411, S2 p_s2_1412, out S1 p_s1_1413, out byte p_byte_1414, long p_long_1415, out S2 p_s2_1416)
		{
			unchecked
			{
				bool bool_1417 = false;
				byte byte_1418 = 31;
				char char_1419 = 'Q';
				decimal decimal_1420 = 1m;
				double double_1421 = 0;
				short short_1422 = 5;
				int int_1423 = 0;
				long long_1424 = 3;
				sbyte sbyte_1425 = 1;
				float float_1426 = 2.030303f;
				string string_1427 = "";
				ushort ushort_1428 = 2;
				uint uint_1429 = 5;
				ulong ulong_1430 = 2;
				S1 s1_1431 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1432 = new S2.S2_D1_F1();
				S2 s2_1433 = new S2();
				p_long_1402 = ((long)(((long)(((int)(s_int_8 | ((int)(int_1423 >>= int_1423)))) % ((int)((((int)(int_1423 -= s_int_8))) | 15)))) + ((long)(s_long_9 *= ((long)(long_1424 + ((long)(s_long_9 << s_int_8))))))));
				p_s1_1413 = s1_1431;
				p_byte_1414 = ((byte)(((byte)(Method28(s_s2_s2_d1_f1_17, p_s2_1408, out s_s2_18, ref s_s2_18, LeafMethod7(), LeafMethod16(), ref s_float_11, out s2_s2_d1_f1_34, LeafMethod14(), s2_35) % ((int)((((int)(((int)(s_int_8 - int_25)) * ((int)(s_int_8 ^= LeafMethod6()))))) | 28)))) ^ ((byte)(byte_20 >>= ((int)(((int)(int_25 << s_int_8)) / ((int)((((int)(s_int_8 |= int_25))) | 33))))))));
				return s_s2_s2_d1_f1_17;
			}
		}
		public S2.S2_D1_F1 Method51(out bool p_bool_1434, out S2.S2_D1_F1 p_s2_s2_d1_f1_1435, S2 p_s2_1436, out S2.S2_D1_F1 p_s2_s2_d1_f1_1437, ref S2 p_s2_1438, S2 p_s2_1439, S2 p_s2_1440, double p_double_1441, ulong p_ulong_1442)
		{
			unchecked
			{
				bool bool_1443 = false;
				byte byte_1444 = 5;
				char char_1445 = 'Q';
				decimal decimal_1446 = 0.0375m;
				double double_1447 = 0.02564102564102564;
				short short_1448 = -2;
				int int_1449 = -2;
				long long_1450 = 2;
				sbyte sbyte_1451 = 3;
				float float_1452 = 5.0266666f;
				string string_1453 = "W7AWWK";
				ushort ushort_1454 = 1;
				uint uint_1455 = 5;
				ulong ulong_1456 = 2;
				S1 s1_1457 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1458 = new S2.S2_D1_F1();
				S2 s2_1459 = new S2();
				S2 s2_1460 = s2_1459;
				p_bool_1434 = ((bool)(s_bool_2 = LeafMethod0()));
				p_s2_s2_d1_f1_1435 = s_s2_s2_d1_f1_17;
				p_s2_s2_d1_f1_1437 = s2_s2_d1_f1_1458;
				return s2_s2_d1_f1_34;
			}
		}
		public S2 Method52(out S2 p_s2_1461, S2.S2_D1_F1 p_s2_s2_d1_f1_1462, sbyte p_sbyte_1463, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1464, ref S2 p_s2_1465, out uint p_uint_1466, int p_int_1467, sbyte p_sbyte_1468, out S1 p_s1_1469)
		{
			unchecked
			{
				bool bool_1470 = true;
				byte byte_1471 = 31;
				char char_1472 = 'H';
				decimal decimal_1473 = -1.9848484848484848484848484848m;
				double double_1474 = 31.12;
				short short_1475 = 5;
				int int_1476 = 1;
				long long_1477 = 31;
				sbyte sbyte_1478 = 31;
				float float_1479 = -0.9764706f;
				string string_1480 = "49HG";
				ushort ushort_1481 = 3;
				uint uint_1482 = 5;
				ulong ulong_1483 = 3;
				S1 s1_1484 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1485 = new S2.S2_D1_F1();
				S2 s2_1486 = new S2();
				p_uint_1466 = Method11(ref s2_35, ref int_25, ref short_1475, ref s_decimal_5, s_s1_16, out s_bool_2, out s_s2_18, s_s2_18, s1_33, out float_28, out s2_35, s2_35);
				p_s1_1469 = s1_33;
				return s2_35;
			}
		}
		public S2 Method53(S2 p_s2_1487, ref S1 p_s1_1488, ref S2 p_s2_1489, S2.S2_D1_F1 p_s2_s2_d1_f1_1490, S1 p_s1_1491, out double p_double_1492, S2 p_s2_1493, ref S1 p_s1_1494, out int p_int_1495, ref sbyte p_sbyte_1496, out S2.S2_D1_F1 p_s2_s2_d1_f1_1497, S2 p_s2_1498, out S1 p_s1_1499, uint p_uint_1500, ref sbyte p_sbyte_1501)
		{
			unchecked
			{
				bool bool_1502 = true;
				byte byte_1503 = 2;
				char char_1504 = '2';
				decimal decimal_1505 = -1.8928571428571428571428571429m;
				double double_1506 = 0.021739130434782608;
				short short_1507 = 0;
				int int_1508 = 5;
				long long_1509 = -5;
				sbyte sbyte_1510 = -1;
				float float_1511 = -4.909091f;
				string string_1512 = "";
				ushort ushort_1513 = 5;
				uint uint_1514 = 1;
				ulong ulong_1515 = 1;
				S1 s1_1516 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1517 = new S2.S2_D1_F1();
				S2 s2_1518 = new S2();
				p_double_1492 = ((double)(((double)(double_1506 + ((double)(LeafMethod4() + ((double)(31.055555555555557 - double_1506)))))) + ((double)(-1.7333333333333334 * ((double)(((double)(double_23 + double_23)) + ((double)(double_1506 + double_1506))))))));
				p_int_1495 = LeafMethod6();
				p_s2_s2_d1_f1_1497 = s2_s2_d1_f1_34;
				p_s1_1499 = s1_1516;
				return s2_35;
			}
		}
		public ulong Method54(out uint p_uint_1519, ushort p_ushort_1520, ref S1 p_s1_1521, S2 p_s2_1522, ref long p_long_1523, out S2.S2_D1_F1 p_s2_s2_d1_f1_1524)
		{
			unchecked
			{
				bool bool_1525 = true;
				byte byte_1526 = 31;
				char char_1527 = 'E';
				decimal decimal_1528 = -1.7894736842105263157894736842m;
				double double_1529 = 31;
				short short_1530 = 0;
				int int_1531 = 31;
				long long_1532 = 31;
				sbyte sbyte_1533 = 0;
				float float_1534 = 5.071429f;
				string string_1535 = "MAT2QAUSK";
				ushort ushort_1536 = 31;
				uint uint_1537 = 31;
				ulong ulong_1538 = 31;
				S1 s1_1539 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1540 = new S2.S2_D1_F1();
				S2 s2_1541 = new S2();
				p_uint_1519 = s_uint_14;
				p_s2_s2_d1_f1_1524 = s2_s2_d1_f1_1540;
				return ((ulong)(ulong_1538 >>= ((int)(int_25 >>= ((int)(((int)(int_1531 ^= LeafMethod6())) >> ((int)(int_25 << s_int_8))))))));
			}
		}
		public S1 Method55(ref S2.S2_D1_F1 p_s2_s2_d1_f1_1542, S1 p_s1_1543)
		{
			unchecked
			{
				bool bool_1544 = true;
				byte byte_1545 = 31;
				char char_1546 = 'M';
				decimal decimal_1547 = -0.9466666666666666666666666667m;
				double double_1548 = 3.061224489795918;
				short short_1549 = 0;
				int int_1550 = -1;
				long long_1551 = -5;
				sbyte sbyte_1552 = -2;
				float float_1553 = 31f;
				string string_1554 = "7H";
				ushort ushort_1555 = 5;
				uint uint_1556 = 0;
				ulong ulong_1557 = 0;
				S1 s1_1558 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1559 = new S2.S2_D1_F1();
				S2 s2_1560 = new S2();
				int __loopvar1 = s_loopInvariant + 6;
				return s1_33;
			}
		}
		public S2 Method56(ref S2 p_s2_1561)
		{
			unchecked
			{
				bool bool_1562 = true;
				byte byte_1563 = 0;
				char char_1564 = '0';
				decimal decimal_1565 = -5m;
				double double_1566 = -4.951807228915663;
				short short_1567 = 31;
				int int_1568 = 31;
				long long_1569 = 31;
				sbyte sbyte_1570 = 31;
				float float_1571 = 1.0655738f;
				string string_1572 = "ZZ9S71KDP";
				ushort ushort_1573 = 0;
				uint uint_1574 = 5;
				ulong ulong_1575 = 31;
				S1 s1_1576 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1577 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1578 = s2_s2_d1_f1_1577;
				S2 s2_1579 = new S2();
				return s2_1579;
			}
		}
		public S1 Method57(ref ushort p_ushort_1580, decimal p_decimal_1581, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1582, S2 p_s2_1583, S2 p_s2_1584, ref S1 p_s1_1585, sbyte p_sbyte_1586, S2 p_s2_1587, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1588, S2.S2_D1_F1 p_s2_s2_d1_f1_1589)
		{
			unchecked
			{
				bool bool_1590 = false;
				byte byte_1591 = 2;
				char char_1592 = '8';
				decimal decimal_1593 = -4.9852941176470588235294117647m;
				double double_1594 = -4.9879518072289155;
				short short_1595 = 0;
				int int_1596 = 5;
				long long_1597 = 31;
				sbyte sbyte_1598 = 5;
				float float_1599 = -1.967742f;
				string string_1600 = "ZZ7BEN";
				ushort ushort_1601 = 0;
				uint uint_1602 = 2;
				ulong ulong_1603 = 31;
				S1 s1_1604 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1605 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1606 = s2_s2_d1_f1_1605;
				S2 s2_1607 = new S2();
				return s_s1_16;
			}
		}
		public S2.S2_D1_F1 Method58(out ulong p_ulong_1608, S2 p_s2_1609, ref S2 p_s2_1610, out ushort p_ushort_1611, S2.S2_D1_F1 p_s2_s2_d1_f1_1612, ref S1 p_s1_1613, S2.S2_D1_F1 p_s2_s2_d1_f1_1614, S2 p_s2_1615, out S2.S2_D1_F1 p_s2_s2_d1_f1_1616, ref int p_int_1617, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1618, S2.S2_D1_F1 p_s2_s2_d1_f1_1619, S1 p_s1_1620, S1 p_s1_1621, out S2.S2_D1_F1 p_s2_s2_d1_f1_1622)
		{
			unchecked
			{
				bool bool_1623 = false;
				byte byte_1624 = 1;
				char char_1625 = 'Z';
				decimal decimal_1626 = 31.1m;
				double double_1627 = 0.03508771929824561;
				short short_1628 = -2;
				int int_1629 = -1;
				long long_1630 = 5;
				sbyte sbyte_1631 = -2;
				float float_1632 = -1.9655173f;
				string string_1633 = "FSQG52";
				ushort ushort_1634 = 0;
				uint uint_1635 = 5;
				ulong ulong_1636 = 31;
				S1 s1_1637 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1638 = new S2.S2_D1_F1();
				S2 s2_1639 = new S2();
				p_ulong_1608 = s_ulong_15;
				p_ushort_1611 = ((ushort)(((ushort)(ushort_30 = ushort_1634)) - ((ushort)(((ushort)(((ushort)(s_ushort_13 += 3)) * ((ushort)(ushort_30 & LeafMethod11())))) - ushort_1634))));
				p_s2_s2_d1_f1_1616 = s2_s2_d1_f1_34;
				p_s2_s2_d1_f1_1622 = s2_s2_d1_f1_34;
				return s2_s2_d1_f1_34;
			}
		}
		public double Method59(S1 p_s1_1640, S2 p_s2_1641, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1642, S1 p_s1_1643, S2 p_s2_1644)
		{
			unchecked
			{
				bool bool_1645 = false;
				byte byte_1646 = 2;
				char char_1647 = 'N';
				decimal decimal_1648 = 31m;
				double double_1649 = 0;
				short short_1650 = 3;
				int int_1651 = -5;
				long long_1652 = -2;
				sbyte sbyte_1653 = -2;
				float float_1654 = -0.8181818f;
				string string_1655 = "V2RF0";
				ushort ushort_1656 = 1;
				uint uint_1657 = 5;
				ulong ulong_1658 = 1;
				S1 s1_1659 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1660 = new S2.S2_D1_F1();
				S2 s2_1661 = new S2();
				return ((double)(int_1651 /= ((int)((s_int_8) | 73))));
			}
		}
		public short Method60(ref S2 p_s2_1662, ref ulong p_ulong_1663, double p_double_1664, S2.S2_D1_F1 p_s2_s2_d1_f1_1665, out S2 p_s2_1666, long p_long_1667, S2.S2_D1_F1 p_s2_s2_d1_f1_1668, ref S2 p_s2_1669, float p_float_1670, S1 p_s1_1671)
		{
			unchecked
			{
				bool bool_1672 = false;
				byte byte_1673 = 0;
				char char_1674 = 'F';
				decimal decimal_1675 = 0.0103092783505154639175257732m;
				double double_1676 = 0.017857142857142856;
				short short_1677 = 31;
				int int_1678 = 1;
				long long_1679 = -2;
				sbyte sbyte_1680 = 0;
				float float_1681 = -0.96907216f;
				string string_1682 = "O2ESG";
				ushort ushort_1683 = 1;
				uint uint_1684 = 31;
				ulong ulong_1685 = 5;
				S1 s1_1686 = new S1();
				S1 s1_1687 = s1_1686;
				S2.S2_D1_F1 s2_s2_d1_f1_1688 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1689 = s2_s2_d1_f1_1688;
				S2 s2_1690 = new S2();
				if (((bool)(((ushort)(((ushort)(((ushort)(ushort_30 &= LeafMethod11())) ^ ((ushort)(ushort_30 * ushort_1683)))) + ushort_1683)) >= ((ushort)(31 + ((ushort)(((ushort)(ushort_30 >>= s_int_8)) ^ ((ushort)(s_ushort_13 + ushort_30)))))))))
				{
					int __loopvar0 = s_loopInvariant + 11;
				}
				else
				{
				}
				return s_short_7;
			}
		}
		public S2 Method61(S2.S2_D1_F1 p_s2_s2_d1_f1_1692, long p_long_1693, ref uint p_uint_1694)
		{
			unchecked
			{
				bool bool_1695 = true;
				byte byte_1696 = 31;
				char char_1697 = 'U';
				decimal decimal_1698 = 1.0361445783132530120481927711m;
				double double_1699 = 31.011627906976745;
				short short_1700 = -1;
				int int_1701 = 31;
				long long_1702 = 31;
				sbyte sbyte_1703 = 0;
				float float_1704 = -4.9649124f;
				string string_1705 = "M4AKRCV";
				ushort ushort_1706 = 0;
				uint uint_1707 = 2;
				ulong ulong_1708 = 1;
				S1 s1_1709 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1710 = new S2.S2_D1_F1();
				S2 s2_1711 = new S2();
				return s_s2_18;
			}
		}
		public S2.S2_D1_F1 Method62(S2.S2_D1_F1 p_s2_s2_d1_f1_1713, ref S1 p_s1_1714, ref int p_int_1715)
		{
			unchecked
			{
				bool bool_1716 = false;
				byte byte_1717 = 2;
				char char_1718 = 'V';
				decimal decimal_1719 = 0.0454545454545454545454545455m;
				double double_1720 = 5.0701754385964914;
				short short_1721 = 1;
				int int_1722 = 5;
				long long_1723 = 0;
				sbyte sbyte_1724 = 0;
				float float_1725 = 31.095238f;
				string string_1726 = "IBD9E9SN9";
				ushort ushort_1727 = 2;
				uint uint_1728 = 2;
				ulong ulong_1729 = 2147483647;
				S1 s1_1730 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1731 = new S2.S2_D1_F1();
				S2 s2_1732 = new S2();
				return p_s2_s2_d1_f1_1713;
			}
		}
		public ushort Method63(out decimal p_decimal_1733, char p_char_1734, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1735, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1736, ulong p_ulong_1737, char p_char_1738, out sbyte p_sbyte_1739, S2.S2_D1_F1 p_s2_s2_d1_f1_1740, ref S2 p_s2_1741, S1 p_s1_1742, out ulong p_ulong_1743, ulong p_ulong_1744, out S2 p_s2_1745, out S2.S2_D1_F1 p_s2_s2_d1_f1_1746)
		{
			unchecked
			{
				bool bool_1747 = true;
				byte byte_1748 = 5;
				char char_1749 = 'J';
				decimal decimal_1750 = -0.8181818181818181818181818182m;
				double double_1751 = -5;
				short short_1752 = 31;
				int int_1753 = -5;
				long long_1754 = -2;
				sbyte sbyte_1755 = 5;
				float float_1756 = 1.2f;
				string string_1757 = "OYXN9BX";
				ushort ushort_1758 = 5;
				uint uint_1759 = 0;
				ulong ulong_1760 = 2147483648;
				S1 s1_1761 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1762 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1763 = s2_s2_d1_f1_1762;
				S2 s2_1764 = new S2();
				p_decimal_1733 = ((decimal)(decimal_22 = ((decimal)(decimal_1750 = ((decimal)(LeafMethod3() + ((decimal)(int_25 % ((int)((LeafMethod6()) | 77))))))))));
				p_sbyte_1739 = ((sbyte)(((sbyte)(s_sbyte_10 = ((sbyte)(((sbyte)(sbyte_1755 | sbyte_1755)) ^ ((sbyte)(sbyte_27 + LeafMethod8())))))) * ((sbyte)(int_25 %= ((int)((((int)(((int)(int_25 &= int_1753)) | int_1753))) | 78))))));
				p_ulong_1743 = ((ulong)(p_ulong_1744 & ((ulong)(((ulong)(ulong_32 >>= ((int)(int_1753 |= -2)))) | ((ulong)(ulong_32 &= ((ulong)(p_ulong_1737 * p_ulong_1744))))))));
				p_s2_s2_d1_f1_1746 = s2_s2_d1_f1_34;
				return ((ushort)(((ushort)(ushort_1758 >>= int_1753)) * ((ushort)(int_25 /= ((int)((LeafMethod6()) | 22))))));
			}
		}
		public S2 Method64(S1 p_s1_1765, out S1 p_s1_1766, S1 p_s1_1767, out S1 p_s1_1768, ref S2 p_s2_1769, S2 p_s2_1770, out S2.S2_D1_F1 p_s2_s2_d1_f1_1771, ref S2 p_s2_1772, ref ushort p_ushort_1773, ref S1 p_s1_1774, ref S1 p_s1_1775, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1776, ref S2 p_s2_1777, ref S1 p_s1_1778, out S1 p_s1_1779)
		{
			unchecked
			{
				bool bool_1780 = true;
				byte byte_1781 = 1;
				char char_1782 = 'V';
				decimal decimal_1783 = -2m;
				double double_1784 = 1.015625;
				short short_1785 = 0;
				int int_1786 = -1;
				long long_1787 = 0;
				sbyte sbyte_1788 = -5;
				float float_1789 = -1.9893616f;
				string string_1790 = "";
				ushort ushort_1791 = 2;
				uint uint_1792 = 0;
				ulong ulong_1793 = 2;
				S1 s1_1794 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1795 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1796 = s2_s2_d1_f1_1795;
				S2 s2_1797 = new S2();
				p_s1_1766 = s1_33;
				p_s1_1768 = p_s1_1778;
				p_s2_s2_d1_f1_1771 = s2_s2_d1_f1_34;
				p_s1_1779 = s_s1_16;
				return p_s2_1777;
			}
		}
		public S2 Method65(out S2.S2_D1_F1 p_s2_s2_d1_f1_1798, ref S1 p_s1_1799, ref short p_short_1800, out long p_long_1801, S2 p_s2_1802, S2.S2_D1_F1 p_s2_s2_d1_f1_1803)
		{
			unchecked
			{
				bool bool_1804 = true;
				byte byte_1805 = 31;
				char char_1806 = 'J';
				decimal decimal_1807 = 5.0625m;
				double double_1808 = 5.25;
				short short_1809 = -2;
				int int_1810 = 31;
				long long_1811 = 31;
				sbyte sbyte_1812 = -1;
				float float_1813 = -1f;
				string string_1814 = "ST";
				ushort ushort_1815 = 2;
				uint uint_1816 = 1;
				ulong ulong_1817 = 0;
				S1 s1_1818 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1819 = new S2.S2_D1_F1();
				S2 s2_1820 = new S2();
				p_s2_s2_d1_f1_1798 = p_s2_s2_d1_f1_1803;
				p_long_1801 = long_26;
				return s_s2_18;
			}
		}
		public S1 Method66(out double p_double_1821, ref S1 p_s1_1822, out S1 p_s1_1823, ref S2 p_s2_1824, uint p_uint_1825, ref S1 p_s1_1826)
		{
			unchecked
			{
				bool bool_1827 = true;
				byte byte_1828 = 2;
				char char_1829 = '8';
				decimal decimal_1830 = 31.022471910112359550561797753m;
				double double_1831 = -4.954545454545454;
				short short_1832 = -5;
				int int_1833 = -1;
				long long_1834 = 31;
				sbyte sbyte_1835 = 5;
				float float_1836 = -1.9818182f;
				string string_1837 = "U";
				ushort ushort_1838 = 32767;
				uint uint_1839 = 2;
				ulong ulong_1840 = 2147483648;
				S1 s1_1841 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1842 = new S2.S2_D1_F1();
				S2 s2_1843 = new S2();
				p_double_1821 = ((double)(((double)(((int)(s_int_8 += ((int)(s_int_8 %= ((int)((LeafMethod6()) | 62)))))) / ((int)((((int)(((int)(LeafMethod6() ^ LeafMethod6())) / ((int)((((int)(s_int_8 ^= LeafMethod6()))) | 32))))) | 50)))) * ((double)(LeafMethod4() * ((double)(s_double_6 = ((double)(LeafMethod4() * LeafMethod4()))))))));
				p_s1_1823 = s1_33;
				if (((bool)(((sbyte)(((sbyte)(LeafMethod8() * ((sbyte)(LeafMethod8() >> s_int_8)))) ^ ((sbyte)(((sbyte)(sbyte_27 + sbyte_1835)) + ((sbyte)(sbyte_1835 &= LeafMethod8())))))) <= ((sbyte)(s_sbyte_10 |= sbyte_1835)))))
				{
				}
				else
				{
					int __loopvar1 = s_loopInvariant + 3, __loopSecondaryVar1_0 = s_loopInvariant - 10;
				}
				return p_s1_1826;
			}
		}
		public long Method67(S1 p_s1_1844, out S1 p_s1_1845, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1846, ref int p_int_1847, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1848)
		{
			unchecked
			{
				bool bool_1849 = true;
				byte byte_1850 = 2;
				char char_1851 = 'A';
				decimal decimal_1852 = 31.090909090909090909090909091m;
				double double_1853 = 0;
				short short_1854 = -2;
				int int_1855 = 5;
				long long_1856 = 2;
				sbyte sbyte_1857 = 0;
				float float_1858 = -4.885714f;
				string string_1859 = "";
				ushort ushort_1860 = 2;
				uint uint_1861 = 2;
				ulong ulong_1862 = 2;
				S1 s1_1863 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1864 = new S2.S2_D1_F1();
				S2 s2_1865 = new S2();
				p_s1_1845 = s_s1_16;
				int __loopvar1 = s_loopInvariant;
				return ((long)(((long)(long_1856 -= ((long)(((long)(long_26 + LeafMethod7())) - long_26)))) << Method28(s_s2_s2_d1_f1_17, s2_35, out s2_1865, ref s2_35, LeafMethod7(), s2_1865, ref float_1858, out s_s2_s2_d1_f1_17, p_s1_1844, s2_35)));
			}
		}
		public S2.S2_D1_F1 Method68(out S2 p_s2_1866, long p_long_1867, ref S2 p_s2_1868, ref S1 p_s1_1869, S2 p_s2_1870, ref double p_double_1871)
		{
			unchecked
			{
				bool bool_1872 = false;
				byte byte_1873 = 1;
				char char_1874 = '3';
				decimal decimal_1875 = 5.0526315789473684210526315789m;
				double double_1876 = -4.911764705882353;
				short short_1877 = -2;
				int int_1878 = -1;
				long long_1879 = 31;
				sbyte sbyte_1880 = 2;
				float float_1881 = 5.022472f;
				string string_1882 = "H0Q";
				ushort ushort_1883 = 2;
				uint uint_1884 = 0;
				ulong ulong_1885 = 2;
				S1 s1_1886 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1887 = new S2.S2_D1_F1();
				S2 s2_1888 = new S2();
				return s_s2_s2_d1_f1_17;
			}
		}
		public sbyte Method69(out S1 p_s1_1889, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1890)
		{
			unchecked
			{
				bool bool_1891 = false;
				byte byte_1892 = 2;
				char char_1893 = 'Z';
				decimal decimal_1894 = -1.7777777777777777777777777778m;
				double double_1895 = 5;
				short short_1896 = 1;
				int int_1897 = -1;
				long long_1898 = 0;
				sbyte sbyte_1899 = 2;
				float float_1900 = -4.9428573f;
				string string_1901 = "ZJYXN2J";
				ushort ushort_1902 = 5;
				uint uint_1903 = 1;
				ulong ulong_1904 = 1;
				S1 s1_1905 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1906 = new S2.S2_D1_F1();
				S2 s2_1907 = new S2();
				S2 s2_1908 = s2_1907;
				p_s1_1889 = s_s1_16;
				return ((sbyte)(LeafMethod6() % ((int)((int_25) | 83))));
			}
		}
		public S2.S2_D1_F1 Method70(ref float p_float_1909, bool p_bool_1910, S1 p_s1_1911, out float p_float_1912, S1 p_s1_1913, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1914, ref decimal p_decimal_1915, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1916, long p_long_1917, S1 p_s1_1918, ref S2.S2_D1_F1 p_s2_s2_d1_f1_1919)
		{
			unchecked
			{
				bool bool_1920 = true;
				byte byte_1921 = 5;
				char char_1922 = '4';
				decimal decimal_1923 = 0.0576923076923076923076923077m;
				double double_1924 = 31.09090909090909;
				short short_1925 = -1;
				int int_1926 = 0;
				long long_1927 = -5;
				sbyte sbyte_1928 = -2;
				float float_1929 = 1f;
				string string_1930 = "H7U5MAUQG";
				ushort ushort_1931 = 5;
				uint uint_1932 = 2147483648;
				ulong ulong_1933 = 1;
				S1 s1_1934 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1935 = new S2.S2_D1_F1();
				S2 s2_1936 = new S2();
				p_float_1912 = ((float)(s1_1934.float_0 + ((float)(((int)(((int)(int_25 >>= int_1926)) / ((int)((((int)(int_25 /= ((int)((int_1926) | 19))))) | 86)))) % ((int)((Method28(s2_s2_d1_f1_34, s2_1936, out s2_35, ref s2_35, 31, LeafMethod16(), ref s_s1_16.float_0, out p_s2_s2_d1_f1_1916, s1_33, LeafMethod16())) | 77))))));
				return p_s2_s2_d1_f1_1916;
			}
		}
		public uint Method71(S1 p_s1_1937, out float p_float_1938, S2 p_s2_1939, out S1 p_s1_1940, out S1 p_s1_1941, out S2 p_s2_1942, ref S1 p_s1_1943, S1 p_s1_1944, ref double p_double_1945, ref short p_short_1946)
		{
			unchecked
			{
				bool bool_1947 = false;
				byte byte_1948 = 31;
				char char_1949 = '3';
				decimal decimal_1950 = -1.9647058823529411764705882353m;
				double double_1951 = -2;
				short short_1952 = 31;
				int int_1953 = -2;
				long long_1954 = 2;
				sbyte sbyte_1955 = -2;
				float float_1956 = -1f;
				string string_1957 = "JTIFXC";
				ushort ushort_1958 = 31;
				uint uint_1959 = 5;
				ulong ulong_1960 = 5;
				S1 s1_1961 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_1962 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_1963 = s2_s2_d1_f1_1962;
				S2 s2_1964 = new S2();
				p_float_1938 = ((float)(((int)(((int)(s_int_8 *= ((int)(LeafMethod6() << LeafMethod6())))) | ((int)(((int)(int_25 + int_25)) | ((int)(s_int_8 <<= LeafMethod6())))))) % ((int)((((int)(((int)(((int)(int_1953 <<= int_25)) - ((int)(s_int_8 ^= LeafMethod6())))) - ((int)(((int)(LeafMethod6() + int_25)) >> int_1953))))) | 81))));
				p_s1_1940 = s1_33;
				p_s1_1941 = s_s1_16;
				if (((bool)(((long)(((long)(-2 >> ((int)(s_int_8 ^= LeafMethod6())))) - ((long)(((long)(s_long_9 ^ LeafMethod7())) | ((long)(s_long_9 << int_25)))))) < ((long)(((long)(long_26 ^= long_26)) << ((int)(((int)(31 / ((int)((s_int_8) | 58)))) + ((int)(LeafMethod6() | LeafMethod6())))))))))
				{
					int __loopvar1 = s_loopInvariant - 6;
				}
				else
				{
				}
				return ((uint)(((uint)(((int)(((int)(int_25 -= int_25)) | ((int)(s_int_8 = LeafMethod6())))) % ((int)((((int)(int_1953 -= s_int_8))) | 28)))) - s_uint_14));
			}
		}
		public S2.S2_D1_F1 Method72(S2 p_s2_1965, S2.S2_D1_F1 p_s2_s2_d1_f1_1966, ref uint p_uint_1967, ref ulong p_ulong_1968, out S2.S2_D1_F1 p_s2_s2_d1_f1_1969, ref S1 p_s1_1970, S2.S2_D1_F1 p_s2_s2_d1_f1_1971, S2.S2_D1_F1 p_s2_s2_d1_f1_1972, out S1 p_s1_1973)
		{
			unchecked
			{
				bool bool_1974 = true;
				byte byte_1975 = 5;
				char char_1976 = 'F';
				decimal decimal_1977 = 31m;
				double double_1978 = 5.023255813953488;
				short short_1979 = -2;
				int int_1980 = 5;
				long long_1981 = 31;
				sbyte sbyte_1982 = 0;
				float float_1983 = -1.9666667f;
				string string_1984 = "BY0L";
				ushort ushort_1985 = 1;
				uint uint_1986 = 2;
				ulong ulong_1987 = 0;
				S1 s1_1988 = new S1();
				S1 s1_1989 = s1_1988;
				S2.S2_D1_F1 s2_s2_d1_f1_1990 = new S2.S2_D1_F1();
				S2 s2_1991 = new S2();
				S2 s2_1992 = s2_1991;
				p_s2_s2_d1_f1_1969 = s_s2_s2_d1_f1_17;
				p_s1_1973 = s_s1_16;
				int __loopvar0 = s_loopInvariant + 3;
				return s2_s2_d1_f1_1990;
			}
		}
		public S1 Method73(bool p_bool_1993, S1 p_s1_1994, ref S2 p_s2_1995, ref sbyte p_sbyte_1996, ref S1 p_s1_1997, ref S1 p_s1_1998, ref S1 p_s1_1999, out uint p_uint_2000, out S2 p_s2_2001)
		{
			unchecked
			{
				bool bool_2002 = true;
				byte byte_2003 = 0;
				char char_2004 = 'I';
				decimal decimal_2005 = -2147483648m;
				double double_2006 = 0.02040816326530612;
				short short_2007 = 0;
				int int_2008 = 31;
				long long_2009 = 0;
				sbyte sbyte_2010 = -2;
				float float_2011 = -0.95555556f;
				string string_2012 = "";
				ushort ushort_2013 = 5;
				uint uint_2014 = 1;
				ulong ulong_2015 = 5;
				S1 s1_2016 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2017 = new S2.S2_D1_F1();
				S2 s2_2018 = new S2();
				p_uint_2000 = Method11(ref s_s2_18, ref s_int_8, ref short_24, ref s_decimal_5, s1_33, out s_bool_2, out s2_35, s_s2_18, s_s1_16, out s1_33.float_0, out s2_35, s_s2_18);
				return s_s1_16;
			}
		}
		public S2 Method74(ref S1 p_s1_2019, float p_float_2020, out S2.S2_D1_F1 p_s2_s2_d1_f1_2021, S2.S2_D1_F1 p_s2_s2_d1_f1_2022, out ulong p_ulong_2023, S2 p_s2_2024)
		{
			unchecked
			{
				bool bool_2025 = false;
				byte byte_2026 = 1;
				char char_2027 = 'J';
				decimal decimal_2028 = -1m;
				double double_2029 = 0;
				short short_2030 = 0;
				int int_2031 = 0;
				long long_2032 = -2;
				sbyte sbyte_2033 = 0;
				float float_2034 = -4.875f;
				string string_2035 = "766CQ1KEI";
				ushort ushort_2036 = 0;
				uint uint_2037 = 31;
				ulong ulong_2038 = 2;
				S1 s1_2039 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2040 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2041 = s2_s2_d1_f1_2040;
				S2 s2_2042 = new S2();
				p_s2_s2_d1_f1_2021 = s_s2_s2_d1_f1_17;
				p_ulong_2023 = ((ulong)(s_int_8 % ((int)((LeafMethod6()) | 24))));
				return s_s2_18;
			}
		}
		public S2.S2_D1_F1 Method75(ref S2 p_s2_2043, ulong p_ulong_2044, out S1 p_s1_2045, ushort p_ushort_2046, S2 p_s2_2047, decimal p_decimal_2048, out S2 p_s2_2049, ref S2 p_s2_2050, S2 p_s2_2051, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2052, out S2 p_s2_2053, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2054)
		{
			unchecked
			{
				bool bool_2055 = false;
				byte byte_2056 = 0;
				char char_2057 = 'R';
				decimal decimal_2058 = 0.0422535211267605633802816901m;
				double double_2059 = 0.2222222222222222;
				short short_2060 = 31;
				int int_2061 = 1;
				long long_2062 = -2;
				sbyte sbyte_2063 = -2;
				float float_2064 = -2.1474836E+09f;
				string string_2065 = "BZ2DVK9U";
				ushort ushort_2066 = 31;
				uint uint_2067 = 5;
				ulong ulong_2068 = 3;
				S1 s1_2069 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2070 = new S2.S2_D1_F1();
				S2 s2_2071 = new S2();
				p_s1_2045 = s_s1_16;
				return s_s2_s2_d1_f1_17;
			}
		}
		public int Method76(out long p_long_2072, S2.S2_D1_F1 p_s2_s2_d1_f1_2073, float p_float_2074, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2075, out S1 p_s1_2076, S1 p_s1_2077, S1 p_s1_2078, out S2 p_s2_2079, S1 p_s1_2080, out S2.S2_D1_F1 p_s2_s2_d1_f1_2081, ref int p_int_2082, ref S1 p_s1_2083, ref uint p_uint_2084)
		{
			unchecked
			{
				bool bool_2085 = true;
				byte byte_2086 = 2;
				char char_2087 = 'R';
				decimal decimal_2088 = -0.9565217391304347826086956522m;
				double double_2089 = 5.012987012987013;
				short short_2090 = -1;
				int int_2091 = -1;
				long long_2092 = 5;
				sbyte sbyte_2093 = 0;
				float float_2094 = -0.962963f;
				string string_2095 = "A";
				ushort ushort_2096 = 1;
				uint uint_2097 = 0;
				ulong ulong_2098 = 1;
				S1 s1_2099 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2100 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2101 = s2_s2_d1_f1_2100;
				S2 s2_2102 = new S2();
				p_long_2072 = ((long)(((long)(long_2092 ^ ((long)(((long)(LeafMethod6() / ((int)((int_25) | 91)))) & ((long)(long_2092 & long_2092)))))) ^ ((long)(long_2092 <<= ((int)(((int)(31 / ((int)((LeafMethod6()) | 13)))) % ((int)((((int)(int_25 - s_int_8))) | 67))))))));
				p_s1_2076 = p_s1_2080;
				p_s2_s2_d1_f1_2081 = s2_s2_d1_f1_2101;
				return ((int)(int_25 &= ((int)(s_int_8 -= ((int)(((int)(LeafMethod6() ^ p_int_2082)) & ((int)(LeafMethod6() >> s_int_8))))))));
			}
		}
		public int Method77(S2 p_s2_2103, S1 p_s1_2104, S2 p_s2_2105, ref long p_long_2106, out long p_long_2107, ref S2 p_s2_2108, ref S1 p_s1_2109, S2.S2_D1_F1 p_s2_s2_d1_f1_2110, ref S2 p_s2_2111, ref uint p_uint_2112)
		{
			unchecked
			{
				bool bool_2113 = false;
				byte byte_2114 = 1;
				char char_2115 = 'Z';
				decimal decimal_2116 = 2.0106382978723404255319148936m;
				double double_2117 = -4.918918918918919;
				short short_2118 = -2;
				int int_2119 = -5;
				long long_2120 = 1;
				sbyte sbyte_2121 = -2;
				float float_2122 = 0.03846154f;
				string string_2123 = "DAY0N7W";
				ushort ushort_2124 = 31;
				uint uint_2125 = 31;
				ulong ulong_2126 = 5;
				S1 s1_2127 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2128 = new S2.S2_D1_F1();
				S2 s2_2129 = new S2();
				S2 s2_2130 = s2_2129;
				p_long_2107 = ((long)(((long)(long_26 <<= ((int)(int_25 ^= ((int)(LeafMethod6() | int_25)))))) & LeafMethod7()));
				return s_int_8;
			}
		}
		public S1 Method78(ref S2 p_s2_2131, S2.S2_D1_F1 p_s2_s2_d1_f1_2132, ulong p_ulong_2133, S2.S2_D1_F1 p_s2_s2_d1_f1_2134, int p_int_2135, out S2 p_s2_2136)
		{
			unchecked
			{
				bool bool_2137 = false;
				byte byte_2138 = 2;
				char char_2139 = 'T';
				decimal decimal_2140 = 31.25m;
				double double_2141 = 31;
				short short_2142 = 0;
				int int_2143 = -2;
				long long_2144 = 5;
				sbyte sbyte_2145 = 1;
				float float_2146 = -1.85f;
				string string_2147 = "2MHUBGG";
				ushort ushort_2148 = 5;
				uint uint_2149 = 5;
				ulong ulong_2150 = 2147483646;
				S1 s1_2151 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2152 = new S2.S2_D1_F1();
				S2 s2_2153 = new S2();
				return s_s1_16;
			}
		}
		public ulong Method79(ref char p_char_2154, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2155, ref S1 p_s1_2156, byte p_byte_2157, out S1 p_s1_2158, ref S2 p_s2_2159, ref sbyte p_sbyte_2160, int p_int_2161, S1 p_s1_2162, S2 p_s2_2163, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2164, out ulong p_ulong_2165, S1 p_s1_2166)
		{
			unchecked
			{
				bool bool_2167 = false;
				byte byte_2168 = 5;
				char char_2169 = '1';
				decimal decimal_2170 = -1m;
				double double_2171 = 5;
				short short_2172 = -2;
				int int_2173 = 5;
				long long_2174 = 2;
				sbyte sbyte_2175 = 1;
				float float_2176 = 31.033333f;
				string string_2177 = "PM";
				ushort ushort_2178 = 31;
				uint uint_2179 = 31;
				ulong ulong_2180 = 2;
				S1 s1_2181 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2182 = new S2.S2_D1_F1();
				S2 s2_2183 = new S2();
				p_s1_2158 = s1_33;
				p_ulong_2165 = ((ulong)(((int)(int_25 <<= ((int)(int_25 += ((int)(int_25 - int_25)))))) % ((int)((((int)(((int)(((int)(int_25 |= int_25)) >> s_int_8)) >> ((int)(((int)(s_int_8 << LeafMethod6())) | ((int)(int_25 - 5))))))) | 20))));
				return ((ulong)(((ulong)(Method9(out s_s2_18, out s2_35, s_s2_18, ref s2_2183, ref s_s1_16, out s1_33, s2_35, 5.0425534f, out s2_s2_d1_f1_2182, ref s2_s2_d1_f1_34, p_byte_2157, s_s1_16, s1_33.float_0, ref s_s2_s2_d1_f1_17) - ((ulong)(ulong_32 ^= LeafMethod13())))) ^ ((ulong)(((ulong)(((ulong)(1 - s_ulong_15)) ^ LeafMethod13())) * ((ulong)(ulong_32 -= ((ulong)(ulong_32 >>= LeafMethod6()))))))));
			}
		}
		public S2 Method80(ref long p_long_2184, S1 p_s1_2185, ref uint p_uint_2186, ref S2 p_s2_2187, S2 p_s2_2188, S2.S2_D1_F1 p_s2_s2_d1_f1_2189, ref S2 p_s2_2190, ref S2 p_s2_2191, out S2 p_s2_2192, out S2.S2_D1_F1 p_s2_s2_d1_f1_2193, ushort p_ushort_2194, ref S1 p_s1_2195)
		{
			unchecked
			{
				bool bool_2196 = true;
				byte byte_2197 = 1;
				char char_2198 = 'O';
				decimal decimal_2199 = -1.75m;
				double double_2200 = 0;
				short short_2201 = -2;
				int int_2202 = -2;
				long long_2203 = 31;
				sbyte sbyte_2204 = 5;
				float float_2205 = 2.0232558f;
				string string_2206 = "U";
				ushort ushort_2207 = 1;
				uint uint_2208 = 31;
				ulong ulong_2209 = 1;
				S1 s1_2210 = new S1();
				S1 s1_2211 = s1_2210;
				S2.S2_D1_F1 s2_s2_d1_f1_2212 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2213 = s2_s2_d1_f1_2212;
				S2 s2_2214 = new S2();
				p_s2_s2_d1_f1_2193 = s2_s2_d1_f1_2212;
				return s2_35;
			}
		}
		public S2 Method81(ref uint p_uint_2215, out S1 p_s1_2216, S2.S2_D1_F1 p_s2_s2_d1_f1_2217, S1 p_s1_2218, S1 p_s1_2219, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2220, S2 p_s2_2221, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2222, out S1 p_s1_2223, out S2 p_s2_2224, S2.S2_D1_F1 p_s2_s2_d1_f1_2225, out S2.S2_D1_F1 p_s2_s2_d1_f1_2226, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2227, ref S1 p_s1_2228)
		{
			unchecked
			{
				bool bool_2229 = true;
				byte byte_2230 = 3;
				char char_2231 = '6';
				decimal decimal_2232 = 31m;
				double double_2233 = -4.9714285714285715;
				short short_2234 = 1;
				int int_2235 = 2147483646;
				long long_2236 = -5;
				sbyte sbyte_2237 = 1;
				float float_2238 = 31.03125f;
				string string_2239 = "N";
				ushort ushort_2240 = 5;
				uint uint_2241 = 5;
				ulong ulong_2242 = 31;
				S1 s1_2243 = new S1();
				S1 s1_2244 = s1_2243;
				S2.S2_D1_F1 s2_s2_d1_f1_2245 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2246 = s2_s2_d1_f1_2245;
				S2 s2_2247 = new S2();
				p_s1_2223 = s1_33;
				p_s2_s2_d1_f1_2226 = p_s2_s2_d1_f1_2227;
				Method24(out s2_s2_d1_f1_34, ref s_s2_18, ref s_s2_18, out s_ushort_13, ref p_s1_2223, ((int)(((int)(((int)(int_25 - s_int_8)) + ((int)(s_int_8 | LeafMethod6())))) + ((int)(LeafMethod6() | ((int)(LeafMethod6() - int_2235)))))), ref s_s1_16, out int_2235, ref s_s1_16, p_s2_2221, ((byte)(byte_20 + ((byte)(((byte)(s_int_8 %= ((int)((s_int_8) | 67)))) * ((byte)(LeafMethod1() ^ 2)))))), p_s2_s2_d1_f1_2225, s_decimal_5, s2_35, out p_s1_2216, s_s2_s2_d1_f1_17, int_2235);
				return s_s2_18;
			}
		}
		public float Method82(ref long p_long_2248, out S1 p_s1_2249, S1 p_s1_2250, ushort p_ushort_2251, out S2 p_s2_2252, out S1 p_s1_2253, S2 p_s2_2254, ref decimal p_decimal_2255, ref S2 p_s2_2256, ref float p_float_2257, ref sbyte p_sbyte_2258, out S1 p_s1_2259, S2.S2_D1_F1 p_s2_s2_d1_f1_2260)
		{
			unchecked
			{
				bool bool_2261 = false;
				byte byte_2262 = 2;
				char char_2263 = '5';
				decimal decimal_2264 = 2.05m;
				double double_2265 = -1.9589041095890412;
				short short_2266 = -1;
				int int_2267 = 0;
				long long_2268 = 5;
				sbyte sbyte_2269 = 31;
				float float_2270 = -1.9090909f;
				string string_2271 = "9FG";
				ushort ushort_2272 = 0;
				uint uint_2273 = 31;
				ulong ulong_2274 = 31;
				S1 s1_2275 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2276 = new S2.S2_D1_F1();
				S2 s2_2277 = new S2();
				S2 s2_2278 = s2_2277;
				p_s1_2249 = p_s1_2250;
				p_s1_2253 = s1_33;
				p_s1_2259 = s_s1_16;
				return s1_2275.float_0;
			}
		}
		public S2 Method83(out S1 p_s1_2279)
		{
			unchecked
			{
				bool bool_2280 = true;
				byte byte_2281 = 31;
				char char_2282 = '7';
				decimal decimal_2283 = -2m;
				double double_2284 = 5.037037037037037;
				short short_2285 = 5;
				int int_2286 = 31;
				long long_2287 = 5;
				sbyte sbyte_2288 = 31;
				float float_2289 = -0.96511626f;
				string string_2290 = "2";
				ushort ushort_2291 = 1;
				uint uint_2292 = 1;
				ulong ulong_2293 = 1;
				S1 s1_2294 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2295 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2296 = s2_s2_d1_f1_2295;
				S2 s2_2297 = new S2();
				p_s1_2279 = s_s1_16;
				int __loopvar0 = s_loopInvariant;
				return s2_2297;
			}
		}
		public S1 Method84(S2 p_s2_2298, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2299, ref S2 p_s2_2300, long p_long_2301, uint p_uint_2302, short p_short_2303, ref float p_float_2304, ref S1 p_s1_2305, out S2 p_s2_2306, ref S2 p_s2_2307, ref sbyte p_sbyte_2308, out S1 p_s1_2309)
		{
			unchecked
			{
				bool bool_2310 = false;
				byte byte_2311 = 0;
				char char_2312 = 'X';
				decimal decimal_2313 = 5.1m;
				double double_2314 = 3.05;
				short short_2315 = -1;
				int int_2316 = -2;
				long long_2317 = -2;
				sbyte sbyte_2318 = 31;
				float float_2319 = 5.0588236f;
				string string_2320 = "NXLQCVYJ";
				ushort ushort_2321 = 31;
				uint uint_2322 = 1;
				ulong ulong_2323 = 31;
				S1 s1_2324 = new S1();
				S1 s1_2325 = s1_2324;
				S2.S2_D1_F1 s2_s2_d1_f1_2326 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2327 = s2_s2_d1_f1_2326;
				S2 s2_2328 = new S2();
				p_s1_2309 = s1_33;
				return s1_2324;
			}
		}
		public short Method85(ref S2 p_s2_2329, S2.S2_D1_F1 p_s2_s2_d1_f1_2330, S1 p_s1_2331, ref double p_double_2332, out S2 p_s2_2333, S2 p_s2_2334, ref float p_float_2335, S2 p_s2_2336, ref float p_float_2337, S1 p_s1_2338)
		{
			unchecked
			{
				bool bool_2339 = false;
				byte byte_2340 = 31;
				char char_2341 = 'B';
				decimal decimal_2342 = -1m;
				double double_2343 = -0.9565217391304348;
				short short_2344 = -2;
				int int_2345 = 2;
				long long_2346 = 2;
				sbyte sbyte_2347 = -1;
				float float_2348 = -0.959596f;
				string string_2349 = "F";
				ushort ushort_2350 = 5;
				uint uint_2351 = 31;
				ulong ulong_2352 = 2;
				S1 s1_2353 = new S1();
				S1 s1_2354 = s1_2353;
				S2.S2_D1_F1 s2_s2_d1_f1_2355 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2356 = s2_s2_d1_f1_2355;
				S2 s2_2357 = new S2();
				return ((short)(((short)(s_short_7 = ((short)(short_2344 >>= LeafMethod6())))) * ((short)(s_short_7 |= ((short)(short_24 >>= ((int)(s_int_8 = -1))))))));
			}
		}
		public ulong Method86(double p_double_2359)
		{
			unchecked
			{
				bool bool_2360 = false;
				byte byte_2361 = 31;
				char char_2362 = 'O';
				decimal decimal_2363 = 0.0202020202020202020202020202m;
				double double_2364 = 1.051948051948052;
				short short_2365 = -2;
				int int_2366 = 0;
				long long_2367 = -5;
				sbyte sbyte_2368 = 31;
				float float_2369 = -5f;
				string string_2370 = "H7SO";
				ushort ushort_2371 = 2;
				uint uint_2372 = 5;
				ulong ulong_2373 = 2;
				S1 s1_2374 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2375 = new S2.S2_D1_F1();
				S2 s2_2376 = new S2();
				if (((bool)(((ulong)(((ulong)(((ulong)(ulong_32 -= LeafMethod13())) - ((ulong)(ulong_2373 * ulong_2373)))) ^ ((ulong)(((ulong)(int_2366 % ((int)((LeafMethod6()) | 87)))) & s_ulong_15)))) < s_ulong_15)))
				{
					int __loopvar0 = s_loopInvariant - 7, __loopSecondaryVar0_0 = s_loopInvariant;
				}
				else
				{
					int __loopvar1 = s_loopInvariant - 13, __loopSecondaryVar1_0 = s_loopInvariant;
				}
				int __loopvar2 = s_loopInvariant - 9, __loopSecondaryVar2_0 = s_loopInvariant;
				return ((ulong)(ulong_2373 ^= ((ulong)(ulong_2373 ^= ((ulong)(((ulong)(int_25 / ((int)((int_25) | 40)))) - ((ulong)(s_int_8 / ((int)((s_int_8) | 70))))))))));
			}
		}
		public S2.S2_D1_F1 Method87(S2 p_s2_2377, ref long p_long_2378, out S2 p_s2_2379, byte p_byte_2380, out long p_long_2381, S2 p_s2_2382, S2 p_s2_2383, S1 p_s1_2384)
		{
			unchecked
			{
				bool bool_2385 = false;
				byte byte_2386 = 5;
				char char_2387 = 'W';
				decimal decimal_2388 = 2.1m;
				double double_2389 = -4.979166666666667;
				short short_2390 = 1;
				int int_2391 = 31;
				long long_2392 = -2;
				sbyte sbyte_2393 = 5;
				float float_2394 = 1f;
				string string_2395 = "S9JJJWXEJ";
				ushort ushort_2396 = 2;
				uint uint_2397 = 2;
				ulong ulong_2398 = 0;
				S1 s1_2399 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2400 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2401 = s2_s2_d1_f1_2400;
				S2 s2_2402 = new S2();
				S2 s2_2403 = s2_2402;
				p_long_2381 = ((long)(((int)(((int)(s_int_8 <<= int_2391)) % ((int)((((int)(int_25 % ((int)((((int)(int_25 += int_2391))) | 27))))) | 64)))) / ((int)((((int)(int_2391 ^ ((int)(((int)(s_int_8 - s_int_8)) ^ ((int)(s_int_8 ^= LeafMethod6()))))))) | 10))));
				return s2_s2_d1_f1_2401;
			}
		}
		public decimal Method88(out string p_string_2404, ref S2 p_s2_2405, S1 p_s1_2406, ref S1 p_s1_2407, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2408, out S2.S2_D1_F1 p_s2_s2_d1_f1_2409, out float p_float_2410, ref ulong p_ulong_2411, float p_float_2412, out S2.S2_D1_F1 p_s2_s2_d1_f1_2413, ref S2 p_s2_2414, S2 p_s2_2415, out S1 p_s1_2416, ref S2 p_s2_2417, S2 p_s2_2418, S1 p_s1_2419, S1 p_s1_2420, out S2.S2_D1_F1 p_s2_s2_d1_f1_2421)
		{
			unchecked
			{
				bool bool_2422 = false;
				byte byte_2423 = 0;
				char char_2424 = 'U';
				decimal decimal_2425 = -1.9880952380952380952380952381m;
				double double_2426 = -1.9473684210526316;
				short short_2427 = -2;
				int int_2428 = 31;
				long long_2429 = -2;
				sbyte sbyte_2430 = 31;
				float float_2431 = -0.9705882f;
				string string_2432 = "ICD";
				ushort ushort_2433 = 5;
				uint uint_2434 = 2;
				ulong ulong_2435 = 1;
				S1 s1_2436 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2437 = new S2.S2_D1_F1();
				S2 s2_2438 = new S2();
				p_string_2404 = string_29;
				p_s2_s2_d1_f1_2409 = p_s2_s2_d1_f1_2408;
				p_float_2410 = ((float)(p_float_2412 = ((float)(Method77(LeafMethod16(), LeafMethod14(), s2_35, ref long_26, out long_26, ref s_s2_18, ref p_s1_2406, LeafMethod15(), ref p_s2_2405, ref s2_s2_d1_f1_34.uint_1) / ((int)((((int)(LeafMethod6() - int_2428))) | 92))))));
				p_s2_s2_d1_f1_2413 = s_s2_s2_d1_f1_17;
				p_s1_2416 = p_s1_2407;
				p_s2_s2_d1_f1_2421 = s_s2_s2_d1_f1_17;
				int __loopvar0 = s_loopInvariant + 9;
				return ((decimal)(decimal_2425 += decimal_2425));
			}
		}
		public S2 Method89(out S2.S2_D1_F1 p_s2_s2_d1_f1_2439, out S2 p_s2_2440, out S1 p_s1_2441, ref S1 p_s1_2442, S1 p_s1_2443)
		{
			unchecked
			{
				bool bool_2444 = true;
				byte byte_2445 = 1;
				char char_2446 = '9';
				decimal decimal_2447 = -4.9550561797752808988764044944m;
				double double_2448 = -5;
				short short_2449 = -2;
				int int_2450 = 31;
				long long_2451 = -2;
				sbyte sbyte_2452 = -2;
				float float_2453 = 5.016667f;
				string string_2454 = "BCZMSU19";
				ushort ushort_2455 = 2;
				uint uint_2456 = 5;
				ulong ulong_2457 = 31;
				S1 s1_2458 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2459 = new S2.S2_D1_F1();
				S2 s2_2460 = new S2();
				p_s2_s2_d1_f1_2439 = s2_s2_d1_f1_34;
				p_s1_2441 = p_s1_2443;
				return s2_2460;
			}
		}
		public S2 Method90(out byte p_byte_2461, S2.S2_D1_F1 p_s2_s2_d1_f1_2462, ref S1 p_s1_2463, uint p_uint_2464)
		{
			unchecked
			{
				bool bool_2465 = false;
				byte byte_2466 = 5;
				char char_2467 = 'K';
				decimal decimal_2468 = 31.157894736842105263157894737m;
				double double_2469 = 0.014492753623188406;
				short short_2470 = 5;
				int int_2471 = 1;
				long long_2472 = -2;
				sbyte sbyte_2473 = 5;
				float float_2474 = 0f;
				string string_2475 = "GS3UP5Y6";
				ushort ushort_2476 = 5;
				uint uint_2477 = 0;
				ulong ulong_2478 = 31;
				S1 s1_2479 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2480 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2481 = s2_s2_d1_f1_2480;
				S2 s2_2482 = new S2();
				p_byte_2461 = ((byte)(((int)(s_int_8 -= ((int)(int_25 & ((int)(int_25 >>= LeafMethod6())))))) % ((int)((s_int_8) | 77))));
				return s2_35;
			}
		}
		public S2 Method91(out int p_int_2483, S1 p_s1_2484, S1 p_s1_2485, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2486, ref S1 p_s1_2487, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2488, S1 p_s1_2489, out S2.S2_D1_F1 p_s2_s2_d1_f1_2490, out S2 p_s2_2491, out S2.S2_D1_F1 p_s2_s2_d1_f1_2492, int p_int_2493, S2.S2_D1_F1 p_s2_s2_d1_f1_2494, S1 p_s1_2495)
		{
			unchecked
			{
				bool bool_2496 = true;
				byte byte_2497 = 1;
				char char_2498 = 'H';
				decimal decimal_2499 = 1.0365853658536585365853658537m;
				double double_2500 = 2147483647.0625;
				short short_2501 = -1;
				int int_2502 = 31;
				long long_2503 = 31;
				sbyte sbyte_2504 = 5;
				float float_2505 = 0f;
				string string_2506 = "9GV4WHGC";
				ushort ushort_2507 = 2;
				uint uint_2508 = 2;
				ulong ulong_2509 = 1;
				S1 s1_2510 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2511 = new S2.S2_D1_F1();
				S2 s2_2512 = new S2();
				S2 s2_2513 = s2_2512;
				p_int_2483 = s_int_8;
				p_s2_s2_d1_f1_2490 = s2_s2_d1_f1_34;
				p_s2_s2_d1_f1_2492 = p_s2_s2_d1_f1_2488;
				return s2_2513;
			}
		}
		public S1 Method92(S1 p_s1_2514, S2.S2_D1_F1 p_s2_s2_d1_f1_2515, S1 p_s1_2516, ref S1 p_s1_2517, ref S2 p_s2_2518, S1 p_s1_2519, S1 p_s1_2520, S2.S2_D1_F1 p_s2_s2_d1_f1_2521, S2.S2_D1_F1 p_s2_s2_d1_f1_2522, S2.S2_D1_F1 p_s2_s2_d1_f1_2523, S1 p_s1_2524, ref long p_long_2525, out ushort p_ushort_2526, S2.S2_D1_F1 p_s2_s2_d1_f1_2527, ref ushort p_ushort_2528, ref sbyte p_sbyte_2529, sbyte p_sbyte_2530, byte p_byte_2531, out S2.S2_D1_F1 p_s2_s2_d1_f1_2532)
		{
			unchecked
			{
				bool bool_2533 = true;
				byte byte_2534 = 2;
				char char_2535 = 'Q';
				decimal decimal_2536 = 31.051724137931034482758620690m;
				double double_2537 = -1.9534883720930232;
				short short_2538 = 0;
				int int_2539 = -2;
				long long_2540 = 31;
				sbyte sbyte_2541 = 0;
				float float_2542 = 0.010309278f;
				string string_2543 = "CTHTCEQ";
				ushort ushort_2544 = 2;
				uint uint_2545 = 31;
				ulong ulong_2546 = 2;
				S1 s1_2547 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2548 = new S2.S2_D1_F1();
				S2 s2_2549 = new S2();
				p_ushort_2526 = ((ushort)(((ushort)(((ushort)(ushort_30 + ((ushort)(LeafMethod11() - LeafMethod11())))) * s_ushort_13)) | ((ushort)(ushort_30 *= ((ushort)(ushort_30 = ((ushort)(s_ushort_13 - s_ushort_13))))))));
				p_s2_s2_d1_f1_2532 = s_s2_s2_d1_f1_17;
				return s1_33;
			}
		}
		public S2.S2_D1_F1 Method93(S2.S2_D1_F1 p_s2_s2_d1_f1_2550, out S2.S2_D1_F1 p_s2_s2_d1_f1_2551, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2552, S2 p_s2_2553, ref long p_long_2554, S2 p_s2_2555, out S1 p_s1_2556, out S1 p_s1_2557, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2558, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2559, out uint p_uint_2560, out S2.S2_D1_F1 p_s2_s2_d1_f1_2561, S2 p_s2_2562, out S2 p_s2_2563, float p_float_2564, S2 p_s2_2565, out S1 p_s1_2566)
		{
			unchecked
			{
				bool bool_2567 = false;
				byte byte_2568 = 2;
				char char_2569 = 'L';
				decimal decimal_2570 = 5m;
				double double_2571 = -2;
				short short_2572 = 5;
				int int_2573 = -5;
				long long_2574 = -2;
				sbyte sbyte_2575 = -1;
				float float_2576 = -5f;
				string string_2577 = "CZSNRK";
				ushort ushort_2578 = 0;
				uint uint_2579 = 5;
				ulong ulong_2580 = 1;
				S1 s1_2581 = new S1();
				S1 s1_2582 = s1_2581;
				S2.S2_D1_F1 s2_s2_d1_f1_2583 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2584 = s2_s2_d1_f1_2583;
				S2 s2_2585 = new S2();
				p_s2_s2_d1_f1_2551 = p_s2_s2_d1_f1_2558;
				p_s1_2556 = s_s1_16;
				p_s1_2557 = s_s1_16;
				p_uint_2560 = ((uint)(((uint)(s_s2_s2_d1_f1_17.uint_1 = ((uint)(s2_s2_d1_f1_34.uint_1 * ((uint)(s2_s2_d1_f1_34.uint_1 ^ s2_s2_d1_f1_2583.uint_1)))))) | s2_s2_d1_f1_2584.uint_1));
				p_s2_s2_d1_f1_2561 = s_s2_s2_d1_f1_17;
				p_s1_2566 = s_s1_16;
				return s2_s2_d1_f1_2583;
			}
		}
		public S2.S2_D1_F1 Method94(ref S1 p_s1_2586, out ushort p_ushort_2587, S2.S2_D1_F1 p_s2_s2_d1_f1_2588, S1 p_s1_2589, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2590, S2.S2_D1_F1 p_s2_s2_d1_f1_2591, out S1 p_s1_2592, float p_float_2593, ref S1 p_s1_2594, S2.S2_D1_F1 p_s2_s2_d1_f1_2595, S2 p_s2_2596, ref S1 p_s1_2597, ushort p_ushort_2598, ref S1 p_s1_2599, decimal p_decimal_2600, ref ulong p_ulong_2601, out S2.S2_D1_F1 p_s2_s2_d1_f1_2602)
		{
			unchecked
			{
				bool bool_2603 = false;
				byte byte_2604 = 5;
				char char_2605 = '1';
				decimal decimal_2606 = 5.0476190476190476190476190476m;
				double double_2607 = -1.9591836734693877;
				short short_2608 = 1;
				int int_2609 = 31;
				long long_2610 = -1;
				sbyte sbyte_2611 = -2;
				float float_2612 = 5.081081f;
				string string_2613 = "C";
				ushort ushort_2614 = 0;
				uint uint_2615 = 2;
				ulong ulong_2616 = 1;
				S1 s1_2617 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2618 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2619 = s2_s2_d1_f1_2618;
				S2 s2_2620 = new S2();
				p_ushort_2587 = ((ushort)(s_ushort_13 |= ((ushort)(((ushort)(((ushort)(int_25 % ((int)((s_int_8) | 73)))) + ((ushort)(31 + p_ushort_2598)))) ^ ((ushort)(ushort_2614 &= ushort_30))))));
				p_s1_2592 = s1_33;
				p_s2_s2_d1_f1_2602 = s_s2_s2_d1_f1_17;
				return s_s2_s2_d1_f1_17;
			}
		}
		public S1 Method95(ref S2 p_s2_2621, out S1 p_s1_2622, S1 p_s1_2623, S1 p_s1_2624, out S1 p_s1_2625, double p_double_2626, float p_float_2627, S1 p_s1_2628, out S1 p_s1_2629, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2630, out S2 p_s2_2631)
		{
			unchecked
			{
				bool bool_2632 = false;
				byte byte_2633 = 31;
				char char_2634 = 'D';
				decimal decimal_2635 = -1m;
				double double_2636 = -4.9879518072289155;
				short short_2637 = 0;
				int int_2638 = 1;
				long long_2639 = 1;
				sbyte sbyte_2640 = -1;
				float float_2641 = -4.967742f;
				string string_2642 = "0VSE";
				ushort ushort_2643 = 2;
				uint uint_2644 = 2;
				ulong ulong_2645 = 5;
				S1 s1_2646 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2647 = new S2.S2_D1_F1();
				S2.S2_D1_F1 s2_s2_d1_f1_2648 = s2_s2_d1_f1_2647;
				S2 s2_2649 = new S2();
				p_s1_2622 = s_s1_16;
				p_s1_2625 = p_s1_2623;
				p_s1_2629 = p_s1_2624;
				return p_s1_2628;
			}
		}
		public byte Method96(out S2 p_s2_2650, ulong p_ulong_2651, out ulong p_ulong_2652)
		{
			unchecked
			{
				bool bool_2653 = true;
				byte byte_2654 = 3;
				char char_2655 = 'C';
				decimal decimal_2656 = -4.8666666666666666666666666667m;
				double double_2657 = -1.9183673469387754;
				short short_2658 = 5;
				int int_2659 = 31;
				long long_2660 = -5;
				sbyte sbyte_2661 = -5;
				float float_2662 = -1.9411764f;
				string string_2663 = "IJVEOQB";
				ushort ushort_2664 = 5;
				uint uint_2665 = 2;
				ulong ulong_2666 = 0;
				S1 s1_2667 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2668 = new S2.S2_D1_F1();
				S2 s2_2669 = new S2();
				p_ulong_2652 = ((ulong)(p_ulong_2651 & ((ulong)(((ulong)(((ulong)(LeafMethod6() % ((int)((int_2659) | 1)))) + ((ulong)(ulong_2666 <<= int_2659)))) | ((ulong)(((ulong)(ulong_32 + LeafMethod13())) | ulong_32))))));
				if (bool_19)
				{
				}
				else
				{
					int __loopvar0 = s_loopInvariant, __loopSecondaryVar0_0 = s_loopInvariant;
				}
				return ((byte)(byte_2654 = ((byte)(((byte)(byte_2654 *= ((byte)(s_byte_3 |= s_byte_3)))) ^ ((byte)(s_byte_3 = ((byte)(s_byte_3 |= LeafMethod1()))))))));
			}
		}
		public S2.S2_D1_F1 Method97(S1 p_s1_2670, S2.S2_D1_F1 p_s2_s2_d1_f1_2671, ref float p_float_2672, out S1 p_s1_2673, ref int p_int_2674, S2.S2_D1_F1 p_s2_s2_d1_f1_2675, ref bool p_bool_2676, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2677, out S2.S2_D1_F1 p_s2_s2_d1_f1_2678, S2.S2_D1_F1 p_s2_s2_d1_f1_2679, long p_long_2680, out short p_short_2681, ref S1 p_s1_2682, uint p_uint_2683)
		{
			unchecked
			{
				bool bool_2684 = true;
				byte byte_2685 = 127;
				char char_2686 = 'X';
				decimal decimal_2687 = -2m;
				double double_2688 = -4.956521739130435;
				short short_2689 = -2;
				int int_2690 = 31;
				long long_2691 = 5;
				sbyte sbyte_2692 = 31;
				float float_2693 = 1f;
				string string_2694 = "9MVU6KJ";
				ushort ushort_2695 = 31;
				uint uint_2696 = 1;
				ulong ulong_2697 = 2;
				S1 s1_2698 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2699 = new S2.S2_D1_F1();
				S2 s2_2700 = new S2();
				p_s1_2673 = p_s1_2670;
				p_s2_s2_d1_f1_2678 = s_s2_s2_d1_f1_17;
				p_short_2681 = LeafMethod5();
				if (((bool)(bool_2684 = ((bool)(((uint)(((uint)(s_uint_14 |= uint_31)) * LeafMethod12())) <= ((uint)(p_uint_2683 - ((uint)(s2_s2_d1_f1_34.uint_1 & uint_31)))))))))
				{
					if (LeafMethod0())
					{
					}
					else
					{
						int __loopvar0 = s_loopInvariant - 14, __loopSecondaryVar0_0 = s_loopInvariant + 11;
					}
				}
				else
				{
				}
				return p_s2_s2_d1_f1_2679;
			}
		}
		public S1 Method98(S2.S2_D1_F1 p_s2_s2_d1_f1_2701, float p_float_2702, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2703, S2 p_s2_2704, ref S2.S2_D1_F1 p_s2_s2_d1_f1_2705, S2.S2_D1_F1 p_s2_s2_d1_f1_2706, out S1 p_s1_2707, ref S2 p_s2_2708, ref int p_int_2709, ref S2 p_s2_2710, S2.S2_D1_F1 p_s2_s2_d1_f1_2711, ref S2 p_s2_2712, ref S2 p_s2_2713, decimal p_decimal_2714, S2.S2_D1_F1 p_s2_s2_d1_f1_2715, ref ushort p_ushort_2716)
		{
			unchecked
			{
				bool bool_2717 = false;
				byte byte_2718 = 3;
				char char_2719 = '9';
				decimal decimal_2720 = 3.0444444444444444444444444444m;
				double double_2721 = -1;
				short short_2722 = -2;
				int int_2723 = -2;
				long long_2724 = 0;
				sbyte sbyte_2725 = -1;
				float float_2726 = -1f;
				string string_2727 = "FQAZHL";
				ushort ushort_2728 = 2;
				uint uint_2729 = 0;
				ulong ulong_2730 = 0;
				S1 s1_2731 = new S1();
				S1 s1_2732 = s1_2731;
				S2.S2_D1_F1 s2_s2_d1_f1_2733 = new S2.S2_D1_F1();
				S2 s2_2734 = new S2();
				p_s1_2707 = s1_2732;
				if (((bool)(((float)(((float)(s1_2731.float_0 = ((float)(s_float_11 + float_28)))) + ((float)(((int)(int_25 + p_int_2709)) % ((int)((((int)(int_25 % ((int)((s_int_8) | 51))))) | 35)))))) != ((float)(((float)(((float)(s_s1_16.float_0 = s1_33.float_0)) * ((float)(p_float_2702 -= LeafMethod9())))) + ((float)(((float)(float_2726 + p_float_2702)) + ((float)(LeafMethod9() + LeafMethod9())))))))))
				{
					int __loopvar0 = s_loopInvariant - 6;
					int __loopSecondaryVar2_0 = s_loopInvariant;
				}
				else
				{
					int __loopvar4 = s_loopInvariant - 12;
				}
				if (((bool)(((byte)(((byte)(byte_20 + ((byte)(int_2723 % ((int)((int_25) | 87)))))) + ((byte)(p_int_2709 /= ((int)((int_25) | 39)))))) == s_byte_3)))
				{
				}
				else
				{
					int __loopvar5 = s_loopInvariant;
				}
				return s_s1_16;
			}
		}
		public S2 Method99(S1 p_s1_2735, float p_float_2736, short p_short_2737, S2.S2_D1_F1 p_s2_s2_d1_f1_2738, S2 p_s2_2739, ref S2 p_s2_2740, out S2 p_s2_2741, S1 p_s1_2742, decimal p_decimal_2743, ref uint p_uint_2744, S2 p_s2_2745, out int p_int_2746, uint p_uint_2747, ref decimal p_decimal_2748, out int p_int_2749, S2 p_s2_2750, S1 p_s1_2751)
		{
			unchecked
			{
				bool bool_2752 = false;
				byte byte_2753 = 5;
				char char_2754 = 'D';
				decimal decimal_2755 = 2.0625m;
				double double_2756 = -1.962962962962963;
				short short_2757 = 5;
				int int_2758 = 2;
				long long_2759 = -2;
				sbyte sbyte_2760 = -5;
				float float_2761 = -4.96f;
				string string_2762 = "3";
				ushort ushort_2763 = 2;
				uint uint_2764 = 0;
				ulong ulong_2765 = 2;
				S1 s1_2766 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2767 = new S2.S2_D1_F1();
				S2 s2_2768 = new S2();
				p_int_2746 = ((int)(((int)(int_2758 = ((int)(int_25 >>= ((int)(int_25 >> s_int_8)))))) ^ ((int)(((int)(((int)(int_2758 *= LeafMethod6())) | ((int)(int_2758 = int_25)))) + s_int_8))));
				p_int_2749 = Method28(p_s2_s2_d1_f1_2738, p_s2_2739, out s2_35, ref s2_35, long_2759, s_s2_18, ref s_float_11, out s_s2_s2_d1_f1_17, s_s1_16, p_s2_2741);
				return s2_35;
			}
		}
		public void Method0()
		{
			unchecked
			{
				bool bool_2769 = true;
				byte byte_2770 = 127;
				char char_2771 = 'Y';
				decimal decimal_2772 = -0.88m;
				double double_2773 = -1.9310344827586208;
				short short_2774 = -5;
				int int_2775 = -2147483648;
				long long_2776 = 0;
				sbyte sbyte_2777 = 2;
				float float_2778 = -1.96f;
				string string_2779 = "5354EXK6";
				ushort ushort_2780 = 5;
				uint uint_2781 = 2147483648;
				ulong ulong_2782 = 1;
				S1 s1_2783 = new S1();
				S2.S2_D1_F1 s2_s2_d1_f1_2784 = new S2.S2_D1_F1();
				S2 s2_2785 = new S2();
				S2 s2_2786 = s2_2785;
				int __loopvar2 = s_loopInvariant, __loopSecondaryVar2_0 = s_loopInvariant;
				s_s1_16 = Method23(out s1_33, out s_s1_16, ref int_2775, s2_s2_d1_f1_2784, out s_s2_18, ref s_s2_s2_d1_f1_17, ((float)(s_int_8 /= ((int)((((int)(((int)(int_25 % ((int)((LeafMethod6()) | 91)))) * ((int)(int_25 >> s_int_8))))) | 29)))));
				s2_2786 = Method56(ref s2_2785);
				s_s1_16 = Method57(ref s_ushort_13, LeafMethod3(), ref s2_s2_d1_f1_2784, s2_35, s2_35, ref s1_2783, s_sbyte_10, s2_35, ref s2_s2_d1_f1_34, s2_s2_d1_f1_2784);
				s2_s2_d1_f1_2784 = Method58(out ulong_2782, s2_2785, ref s2_2786, out ushort_30, s2_s2_d1_f1_2784, ref s1_33, s2_s2_d1_f1_34, s2_2785, out s_s2_s2_d1_f1_17, ref s_int_8, ref s_s2_s2_d1_f1_17, s_s2_s2_d1_f1_17, s1_33, s1_33, out s2_s2_d1_f1_2784);
				s_double_6 = Method59(s1_33, s_s2_18, ref s_s2_s2_d1_f1_17, s_s1_16, s_s2_18);
				s_short_7 = Method60(ref s2_2785, ref s_ulong_15, ((double)(s_double_6 - ((double)(((double)(double_2773 += s_double_6)) + ((double)(s_int_8 / ((int)((s_int_8) | 61)))))))), s2_s2_d1_f1_34, out s2_35, long_2776, s_s2_s2_d1_f1_17, ref s2_2785, s1_2783.float_0, s1_33);
				s2_2785 = Method61(s2_s2_d1_f1_34, long_2776, ref uint_2781);
				s2_s2_d1_f1_34 = Method62(s_s2_s2_d1_f1_17, ref s1_2783, ref int_25);
				ushort_2780 = Method63(out decimal_2772, LeafMethod2(), ref s2_s2_d1_f1_34, ref s_s2_s2_d1_f1_17, ulong_32, LeafMethod2(), out sbyte_2777, s2_s2_d1_f1_2784, ref s_s2_18, s1_2783, out ulong_2782, ((ulong)(ulong_32 <<= int_2775)), out s_s2_18, out s2_s2_d1_f1_2784);
				s_s2_18 = Method64(s1_33, out s1_33, s1_2783, out s1_33, ref s_s2_18, s2_35, out s2_s2_d1_f1_34, ref s_s2_18, ref ushort_30, ref s_s1_16, ref s_s1_16, ref s2_s2_d1_f1_2784, ref s2_2785, ref s1_33, out s_s1_16);
				s_s2_18 = Method65(out s2_s2_d1_f1_34, ref s_s1_16, ref s_short_7, out long_2776, s2_2785, s_s2_s2_d1_f1_17);
				s1_2783 = Method66(out double_23, ref s1_2783, out s1_2783, ref s_s2_18, ((uint)(s2_s2_d1_f1_2784.uint_1 = ((uint)(s_int_8 / ((int)((s_int_8) | 15)))))), ref s_s1_16);
				s_long_9 = Method67(s1_33, out s1_2783, ref s_s2_s2_d1_f1_17, ref int_25, ref s2_s2_d1_f1_34);
				s2_s2_d1_f1_34 = Method68(out s2_2785, ((long)(s_int_8 %= ((int)((((int)(int_2775 >>= LeafMethod6()))) | 47)))), ref s2_2785, ref s_s1_16, s_s2_18, ref double_2773);
				sbyte_2777 = Method69(out s1_33, ref s_s2_s2_d1_f1_17);
				s_s2_s2_d1_f1_17 = Method70(ref s1_33.float_0, ((bool)(((bool)(((float)(s1_2783.float_0 * LeafMethod9())) >= LeafMethod9())) == ((bool)(bool_2769 = ((bool)(LeafMethod12() != 2)))))), s1_33, out s1_2783.float_0, s_s1_16, ref s2_s2_d1_f1_34, ref s_decimal_5, ref s2_s2_d1_f1_2784, ((long)(s_long_9 >>= ((int)(((int)(s_int_8 ^ int_25)) ^ ((int)(int_2775 / ((int)((s_int_8) | 97)))))))), s1_33, ref s2_s2_d1_f1_2784);
				s2_s2_d1_f1_2784.uint_1 = Method71(s1_33, out s_s1_16.float_0, s2_35, out s1_2783, out s1_2783, out s2_35, ref s_s1_16, s1_2783, ref s_double_6, ref short_24);
				s_s2_s2_d1_f1_17 = Method72(s2_2785, s2_s2_d1_f1_2784, ref s_s2_s2_d1_f1_17.uint_1, ref s_ulong_15, out s_s2_s2_d1_f1_17, ref s1_33, s2_s2_d1_f1_2784, s2_s2_d1_f1_34, out s1_2783);
				s1_33 = Method73(((bool)(bool_19 = s_bool_2)), s1_2783, ref s2_2786, ref sbyte_27, ref s_s1_16, ref s1_33, ref s1_33, out s2_s2_d1_f1_2784.uint_1, out s2_2786);
				s_s2_18 = Method74(ref s1_33, ((float)(int_25 / ((int)((((int)(int_2775 = ((int)(int_25 |= int_25))))) | 25)))), out s2_s2_d1_f1_2784, s2_s2_d1_f1_2784, out ulong_32, s2_2785);
				s2_s2_d1_f1_2784 = Method75(ref s2_35, ((ulong)(((int)(((int)(int_25 | -2)) | int_25)) % ((int)((((int)(((int)(s_int_8 /= ((int)((s_int_8) | 89)))) - LeafMethod6()))) | 24)))), out s1_33, ((ushort)(s_ushort_13 |= ushort_30)), s2_35, ((decimal)(s_int_8 % ((int)((((int)(int_25 << ((int)(int_2775 -= LeafMethod6()))))) | 22)))), out s_s2_18, ref s2_35, s_s2_18, ref s_s2_s2_d1_f1_17, out s2_2785, ref s2_s2_d1_f1_34);
				int_2775 = Method76(out long_2776, s2_s2_d1_f1_34, ((float)(((float)(((int)(LeafMethod6() >> LeafMethod6())) % ((int)((LeafMethod6()) | 1)))) * ((float)(((float)(LeafMethod6() / ((int)((s_int_8) | 81)))) * ((float)(s_float_11 - s_float_11)))))), ref s2_s2_d1_f1_2784, out s1_2783, s1_2783, s_s1_16, out s2_2785, s1_2783, out s2_s2_d1_f1_2784, ref s_int_8, ref s1_33, ref s_uint_14);
				s_int_8 = Method77(s_s2_18, s_s1_16, s2_35, ref long_26, out s_long_9, ref s2_2786, ref s1_33, s2_s2_d1_f1_2784, ref s_s2_18, ref s_uint_14);
				s1_33 = Method78(ref s2_35, s2_s2_d1_f1_34, ulong_2782, s2_s2_d1_f1_2784, s_int_8, out s2_35);
				s_ulong_15 = Method79(ref char_21, ref s2_s2_d1_f1_2784, ref s_s1_16, byte_2770, out s1_2783, ref s_s2_18, ref sbyte_27, ((int)(int_2775 *= ((int)(((int)(LeafMethod6() % ((int)((LeafMethod6()) | 99)))) + LeafMethod6())))), s1_2783, s2_35, ref s2_s2_d1_f1_2784, out ulong_2782, s1_33);
				s2_2785 = Method80(ref s_long_9, s1_33, ref s_s2_s2_d1_f1_17.uint_1, ref s2_2786, s2_35, s_s2_s2_d1_f1_17, ref s2_35, ref s2_2786, out s2_35, out s2_s2_d1_f1_2784, ((ushort)(((int)(((int)(s_int_8 ^ s_int_8)) - ((int)(int_2775 % ((int)((LeafMethod6()) | 4)))))) % ((int)((((int)(s_int_8 % ((int)((((int)(int_25 += LeafMethod6()))) | 40))))) | 29)))), ref s_s1_16);
				s2_35 = Method81(ref s2_s2_d1_f1_34.uint_1, out s1_33, s_s2_s2_d1_f1_17, s1_2783, s_s1_16, ref s2_s2_d1_f1_2784, s2_2785, ref s_s2_s2_d1_f1_17, out s_s1_16, out s2_35, s2_s2_d1_f1_34, out s2_s2_d1_f1_2784, ref s2_s2_d1_f1_2784, ref s1_2783);
				s_float_11 = Method82(ref s_long_9, out s1_33, s1_33, ushort_2780, out s_s2_18, out s1_33, s2_35, ref decimal_22, ref s_s2_18, ref s_float_11, ref sbyte_2777, out s1_2783, s_s2_s2_d1_f1_17);
				s_s2_18 = Method83(out s_s1_16);
				s1_33 = Method84(s2_2785, ref s_s2_s2_d1_f1_17, ref s_s2_18, ((long)(s_int_8 % ((int)((int_2775) | 28)))), s2_s2_d1_f1_34.uint_1, s_short_7, ref float_2778, ref s_s1_16, out s2_35, ref s2_2786, ref s_sbyte_10, out s1_33);
				s_short_7 = Method85(ref s2_35, s2_s2_d1_f1_2784, s_s1_16, ref s_double_6, out s2_35, s_s2_18, ref s1_33.float_0, s2_35, ref s1_33.float_0, s1_2783);
				ulong_32 = Method86(((double)(((double)(((int)(LeafMethod6() >> s_int_8)) / ((int)((s_int_8) | 63)))) * ((double)(s_int_8 %= ((int)((((int)(int_2775 ^ s_int_8))) | 8)))))));
				s2_s2_d1_f1_34 = Method87(s_s2_18, ref long_2776, out s2_2785, ((byte)(byte_2770 <<= ((int)(s_int_8 %= ((int)((((int)(LeafMethod6() | int_25))) | 34)))))), out long_26, s2_35, s_s2_18, s1_33);
				decimal_22 = Method88(out string_2779, ref s_s2_18, s1_2783, ref s1_33, ref s_s2_s2_d1_f1_17, out s2_s2_d1_f1_34, out float_2778, ref ulong_32, ((float)(s_s1_16.float_0 + ((float)(((int)(int_25 += LeafMethod6())) % ((int)((s_int_8) | 16)))))), out s_s2_s2_d1_f1_17, ref s_s2_18, s2_2786, out s_s1_16, ref s2_2785, s_s2_18, s1_2783, s_s1_16, out s2_s2_d1_f1_34);
				s2_35 = Method89(out s_s2_s2_d1_f1_17, out s_s2_18, out s1_33, ref s1_33, s1_2783);
				s2_35 = Method90(out s_byte_3, s2_s2_d1_f1_2784, ref s1_33, s2_s2_d1_f1_2784.uint_1);
				s_s2_18 = Method91(out int_25, s1_33, s1_2783, ref s2_s2_d1_f1_34, ref s_s1_16, ref s2_s2_d1_f1_2784, s1_2783, out s2_s2_d1_f1_2784, out s_s2_18, out s2_s2_d1_f1_2784, ((int)(int_25 % ((int)((((int)(((int)(int_2775 *= 0)) >> ((int)(s_int_8 &= int_2775))))) | 56)))), s_s2_s2_d1_f1_17, s_s1_16);
				s1_33 = Method92(s1_33, s2_s2_d1_f1_34, s1_2783, ref s1_2783, ref s2_2785, s1_2783, s1_2783, s2_s2_d1_f1_2784, s2_s2_d1_f1_34, s2_s2_d1_f1_34, s1_33, ref long_26, out ushort_2780, s_s2_s2_d1_f1_17, ref s_ushort_13, ref sbyte_27, ((sbyte)(s_sbyte_10 <<= Method77(s2_2785, s1_2783, s2_35, ref long_2776, out long_26, ref s2_35, ref s_s1_16, s_s2_s2_d1_f1_17, ref s_s2_18, ref uint_2781))), ((byte)(int_25 / ((int)((((int)(((int)(LeafMethod6() >> int_25)) + ((int)(LeafMethod6() + int_25))))) | 68)))), out s_s2_s2_d1_f1_17);
				s2_s2_d1_f1_2784 = Method93(s_s2_s2_d1_f1_17, out s2_s2_d1_f1_2784, ref s2_s2_d1_f1_34, s2_35, ref s_long_9, s_s2_18, out s1_33, out s1_2783, ref s_s2_s2_d1_f1_17, ref s2_s2_d1_f1_2784, out s2_s2_d1_f1_34.uint_1, out s_s2_s2_d1_f1_17, s2_2785, out s_s2_18, ((float)(((float)(((float)(LeafMethod9() * LeafMethod9())) + ((float)(s1_2783.float_0 -= s_float_11)))) + ((float)(((float)(int_25 % ((int)((s_int_8) | 35)))) + ((float)(float_28 + LeafMethod9())))))), s2_35, out s1_2783);
				s_s2_s2_d1_f1_17 = Method94(ref s_s1_16, out ushort_30, s2_s2_d1_f1_34, s1_2783, ref s2_s2_d1_f1_34, s_s2_s2_d1_f1_17, out s1_2783, Method82(ref s_long_9, out s1_2783, s1_33, ushort_2780, out s2_35, out s1_33, s2_35, ref s_decimal_5, ref s_s2_18, ref float_28, ref sbyte_27, out s1_33, s2_s2_d1_f1_2784), ref s1_2783, s2_s2_d1_f1_34, s2_2785, ref s_s1_16, ((ushort)(s_ushort_13 -= s_ushort_13)), ref s1_2783, decimal_22, ref ulong_32, out s_s2_s2_d1_f1_17);
				s1_2783 = Method95(ref s2_35, out s1_2783, s_s1_16, s1_33, out s1_33, ((double)(s_double_6 = double_23)), ((float)(float_28 *= ((float)(((float)(s_float_11 = s1_2783.float_0)) - ((float)(s_int_8 / ((int)((int_25) | 70)))))))), s1_2783, out s1_2783, ref s_s2_s2_d1_f1_17, out s2_35);
				byte_2770 = Method96(out s_s2_18, ((ulong)(((ulong)(((ulong)(LeafMethod13() + ulong_2782)) ^ ((ulong)(s_ulong_15 - LeafMethod13())))) ^ ((ulong)(((int)(int_25 << s_int_8)) % ((int)((((int)(s_int_8 % ((int)((LeafMethod6()) | 35))))) | 73)))))), out ulong_2782);
				s_s2_s2_d1_f1_17 = Method97(s1_33, s_s2_s2_d1_f1_17, ref float_2778, out s1_33, ref int_2775, s2_s2_d1_f1_2784, ref s_bool_2, ref s_s2_s2_d1_f1_17, out s2_s2_d1_f1_2784, s2_s2_d1_f1_2784, ((long)(long_2776 += long_2776)), out short_24, ref s1_2783, s_uint_14);
				s1_33 = Method98(s2_s2_d1_f1_34, ((float)(((float)(float_2778 - ((float)(s_float_11 * s_float_11)))) + ((float)(((float)(int_25 / ((int)((LeafMethod6()) | 40)))) + s1_2783.float_0)))), ref s2_s2_d1_f1_34, s_s2_18, ref s2_s2_d1_f1_34, s2_s2_d1_f1_2784, out s1_33, ref s2_2786, ref int_25, ref s_s2_18, s2_s2_d1_f1_2784, ref s2_35, ref s_s2_18, ((decimal)(LeafMethod3() + decimal_2772)), s2_s2_d1_f1_2784, ref ushort_2780);
				s2_2785 = Method99(s_s1_16, ((float)(s_float_11 + ((float)(float_28 *= ((float)(int_2775 / ((int)((int_25) | 51)))))))), ((short)(short_24 &= ((short)(((int)(int_2775 += int_2775)) % ((int)((((int)(int_25 >>= s_int_8))) | 12)))))), s2_s2_d1_f1_34, s2_35, ref s2_35, out s_s2_18, s_s1_16, ((decimal)(decimal_2772 = ((decimal)(((int)(int_2775 | int_25)) % ((int)((((int)(int_25 % ((int)((LeafMethod6()) | 57))))) | 2)))))), ref uint_31, s_s2_18, out s_int_8, Method11(ref s2_35, ref int_2775, ref short_2774, ref decimal_2772, s1_2783, out bool_2769, out s_s2_18, s2_35, s_s1_16, out s1_33.float_0, out s2_35, s2_2785), ref decimal_22, out s_int_8, s2_2785, s1_2783);
				return;
			}
		}
	}

    // This is trying to stress the JIT to ensure we do not encounter an assertion.
	[Fact]
	public static int TestEntryPoint() {
		new TestClass().Method0();
		return 100;
	}
}
