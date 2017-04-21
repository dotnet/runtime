//
// AbstractLinkingTestFixture.cs
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

using System.IO;
using Mono.Cecil;
using Mono.Linker.Steps;
using NUnit.Framework;
using System.Linq;
using System.Collections.Generic;

namespace Mono.Linker.Tests
{

	public abstract class AbstractLinkingTestFixture : AbstractTestFixture
	{

		[SetUp]
		public override void SetUp ()
		{
			base.SetUp ();

			TestCasesRoot = "Linker";
		}

		[TearDown]
		public override void TearDown ()
		{
			base.TearDown ();

			string output = GetOutputPath ();
			if (Directory.Exists (output))
				Directory.Delete (output, true);
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
			base.RunTest (testCase);

			Prepare ();
		}

		void Prepare ()
		{
			Context = GetContext ();
			string output = GetOutputPath ();
			if (Directory.Exists (output))
				Directory.Delete (output, true);
			Directory.CreateDirectory (output);
		}

		protected override void Run ()
		{
			base.Run ();
			Compare ();
		}

		void Compare ()
		{
			bool compared = false;
			foreach (AssemblyDefinition assembly in Context.GetAssemblies ()) {
				if (Context.Annotations.GetAction (assembly) != AssemblyAction.Link)
					continue;

				string fileName = GetAssemblyFileName (assembly);

				var original = AssemblyDefinition.ReadAssembly (Path.Combine (GetTestCasePath (), fileName));
				var linked = AssemblyDefinition.ReadAssembly (Path.Combine (GetOutputPath (), fileName));
				compared = CompareAssemblies (original, linked);
			}

			Assert.IsTrue (compared, $"No data compared (are you missing '{TestCase}' namespace for the test case?)");
		}

		bool CompareAssemblies (AssemblyDefinition original, AssemblyDefinition linked)
		{
			bool compared = false;
			foreach (TypeDefinition originalType in original.MainModule.Types) {
				if (!originalType.FullName.StartsWith (TestCase, System.StringComparison.Ordinal))
					continue;

				compared = true;

				TypeDefinition linkedType = linked.MainModule.Types.FirstOrDefault (l => l.FullName == originalType.FullName);
				if (MustBeLinked (originalType)) {
					Assert.IsNull (linkedType, string.Format ("Type `{0}' was not linked", originalType));
					continue;
				}

				Assert.IsNotNull (linkedType, string.Format ("Type `{0}' was linked", originalType));
				CompareTypes (originalType, linkedType);
			}

			return compared;
		}

		static void CompareTypes (TypeDefinition type, TypeDefinition linkedType)
		{
			foreach (FieldDefinition originalField in type.Fields) {
				IEnumerable<FieldDefinition> fd = linkedType.Fields;
				FieldDefinition linkedField = fd.FirstOrDefault (l => l.Name == originalField.Name);// TODO: also get with the type!
				if (MustBeLinked (originalField)) {
					Assert.IsNull (linkedField, string.Format ("Field `{0}' should not have been linked", originalField));
					continue;
				}

				Assert.IsNotNull (linkedField, string.Format ("Field `{0}' should have been linked", originalField));
			}

			foreach (MethodDefinition originalMethod in type.Methods) {
				MethodDefinition linkedMethod = linkedType.Methods.FirstOrDefault (l => l.Name == originalMethod.Name && l.Parameters.Count == originalMethod.Parameters.Count); // TODO: lame
				if (MustBeLinked (originalMethod)) {
					Assert.IsNull (linkedMethod, string.Format ("Method `{0}' was not linked", originalMethod));
					continue;
				}

				Assert.IsNotNull (linkedMethod, string.Format ("Method `{0}' was linked", originalMethod));
			}
		}

		static bool MustBeLinked (ICustomAttributeProvider provider)
		{
			foreach (CustomAttribute ca in provider.CustomAttributes)
				if (ca.Constructor.DeclaringType.Name == "AssertLinkedAttribute")
					return true;

			return false;
		}
	}
}
