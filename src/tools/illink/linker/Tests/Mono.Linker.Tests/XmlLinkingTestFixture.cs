//
// XmlLinkingTestFixture.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Xml.XPath;

using Mono.Linker.Steps;
using NUnit.Framework;

namespace Mono.Linker.Tests {

	[TestFixture]
	public class XmlLinkingTestFixture : AbstractLinkingTestFixture {

		[Test]
		public void TestSimpleXml ()
		{
			RunTest ("SimpleXml");
		}

		[Test]
		public void TestInterface ()
		{
			RunTest ("Interface");
		}

		[Test]
		public void TestReferenceInVirtualMethod ()
		{
			RunTest ("ReferenceInVirtualMethod");
		}

		[Test]
		public void TestGenerics ()
		{
			RunTest ("Generics");
		}

		[Test]
		public void TestNestedNested ()
		{
			RunTest ("NestedNested");
		}

		[Test]
		public void TestPreserveFieldsRequired ()
		{
			RunTest ("PreserveFieldsRequired");
		}

		[Test]
		public void TestReferenceInAttributes ()
		{
			RunTest ("ReferenceInAttributes");
		}

		[Test]
		public void TestXmlPattern ()
		{
			RunTest ("XmlPattern");
		}

		protected override void RunTest (string testCase)
		{
			var fullTestCaseName = "TestCases.Xml." + testCase;
			string resourceName = fullTestCaseName + ".desc.xml";

			var ms = new MemoryStream ();
			var assembly = typeof (TestCases.AssertLinkedAttribute).Assembly;
			using (Stream stream = assembly.GetManifestResourceStream (resourceName)) {
				Assert.IsNotNull (stream, "Missing embedded desc.xml");
				using (StreamReader reader = new StreamReader (stream)) {
					stream.CopyTo (ms);
				}
			}

			ms.Seek (0, SeekOrigin.Begin);

			base.RunTest (fullTestCaseName);

			Pipeline.PrependStep (new ResolveFromXmlStep (new XPathDocument (ms)));

			string cd = Environment.CurrentDirectory;
			Environment.CurrentDirectory = GetTestCasePath ();
			try {
				Run ();
			} finally {
				Environment.CurrentDirectory = cd;
			}
		}
	}
}
