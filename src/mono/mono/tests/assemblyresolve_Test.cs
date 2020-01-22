using System;
using TestBase;

namespace Test
{
  public class Test : TestBase.TestBase
  {

  }

	public class ReturnsTestBase {
		public TestBase.TestBase M () { return null; }
	}
}
