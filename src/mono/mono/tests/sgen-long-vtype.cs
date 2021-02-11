struct Bla {
	public object o;
	int i;
	object oa;
	int ia;
	object oaa;
	int iaa;
	object oaaa;
	int iaaa;
	object oaaaa;
	int iaaaa;
	object oaaaaa;
	int iaaaaa;
	object oaaaaaa;
	int iaaaaaa;
	object oaaaaaaa;
	int iaaaaaaa;
	object oaaaaaaaa;
	int iaaaaaaaa;
	object oaaaaaaaaa;
	int iaaaaaaaaa;
	object oaaaaaaaaaa;
	int iaaaaaaaaaa;
	object oaaaaaaaaaaa;
	int iaaaaaaaaaaa;
	object oaaaaaaaaaaaa;
	int iaaaaaaaaaaaa;
	object oaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	object oaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
	int iaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;
};

public class Test {
	static Bla [] blas;

	public static int Main (string [] args) {
		for (int i = 0; i < 200; ++i) {
			Bla [] x = new Bla [256];
			if (i % 10 == 0)
				blas = x;
			for (int j = 0; j < 256; ++j)
				blas [j].o = new int [32];
			blas [1] = x [1];
		}
		return 0;
	}
}
