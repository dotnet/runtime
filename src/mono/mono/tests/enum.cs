namespace Test {

public enum YaddaYadda {
	buba,
	birba,
	dadoom,
};

public class test {
	public static int Main () {
		YaddaYadda val = YaddaYadda.dadoom;
		if (val != YaddaYadda.dadoom)
			return 1;
		return 0;
	}
}

}
