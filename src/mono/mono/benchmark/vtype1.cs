using System;

public struct TestResult
{
        public TestResult(int x) {
		a=x;
	}

	public int a;
}

public class Test
{
	static int Main ()
	{
		TestResult x;
		
		for (int i = 0; i < 500000000; i++) {
			//x = new TestResult(i, 0m);
			x = new TestResult(i);
		}
		
		return 0;
	}

}
