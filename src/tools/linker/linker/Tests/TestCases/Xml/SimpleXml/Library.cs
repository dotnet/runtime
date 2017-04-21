using System;

namespace TestCases.Xml.SimpleXml
{
	public class Foo
	{

		int _baz;
		[AssertLinked] int _shebang;

		public Foo ()
		{
			_baz = 42;
		}

		public int Baz ()
		{
			return _baz;
		}

		[AssertLinked]
		public int Shebang (int bang)
		{
			return _shebang = bang * 2;
		}
	}

	public class Bar
	{

		int _truc;

		public Bar ()
		{
			_truc = 12;
		}

		public int Truc ()
		{
			return _truc;
		}
	}

	[AssertLinked]
	public class Gazonk
	{
	}
}
