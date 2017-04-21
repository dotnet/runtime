using System;

namespace TestCases.Xml.ReferenceInAttributes
{
	public class LibLibAttribute : Attribute
	{
		public Type LibLibType {
			[AssertLinked]
			get { return null; }
			set { }
		}
	}

	public class BilBil
	{

		[AssertLinked]
		public BilBil ()
		{
		}
	}
}

