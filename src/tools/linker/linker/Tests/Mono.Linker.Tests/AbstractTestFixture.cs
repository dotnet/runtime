//
// AbstractTestFixture.cs
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

namespace Mono.Linker.Tests {

	using System;
	using System.IO;

	using Mono.Cecil;

	using NUnit.Framework;

	public abstract class AbstractTestFixture {

		string _testCasesRoot;
		string _testCase;
		LinkContext _context;
		Pipeline _pipeline;

		protected LinkContext Context {
			get { return _context; }
			set { _context = value; }
		}

		protected Pipeline Pipeline {
			get { return _pipeline; }
			set { _pipeline = value; }
		}

		public string TestCasesRoot {
			get { return _testCasesRoot; }
			set { _testCasesRoot = value; }
		}

		public string TestCase {
			get { return _testCase; }
			set { _testCase = value; }
		}

		[SetUp]
		public virtual void SetUp ()
		{
			_pipeline = GetPipeline ();
		}

		[TearDown]
		public virtual void TearDown ()
		{
		}

		protected virtual Pipeline GetPipeline ()
		{
			return new Pipeline ();
		}

		protected virtual LinkContext GetContext ()
		{
			LinkContext context = new LinkContext (_pipeline);
			context.OutputDirectory = GetOutputPath ();
			context.CoreAction = AssemblyAction.Copy;
			return context;
		}

		protected string GetOutputPath ()
		{
			return Path.Combine (
				Path.GetTempPath (),
				_testCase);
		}

		protected string GetTestCasePath ()
		{
			return Path.Combine (
				Path.Combine (
#if DEBUG
					Path.Combine (Environment.CurrentDirectory,
						Path.Combine ("..", "..")),
#else
					Environment.CurrentDirectory,
#endif
					"TestCases"),
				Path.Combine (
					_testCasesRoot,
					_testCase));
		}

		protected virtual void RunTest (string testCase)
		{
			_testCase = testCase;
			_context = GetContext ();
		}

		protected virtual void Run ()
		{
			string cd = Environment.CurrentDirectory;
			Environment.CurrentDirectory = GetTestCasePath ();
			try {
				_pipeline.Process (_context);
			} finally {
				Environment.CurrentDirectory = cd;
			}
		}

		protected static string GetAssemblyFileName (AssemblyDefinition asm)
		{
			return asm.Name.Name + (asm.Kind == AssemblyKind.Dll ? ".dll" : ".exe");
		}
	}
}
