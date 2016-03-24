// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

[SecuritySafeCritical]
class TestApp {
	//***** TEST CODE *****
	static double test_0_0(double num, AA init, AA zero) {
		return init.q;
	}
	static double test_0_1(double num, AA init, AA zero) {
		zero.q=num;
		return zero.q;
	}
	static double test_0_2(double num, AA init, AA zero) {
		return init.q+zero.q;
	}
	static double test_0_3(double num, AA init, AA zero) {
		return checked(init.q-zero.q);
	}
	static double test_0_4(double num, AA init, AA zero) {
		zero.q+=num; return zero.q;
		
	}
	static double test_0_5(double num, AA init, AA zero) {
		zero.q+=init.q; return zero.q;
		
	}
	static double test_0_6(double num, AA init, AA zero) {
		if (init.q==num)
			return 100;
		else
			return zero.q;
		
	}
	static double test_0_7(double num, AA init, AA zero) {
		return init.q<num+1 ? 100 : -1;
		
	}
	static double test_0_8(double num, AA init, AA zero) {
		return (init.q>zero.q?1:0)+99;
		
	}
	static double test_0_9(double num, AA init, AA zero) {
		object bb=init.q;
		return (double)bb;
	}
	static double test_0_10(double num, AA init, AA zero) {
		double dbl=init.q;
		return (double)dbl;
	}
	static double test_0_11(double num, AA init, AA zero) {
		return AA.call_target(init.q);
	}
	static double test_0_12(double num, AA init, AA zero) {
		return AA.call_target_ref(ref init.q);
	}
	static double test_1_0(double num, ref AA r_init, ref AA r_zero) {
		return r_init.q;
	}
	static double test_1_1(double num, ref AA r_init, ref AA r_zero) {
		r_zero.q=num;
		return r_zero.q;
	}
	static double test_1_2(double num, ref AA r_init, ref AA r_zero) {
		return r_init.q+r_zero.q;
	}
	static double test_1_3(double num, ref AA r_init, ref AA r_zero) {
		return checked(r_init.q-r_zero.q);
	}
	static double test_1_4(double num, ref AA r_init, ref AA r_zero) {
		r_zero.q+=num; return r_zero.q;
		
	}
	static double test_1_5(double num, ref AA r_init, ref AA r_zero) {
		r_zero.q+=r_init.q; return r_zero.q;
		
	}
	static double test_1_6(double num, ref AA r_init, ref AA r_zero) {
		if (r_init.q==num)
			return 100;
		else
			return r_zero.q;
		
	}
	static double test_1_7(double num, ref AA r_init, ref AA r_zero) {
		return r_init.q<num+1 ? 100 : -1;
		
	}
	static double test_1_8(double num, ref AA r_init, ref AA r_zero) {
		return (r_init.q>r_zero.q?1:0)+99;
		
	}
	static double test_1_9(double num, ref AA r_init, ref AA r_zero) {
		object bb=r_init.q;
		return (double)bb;
	}
	static double test_1_10(double num, ref AA r_init, ref AA r_zero) {
		double dbl=r_init.q;
		return (double)dbl;
	}
	static double test_1_11(double num, ref AA r_init, ref AA r_zero) {
		return AA.call_target(r_init.q);
	}
	static double test_1_12(double num, ref AA r_init, ref AA r_zero) {
		return AA.call_target_ref(ref r_init.q);
	}
	static double test_2_0(double num) {
		return AA.a_init[(int)num].q;
	}
	static double test_2_1(double num) {
		AA.a_zero[(int)num].q=num;
		return AA.a_zero[(int)num].q;
	}
	static double test_2_2(double num) {
		return AA.a_init[(int)num].q+AA.a_zero[(int)num].q;
	}
	static double test_2_3(double num) {
		return checked(AA.a_init[(int)num].q-AA.a_zero[(int)num].q);
	}
	static double test_2_4(double num) {
		AA.a_zero[(int)num].q+=num; return AA.a_zero[(int)num].q;
		
	}
	static double test_2_5(double num) {
		AA.a_zero[(int)num].q+=AA.a_init[(int)num].q; return AA.a_zero[(int)num].q;
		
	}
	static double test_2_6(double num) {
		if (AA.a_init[(int)num].q==num)
			return 100;
		else
			return AA.a_zero[(int)num].q;
		
	}
	static double test_2_7(double num) {
		return AA.a_init[(int)num].q<num+1 ? 100 : -1;
		
	}
	static double test_2_8(double num) {
		return (AA.a_init[(int)num].q>AA.a_zero[(int)num].q?1:0)+99;
		
	}
	static double test_2_9(double num) {
		object bb=AA.a_init[(int)num].q;
		return (double)bb;
	}
	static double test_2_10(double num) {
		double dbl=AA.a_init[(int)num].q;
		return (double)dbl;
	}
	static double test_2_11(double num) {
		return AA.call_target(AA.a_init[(int)num].q);
	}
	static double test_2_12(double num) {
		return AA.call_target_ref(ref AA.a_init[(int)num].q);
	}
	static double test_3_0(double num) {
		return AA.aa_init[0,(int)num-1,(int)num/100].q;
	}
	static double test_3_1(double num) {
		AA.aa_zero[0,(int)num-1,(int)num/100].q=num;
		return AA.aa_zero[0,(int)num-1,(int)num/100].q;
	}
	static double test_3_2(double num) {
		return AA.aa_init[0,(int)num-1,(int)num/100].q+AA.aa_zero[0,(int)num-1,(int)num/100].q;
	}
	static double test_3_3(double num) {
		return checked(AA.aa_init[0,(int)num-1,(int)num/100].q-AA.aa_zero[0,(int)num-1,(int)num/100].q);
	}
	static double test_3_4(double num) {
		AA.aa_zero[0,(int)num-1,(int)num/100].q+=num; return AA.aa_zero[0,(int)num-1,(int)num/100].q;
		
	}
	static double test_3_5(double num) {
		AA.aa_zero[0,(int)num-1,(int)num/100].q+=AA.aa_init[0,(int)num-1,(int)num/100].q; return AA.aa_zero[0,(int)num-1,(int)num/100].q;
		
	}
	static double test_3_6(double num) {
		if (AA.aa_init[0,(int)num-1,(int)num/100].q==num)
			return 100;
		else
			return AA.aa_zero[0,(int)num-1,(int)num/100].q;
		
	}
	static double test_3_7(double num) {
		return AA.aa_init[0,(int)num-1,(int)num/100].q<num+1 ? 100 : -1;
		
	}
	static double test_3_8(double num) {
		return (AA.aa_init[0,(int)num-1,(int)num/100].q>AA.aa_zero[0,(int)num-1,(int)num/100].q?1:0)+99;
		
	}
	static double test_3_9(double num) {
		object bb=AA.aa_init[0,(int)num-1,(int)num/100].q;
		return (double)bb;
	}
	static double test_3_10(double num) {
		double dbl=AA.aa_init[0,(int)num-1,(int)num/100].q;
		return (double)dbl;
	}
	static double test_3_11(double num) {
		return AA.call_target(AA.aa_init[0,(int)num-1,(int)num/100].q);
	}
	static double test_3_12(double num) {
		return AA.call_target_ref(ref AA.aa_init[0,(int)num-1,(int)num/100].q);
	}
	static double test_4_0(double num) {
		return BB.f_init.q;
	}
	static double test_4_1(double num) {
		BB.f_zero.q=num;
		return BB.f_zero.q;
	}
	static double test_4_2(double num) {
		return BB.f_init.q+BB.f_zero.q;
	}
	static double test_4_3(double num) {
		return checked(BB.f_init.q-BB.f_zero.q);
	}
	static double test_4_4(double num) {
		BB.f_zero.q+=num; return BB.f_zero.q;
		
	}
	static double test_4_5(double num) {
		BB.f_zero.q+=BB.f_init.q; return BB.f_zero.q;
		
	}
	static double test_4_6(double num) {
		if (BB.f_init.q==num)
			return 100;
		else
			return BB.f_zero.q;
		
	}
	static double test_4_7(double num) {
		return BB.f_init.q<num+1 ? 100 : -1;
		
	}
	static double test_4_8(double num) {
		return (BB.f_init.q>BB.f_zero.q?1:0)+99;
		
	}
	static double test_4_9(double num) {
		object bb=BB.f_init.q;
		return (double)bb;
	}
	static double test_4_10(double num) {
		double dbl=BB.f_init.q;
		return (double)dbl;
	}
	static double test_4_11(double num) {
		return AA.call_target(BB.f_init.q);
	}
	static double test_4_12(double num) {
		return AA.call_target_ref(ref BB.f_init.q);
	}
	static double test_5_0(double num) {
		return ((AA)AA.b_init).q;
	}
	static unsafe double test_7_0(double num, void *ptr_init, void *ptr_zero) {
		return (*((AA*)ptr_init)).q;
	}
	static unsafe double test_7_1(double num, void *ptr_init, void *ptr_zero) {
		(*((AA*)ptr_zero)).q=num;
		return (*((AA*)ptr_zero)).q;
	}
	static unsafe double test_7_2(double num, void *ptr_init, void *ptr_zero) {
		return (*((AA*)ptr_init)).q+(*((AA*)ptr_zero)).q;
	}
	static unsafe double test_7_3(double num, void *ptr_init, void *ptr_zero) {
		return checked((*((AA*)ptr_init)).q-(*((AA*)ptr_zero)).q);
	}
	static unsafe double test_7_4(double num, void *ptr_init, void *ptr_zero) {
		(*((AA*)ptr_zero)).q+=num; return (*((AA*)ptr_zero)).q;
		
	}
	static unsafe double test_7_5(double num, void *ptr_init, void *ptr_zero) {
		(*((AA*)ptr_zero)).q+=(*((AA*)ptr_init)).q; return (*((AA*)ptr_zero)).q;
		
	}
	static unsafe double test_7_6(double num, void *ptr_init, void *ptr_zero) {
		if ((*((AA*)ptr_init)).q==num)
			return 100;
		else
			return (*((AA*)ptr_zero)).q;
		
	}
	static unsafe double test_7_7(double num, void *ptr_init, void *ptr_zero) {
		return (*((AA*)ptr_init)).q<num+1 ? 100 : -1;
		
	}
	static unsafe double test_7_8(double num, void *ptr_init, void *ptr_zero) {
		return ((*((AA*)ptr_init)).q>(*((AA*)ptr_zero)).q?1:0)+99;
		
	}
	static unsafe double test_7_9(double num, void *ptr_init, void *ptr_zero) {
		object bb=(*((AA*)ptr_init)).q;
		return (double)bb;
	}
	static unsafe double test_7_10(double num, void *ptr_init, void *ptr_zero) {
		double dbl=(*((AA*)ptr_init)).q;
		return (double)dbl;
	}
	static unsafe double test_7_11(double num, void *ptr_init, void *ptr_zero) {
		return AA.call_target((*((AA*)ptr_init)).q);
	}
	static unsafe double test_7_12(double num, void *ptr_init, void *ptr_zero) {
		return AA.call_target_ref(ref (*((AA*)ptr_init)).q);
	}

