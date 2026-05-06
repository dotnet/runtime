namespace NS.B
{
	public class TestB
	{
		private static readonly NS.A.TestA testb = new NS.A.TestA ();

		public TestB ()
		{
			if (testb == null) {
			}
		}
	}
}
