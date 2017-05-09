

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	class TypeWithPreserveFieldsHasBackingFieldsOfPropertiesRemoved {
		public static void Main () {
		}

		[Kept]
		[KeptInterface(typeof (IFoo<System.Int32>))]
		[KeptInterface(typeof (IFoo<System.String>))]
		[KeptInterface(typeof (IFoo<Cat>))]
		[KeptInterface(typeof (IFoo2<System.Int32>))]
		[KeptInterface(typeof (IFoo3<System.Int32,System.String,System.Char>))]
		[KeptInterface(typeof (IDog))]
		[KeptInterface(typeof (IFoo<IFoo<System.Int32>>))]
		class Unused : IFoo<int>, IFoo<string>, IFoo<Cat>, IFoo2<int>, IFoo3<int, string, char>, IDog, IFoo<IFoo<int>> {
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

		[Kept]
		interface IDog {
			string Name { get; set; }
		}

		[Kept]
		interface IFoo<T> {

			int Bar { get; set; }
		}

		[Kept]
		interface IFoo2<T> {
			int Bar2 { get; set; }
		}

		[Kept]
		interface IFoo3<T, K, J> {
			int Bar3 { get; set; }
		}

		[Kept]
		class Cat {
		}
	}
}
