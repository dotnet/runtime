using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("TypeWithPreserveFieldsHasBackingFieldsOfPropertiesRemoved.xml")]
	class TypeWithPreserveFieldsHasBackingFieldsOfPropertiesRemoved
	{
		public static void Main ()
		{
		}

		[Kept]
		class Unused : IFoo<int>, IFoo<string>, IFoo<Cat>, IFoo2<int>, IFoo3<int, string, char>, IDog, IFoo<IFoo<int>>
		{
			[Kept]
			public int Field1;

			[Kept]
			public IFoo<int> Field2;

			public IFoo<int> Property1 { get; set; }

			string IDog.Name { get; set; }

			int IFoo<int>.Bar { get; set; }

			int IFoo<string>.Bar { get; set; }

			int IFoo<Cat>.Bar { get; set; }

			int Bar2 { get; set; }

			int IFoo2<int>.Bar2 { get; set; }

			int Bar3 { get; set; }

			int IFoo3<int, string, char>.Bar3 { get; set; }

			int IFoo<IFoo<int>>.Bar { get; set; }
		}

		interface IDog
		{
			string Name { get; set; }
		}

		[Kept]
		interface IFoo<T>
		{

			int Bar { get; set; }
		}

		interface IFoo2<T>
		{
			int Bar2 { get; set; }
		}

		interface IFoo3<T, K, J>
		{
			int Bar3 { get; set; }
		}

		class Cat
		{
		}
	}
}
