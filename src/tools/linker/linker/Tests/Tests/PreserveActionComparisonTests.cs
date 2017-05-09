using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class PreserveActionComparisonTests
	{
		[TestCase (TypePreserve.All, TypePreserve.All, TypePreserve.All)]
		[TestCase (TypePreserve.All, TypePreserve.Methods, TypePreserve.All)]
		[TestCase (TypePreserve.All, TypePreserve.Fields, TypePreserve.All)]
		[TestCase (TypePreserve.All, TypePreserve.Nothing, TypePreserve.All)]
		[TestCase (TypePreserve.Methods, TypePreserve.All, TypePreserve.All)]
		[TestCase (TypePreserve.Methods, TypePreserve.Methods, TypePreserve.Methods)]
		[TestCase (TypePreserve.Methods, TypePreserve.Fields, TypePreserve.All)]
		[TestCase (TypePreserve.Methods, TypePreserve.Nothing, TypePreserve.Methods)]
		[TestCase (TypePreserve.Fields, TypePreserve.All, TypePreserve.All)]
		[TestCase (TypePreserve.Fields, TypePreserve.Methods, TypePreserve.All)]
		[TestCase (TypePreserve.Fields, TypePreserve.Fields, TypePreserve.Fields)]
		[TestCase (TypePreserve.Fields, TypePreserve.Nothing, TypePreserve.Fields)]
		[TestCase (TypePreserve.Nothing, TypePreserve.All, TypePreserve.All)]
		[TestCase (TypePreserve.Nothing, TypePreserve.Methods, TypePreserve.Methods)]
		[TestCase (TypePreserve.Nothing, TypePreserve.Fields, TypePreserve.Fields)]
		[TestCase (TypePreserve.Nothing, TypePreserve.Nothing, TypePreserve.Nothing)]
		public void VerifyBehaviorOfChoosePreserveActionWhichPreservesTheMost (TypePreserve left, TypePreserve right, TypePreserve expected)
		{
			Assert.That (expected, Is.EqualTo (AnnotationStore.ChoosePreserveActionWhichPreservesTheMost (left, right)));
			Assert.That (expected, Is.EqualTo (AnnotationStore.ChoosePreserveActionWhichPreservesTheMost (right, left)));
		}
	}
}
