using Mono.Cecil;
using NUnit.Framework;

namespace Mono.Linker.Tests.Tests
{
	public class AnnotationStoreTest
	{
		AnnotationStore store;

		[SetUp]
		public void Setup ()
		{
			var ctx = new LinkContext (null, new ConsoleLogger (), string.Empty);
			store = new AnnotationStore (ctx);
		}

		[Test]
		public void CustomAnnotations ()
		{
			var td = new TypeDefinition ("ns", "name", TypeAttributes.Public);

			Assert.IsNull (store.GetCustomAnnotation ("k", td));

			store.SetCustomAnnotation ("k", td, "value");
			Assert.AreEqual ("value", store.GetCustomAnnotation ("k", td));
		}
	}
}
