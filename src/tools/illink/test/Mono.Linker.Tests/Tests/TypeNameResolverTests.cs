// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Runtime.TypeParsing;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class TypeNameResolverTests
	{
		[Test]
		public void TryParseAssemblyQualifiedTypeName_Null ()
		{
			Assert.IsNull (TypeParser.ParseTypeName (null));
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_FullyQualified ()
		{
			var value = typeof (TypeNameResolverTests).AssemblyQualifiedName;
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsTrue (typeName is AssemblyQualifiedTypeName);
			AssemblyQualifiedTypeName assemblyQualifiedTypeName = (AssemblyQualifiedTypeName) typeName;
			Assert.AreEqual (assemblyQualifiedTypeName.AssemblyName.FullName, typeof (TypeNameResolverTests).Assembly.FullName);
			Assert.AreEqual (assemblyQualifiedTypeName.TypeName.ToString (), typeof (TypeNameResolverTests).FullName);
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (TypeNameResolverTests).FullName}, {typeof (TypeNameResolverTests).Assembly.GetName ().Name}";
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsTrue (typeName is AssemblyQualifiedTypeName);
			AssemblyQualifiedTypeName assemblyQualifiedTypeName = (AssemblyQualifiedTypeName) typeName;
			Assert.AreEqual (assemblyQualifiedTypeName.AssemblyName.Name, typeof (TypeNameResolverTests).Assembly.GetName ().Name);
			Assert.AreEqual (assemblyQualifiedTypeName.TypeName.ToString (), typeof (TypeNameResolverTests).FullName);
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_NameOnly ()
		{
			var value = typeof (TypeNameResolverTests).FullName;
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsFalse (typeName is AssemblyQualifiedTypeName);
			Assert.AreEqual (typeName.ToString (), value);
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_GenericType_FullyQualified ()
		{
			var value = typeof (GenericType<,>).AssemblyQualifiedName;
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsTrue (typeName is AssemblyQualifiedTypeName);
			AssemblyQualifiedTypeName assemblyQualifiedTypeName = (AssemblyQualifiedTypeName) typeName;
			Assert.AreEqual (assemblyQualifiedTypeName.AssemblyName.Name, typeof (TypeNameResolverTests).Assembly.GetName ().Name);
			Assert.AreEqual (assemblyQualifiedTypeName.TypeName.ToString (), $"{typeof (TypeNameResolverTests).FullName}/GenericType`2");
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_GenericType_NameAndAssemblyOnly ()
		{
			var value = $"{typeof (GenericType<,>).FullName}, {typeof (TypeNameResolverTests).Assembly.GetName ().Name}";
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsTrue (typeName is AssemblyQualifiedTypeName);
			AssemblyQualifiedTypeName assemblyQualifiedTypeName = (AssemblyQualifiedTypeName) typeName;
			Assert.AreEqual (assemblyQualifiedTypeName.AssemblyName.Name, typeof (TypeNameResolverTests).Assembly.GetName ().Name);
			Assert.AreEqual (assemblyQualifiedTypeName.TypeName.ToString (), $"{typeof (TypeNameResolverTests).FullName}/GenericType`2");
		}

		[Test]
		public void TryParseAssemblyQualifiedTypeName_GenericType_NameOnly ()
		{
			var value = typeof (GenericType<,>).FullName;
			var typeName = TypeParser.ParseTypeName (value);
			Assert.IsFalse (typeName is AssemblyQualifiedTypeName);
			Assert.AreEqual (typeName.ToString (), $"{typeof (TypeNameResolverTests).FullName}/GenericType`2");
		}

		[Test]
		public void MissingTypeName ()
		{
			Assert.IsNull (TypeParser.ParseTypeName (", System"));
		}

		sealed class GenericType<T1, T2> { }
	}
}
