using System;

namespace Test {

enum TestingEnum {This, Is, A, Test};
enum TestingEnum2 {This, Is, A, Test};

public class test {
	public static int Main () {
		int num = 0;
		
		Enum e1 = new TestingEnum();
		Enum e2 = new TestingEnum();
		Enum e3 = new TestingEnum2();

		++num;
		if (!e1.Equals(e2))
			return num;
		
		++num;
		if (e1.Equals(e3))
			return num;
		
		++num;
		if (TestingEnum.Test.Equals(TestingEnum2.Test))
			return num;
		
		return 0;
	}
}

}
