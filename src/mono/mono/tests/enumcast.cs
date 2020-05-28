namespace Test {
	enum MyEnum {
		ZERO,
		ONE
	}
	public class Test {

		static int unbox_and_return (object obj) {
			return (int)(MyEnum)obj;
		}
		public static int Main() {
			if (unbox_and_return (MyEnum.ZERO) != 0)
				return 1;
			if (unbox_and_return (MyEnum.ONE) != 1)
				return 2;
			return 0;
		}
	}
}
