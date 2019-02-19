using NUnit.Framework;

namespace Mono.Linker.Tests {
	[TestFixture]
	public class TypeNameParserTests {
		[Test]
		public void TryParseTypeAssemblyQualifiedName_Null ()
		{
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (null, out string typeName, out string assemblyName), Is.False);
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_FullyQualified ()
		{
			var value = typeof (TypeNameParserTests).AssemblyQualifiedName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo (typeof (TypeNameParserTests).FullName));
			Assert.That (assemblyName, Is.EqualTo (typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (TypeNameParserTests).FullName}, {typeof (TypeNameParserTests).Assembly.GetName ().Name}";
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo (typeof (TypeNameParserTests).FullName));
			Assert.That (assemblyName, Is.EqualTo (typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_NameOnly ()
		{
			var value = typeof (TypeNameParserTests).FullName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo (typeof (TypeNameParserTests).FullName));
			Assert.That (assemblyName, Is.Null);
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_GenericType_FullyQualified ()
		{
			var value = typeof (SampleGenericType<,>).AssemblyQualifiedName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo($"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2"));
			Assert.That (assemblyName, Is.EqualTo(typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_GenericType_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (SampleGenericType<,>).FullName}, {typeof (TypeNameParserTests).Assembly.GetName ().Name}";
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo ($"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2"));
			Assert.That (assemblyName, Is.EqualTo (typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_GenericType_NameOnly ()
		{
			var value = typeof (SampleGenericType<,>).FullName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo ($"{typeof (TypeNameParserTests).FullName}/SampleGenericType`2"));
			Assert.That (assemblyName, Is.Null);
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_NestedType_FullyQualified ()
		{
			var value = typeof (SampleNestedType).AssemblyQualifiedName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo($"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}"));
			Assert.That (assemblyName, Is.EqualTo(typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_NestedType_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (SampleNestedType).FullName}, {typeof (TypeNameParserTests).Assembly.GetName().Name}";
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo ($"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}"));
			Assert.That (assemblyName, Is.EqualTo (typeof (TypeNameParserTests).Assembly.GetName ().Name));
		}
		
		[Test]
		public void TryParseTypeAssemblyQualifiedName_NestedType_NameOnly ()
		{
			var value = typeof (SampleNestedType).FullName;
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (value, out string typeName, out string assemblyName), Is.True);
			Assert.That (typeName, Is.EqualTo ($"{typeof (TypeNameParserTests).FullName}/{nameof (SampleNestedType)}"));
			Assert.That (assemblyName, Is.Null);
		}

		[Test]
		public void MissingTypeName ()
		{
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (", System", out string typeName, out string assemblyName), Is.False);
			Assert.That (typeName, Is.Null);
			Assert.That (assemblyName, Is.Null);
		}

		
		[TestCase ("A[]][")]
		[TestCase ("A][")]
		[TestCase ("A[")]
		[TestCase (",    ,    ")]
		[TestCase (", , , ")]
		[TestCase (", , , , ")]
		public void InvalidValues (string name)
		{
			Assert.That (TypeNameParser.TryParseTypeAssemblyQualifiedName (name, out string typeName, out string assemblyName), Is.False);
			Assert.That (typeName, Is.Null);
			Assert.That (assemblyName, Is.Null);
		}

		class SampleNestedType {
		}

		class SampleGenericType<T1, T2> {
		}
	}
}