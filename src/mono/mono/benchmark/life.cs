// created on 03/03/2002 at 15:12
using System;

class App {
    static String s0, s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15;
    static int[] offsets = {-16, -15, -14, -1, 1, 14, 15, 16};
    public static int Main(String[] args) {
	    int i2 = 500;
	    int i0;
	    double n2;
	    DateTime start, end;
	    start = DateTime.Now;
	s0 = "               ";
	s1 = "               ";
	s2 = "               ";
	s3 = "      ***      ";
	s4 = "               ";
	s5 = "               ";
	s6 = "         *     ";
	s7 = "         *     ";
	s8 = "         *     ";
	s9 = "     *         ";
	s10 ="     *         ";
	s11 ="     *         ";
	s12 ="               ";
	s13 ="               ";
	s14 ="               ";
	s15 ="";
	s15 = s0+s1+s2+s3+s4+s5+s6+s7+s8+s9+s10+s11+s12+s13+s14;
	dump();
	i0 =0;
	while (i0++ < i2) {
		generate();
		dump();
	}        
	end = DateTime.Now;
	n2 = (end-start).TotalMilliseconds;
	Console.WriteLine("{0} generations in {1} milliseconds, {2} gen/sec.",
	i2, (int)n2, (int)(i2/(n2/1000)));
	return 0;
}    
static void generate() {
	int i0, i1, i2, i3;
	i0 = s15.Length;
	s1 = "";
	i1 = 0;
	do {
		i2 = 0;
		foreach (int offset in offsets) {
			i3 = (offset + i0 + i1) % i0;
			if (s15.Substring(i3, 1) == "*")
				i2++;
		}            
		if (s15.Substring(i1, 1) == "*") {
			if (i2 < 2 || i2 > 3) {
				s1 += " ";
			} else {
				s1 += "*";
			}
		} else {
			if (i2 == 3) {
				s1 += "*";
			} else {
				s1 += "*";
			}
		}
	} while (++i1 < i0);
	s15 = s1;
}
static void dump() {
	;
}
}

