namespace Test {

public enum YaddaYadda {
	buba,
	birba,
	dadoom,
};

public enum byteenum : byte {
	zero,
	one,
	two,
	three
}

public enum longenum: long {
	s0 = 0,
	s1 = 1
}

public enum sbyteenum : sbyte {
	d0,
	d1
}

public class test {
	public static int Main () {
		YaddaYadda val = YaddaYadda.dadoom;
		byteenum be = byteenum.one;
		if (val != YaddaYadda.dadoom)
			return 1;
		if (be != (byteenum)1)
			return 2;
		return 0;
	}
}

}