	//***** MAIN CODE *****
	static unsafe int Main() {
		AA.reset();
		if (test_0_0(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_0() failed.");
			return 101;
		}
		AA.verify_all();		AA.reset();
		if (test_0_1(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_1() failed.");
			return 102;
		}
		AA.verify_all();		AA.reset();
		if (test_0_2(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_2() failed.");
			return 103;
		}
		AA.verify_all();		AA.reset();
		if (test_0_3(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_3() failed.");
			return 104;
		}
		AA.verify_all();		AA.reset();
		if (test_0_4(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_4() failed.");
			return 105;
		}
		AA.verify_all();		AA.reset();
		if (test_0_5(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_5() failed.");
			return 106;
		}
		AA.verify_all();		AA.reset();
		if (test_0_6(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_6() failed.");
			return 107;
		}
		AA.verify_all();		AA.reset();
		if (test_0_7(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_7() failed.");
			return 108;
		}
		AA.verify_all();		AA.reset();
		if (test_0_8(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_8() failed.");
			return 109;
		}
		AA.verify_all();		AA.reset();
		if (test_0_9(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_9() failed.");
			return 110;
		}
		AA.verify_all();		AA.reset();
		if (test_0_10(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_10() failed.");
			return 111;
		}
		AA.verify_all();		AA.reset();
		if (test_0_11(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_11() failed.");
			return 112;
		}
		AA.verify_all();		AA.reset();
		if (test_0_12(100, new AA(100), new AA(0)) != 100) {
			Console.WriteLine("test_0_12() failed.");
			return 113;
		}
		AA.verify_all();		AA.reset();
		if (test_1_0(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_0() failed.");
			return 114;
		}
		AA.verify_all();		AA.reset();
		if (test_1_1(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_1() failed.");
			return 115;
		}
		AA.verify_all();		AA.reset();
		if (test_1_2(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_2() failed.");
			return 116;
		}
		AA.verify_all();		AA.reset();
		if (test_1_3(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_3() failed.");
			return 117;
		}
		AA.verify_all();		AA.reset();
		if (test_1_4(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_4() failed.");
			return 118;
		}
		AA.verify_all();		AA.reset();
		if (test_1_5(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_5() failed.");
			return 119;
		}
		AA.verify_all();		AA.reset();
		if (test_1_6(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_6() failed.");
			return 120;
		}
		AA.verify_all();		AA.reset();
		if (test_1_7(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_7() failed.");
			return 121;
		}
		AA.verify_all();		AA.reset();
		if (test_1_8(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_8() failed.");
			return 122;
		}
		AA.verify_all();		AA.reset();
		if (test_1_9(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_9() failed.");
			return 123;
		}
		AA.verify_all();		AA.reset();
		if (test_1_10(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_10() failed.");
			return 124;
		}
		AA.verify_all();		AA.reset();
		if (test_1_11(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_11() failed.");
			return 125;
		}
		AA.verify_all();		AA.reset();
		if (test_1_12(100, ref AA._init, ref AA._zero) != 100) {
			Console.WriteLine("test_1_12() failed.");
			return 126;
		}
		AA.verify_all();		AA.reset();
		if (test_2_0(100) != 100) {
			Console.WriteLine("test_2_0() failed.");
			return 127;
		}
		AA.verify_all();		AA.reset();
		if (test_2_1(100) != 100) {
			Console.WriteLine("test_2_1() failed.");
			return 128;
		}
		AA.verify_all();		AA.reset();
		if (test_2_2(100) != 100) {
			Console.WriteLine("test_2_2() failed.");
			return 129;
		}
		AA.verify_all();		AA.reset();
		if (test_2_3(100) != 100) {
			Console.WriteLine("test_2_3() failed.");
			return 130;
		}
		AA.verify_all();		AA.reset();
		if (test_2_4(100) != 100) {
			Console.WriteLine("test_2_4() failed.");
			return 131;
		}
		AA.verify_all();		AA.reset();
		if (test_2_5(100) != 100) {
			Console.WriteLine("test_2_5() failed.");
			return 132;
		}
		AA.verify_all();		AA.reset();
		if (test_2_6(100) != 100) {
			Console.WriteLine("test_2_6() failed.");
			return 133;
		}
		AA.verify_all();		AA.reset();
		if (test_2_7(100) != 100) {
			Console.WriteLine("test_2_7() failed.");
			return 134;
		}
		AA.verify_all();		AA.reset();
		if (test_2_8(100) != 100) {
			Console.WriteLine("test_2_8() failed.");
			return 135;
		}
		AA.verify_all();		AA.reset();
		if (test_2_9(100) != 100) {
			Console.WriteLine("test_2_9() failed.");
			return 136;
		}
		AA.verify_all();		AA.reset();
		if (test_2_10(100) != 100) {
			Console.WriteLine("test_2_10() failed.");
			return 137;
		}
		AA.verify_all();		AA.reset();
		if (test_2_11(100) != 100) {
			Console.WriteLine("test_2_11() failed.");
			return 138;
		}
		AA.verify_all();		AA.reset();
		if (test_2_12(100) != 100) {
			Console.WriteLine("test_2_12() failed.");
			return 139;
		}
		AA.verify_all();		AA.reset();
		if (test_3_0(100) != 100) {
			Console.WriteLine("test_3_0() failed.");
			return 140;
		}
		AA.verify_all();		AA.reset();
		if (test_3_1(100) != 100) {
			Console.WriteLine("test_3_1() failed.");
			return 141;
		}
		AA.verify_all();		AA.reset();
		if (test_3_2(100) != 100) {
			Console.WriteLine("test_3_2() failed.");
			return 142;
		}
		AA.verify_all();		AA.reset();
		if (test_3_3(100) != 100) {
			Console.WriteLine("test_3_3() failed.");
			return 143;
		}
		AA.verify_all();		AA.reset();
		if (test_3_4(100) != 100) {
			Console.WriteLine("test_3_4() failed.");
			return 144;
		}
		AA.verify_all();		AA.reset();
		if (test_3_5(100) != 100) {
			Console.WriteLine("test_3_5() failed.");
			return 145;
		}
		AA.verify_all();		AA.reset();
		if (test_3_6(100) != 100) {
			Console.WriteLine("test_3_6() failed.");
			return 146;
		}
		AA.verify_all();		AA.reset();
		if (test_3_7(100) != 100) {
			Console.WriteLine("test_3_7() failed.");
			return 147;
		}
		AA.verify_all();		AA.reset();
		if (test_3_8(100) != 100) {
			Console.WriteLine("test_3_8() failed.");
			return 148;
		}
		AA.verify_all();		AA.reset();
		if (test_3_9(100) != 100) {
			Console.WriteLine("test_3_9() failed.");
			return 149;
		}
		AA.verify_all();		AA.reset();
		if (test_3_10(100) != 100) {
			Console.WriteLine("test_3_10() failed.");
			return 150;
		}
		AA.verify_all();		AA.reset();
		if (test_3_11(100) != 100) {
			Console.WriteLine("test_3_11() failed.");
			return 151;
		}
		AA.verify_all();		AA.reset();
		if (test_3_12(100) != 100) {
			Console.WriteLine("test_3_12() failed.");
			return 152;
		}
		AA.verify_all();		AA.reset();
		if (test_4_0(100) != 100) {
			Console.WriteLine("test_4_0() failed.");
			return 153;
		}
		AA.verify_all();		AA.reset();
		if (test_4_1(100) != 100) {
			Console.WriteLine("test_4_1() failed.");
			return 154;
		}
		AA.verify_all();		AA.reset();
		if (test_4_2(100) != 100) {
			Console.WriteLine("test_4_2() failed.");
			return 155;
		}
		AA.verify_all();		AA.reset();
		if (test_4_3(100) != 100) {
			Console.WriteLine("test_4_3() failed.");
			return 156;
		}
		AA.verify_all();		AA.reset();
		if (test_4_4(100) != 100) {
			Console.WriteLine("test_4_4() failed.");
			return 157;
		}
		AA.verify_all();		AA.reset();
		if (test_4_5(100) != 100) {
			Console.WriteLine("test_4_5() failed.");
			return 158;
		}
		AA.verify_all();		AA.reset();
		if (test_4_6(100) != 100) {
			Console.WriteLine("test_4_6() failed.");
			return 159;
		}
		AA.verify_all();		AA.reset();
		if (test_4_7(100) != 100) {
			Console.WriteLine("test_4_7() failed.");
			return 160;
		}
		AA.verify_all();		AA.reset();
		if (test_4_8(100) != 100) {
			Console.WriteLine("test_4_8() failed.");
			return 161;
		}
		AA.verify_all();		AA.reset();
		if (test_4_9(100) != 100) {
			Console.WriteLine("test_4_9() failed.");
			return 162;
		}
		AA.verify_all();		AA.reset();
		if (test_4_10(100) != 100) {
			Console.WriteLine("test_4_10() failed.");
			return 163;
		}
		AA.verify_all();		AA.reset();
		if (test_4_11(100) != 100) {
			Console.WriteLine("test_4_11() failed.");
			return 164;
		}
		AA.verify_all();		AA.reset();
		if (test_4_12(100) != 100) {
			Console.WriteLine("test_4_12() failed.");
			return 165;
		}
		AA.verify_all();		AA.reset();
		if (test_5_0(100) != 100) {
			Console.WriteLine("test_5_0() failed.");
			return 166;
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_0(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_0() failed.");
				return 168;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_1(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_1() failed.");
				return 169;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_2(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_2() failed.");
				return 170;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_3(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_3() failed.");
				return 171;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_4(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_4() failed.");
				return 172;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_5(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_5() failed.");
				return 173;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_6(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_6() failed.");
				return 174;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_7(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_7() failed.");
				return 175;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_8(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_8() failed.");
				return 176;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_9(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_9() failed.");
				return 177;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_10(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_10() failed.");
				return 178;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_11(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_11() failed.");
				return 179;
			}
		}
		AA.verify_all();		AA.reset();
		fixed (void *p_init = &AA._init, p_zero = &AA._zero) {
			if (test_7_12(100, p_init, p_zero) != 100) {
				Console.WriteLine("test_7_12() failed.");
				return 180;
			}
		}
		AA.verify_all();		Console.WriteLine("All tests passed.");
		return 100;
	}
}
