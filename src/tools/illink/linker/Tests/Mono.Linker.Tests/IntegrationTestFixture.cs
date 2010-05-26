//
// IntegrationTestFixture.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
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
using System.Diagnostics;
using System.IO;
using Mono.Linker.Steps;
using NUnit.Framework;

namespace Mono.Linker.Tests {

	[TestFixture]
	public class IntegrationTestFixture : AbstractTestFixture {

		[SetUp]
		public override void SetUp ()
		{
			base.SetUp ();

			TestCasesRoot = "Integration";
		}

		[Test]
		public void TestHelloWorld ()
		{
			RunTest ("HelloWorld");
		}

		[Test]
		public void TestCrypto ()
		{
			RunTest ("Crypto");
		}

		protected override LinkContext GetContext()
		{
			LinkContext context = base.GetContext ();
			context.CoreAction = AssemblyAction.Link;
			return context;
		}

		protected override Pipeline GetPipeline ()
		{
			Pipeline p = new Pipeline ();
			p.AppendStep (new LoadReferencesStep ());
			p.AppendStep (new BlacklistStep ());
			p.AppendStep (new TypeMapStep ());
			p.AppendStep (new MarkStep ());
			p.AppendStep (new SweepStep ());
			p.AppendStep (new CleanStep ());
			p.AppendStep (new OutputStep ());
			return p;
		}

		protected override void RunTest (string testCase)
		{
			if (!OnMono ())
				Assert.Ignore ("Integration tests are only runnable on Mono");

			base.RunTest (testCase);
			string test = Path.Combine (GetTestCasePath (), "Test.exe");

			string original = Execute (GetTestCasePath (), "Test.exe");

			Pipeline.PrependStep (
				new ResolveFromAssemblyStep (
					test));

			Run ();

			string linked = Execute (Context.OutputDirectory, "Test.exe");

			Assert.AreEqual (original, linked);
		}

		static bool OnMono ()
		{
			return Type.GetType ("System.MonoType") != null;
		}

		static string Execute (string directory, string file)
		{
			Process p = new Process ();
			p.StartInfo.EnvironmentVariables ["MONO_PATH"] = directory;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.WorkingDirectory = directory;
			p.StartInfo.FileName = "mono";
			p.StartInfo.Arguments = file;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.UseShellExecute = false;

			p.Start ();
			p.WaitForExit ();

			return p.StandardOutput.ReadToEnd ();
		}
	}
}
