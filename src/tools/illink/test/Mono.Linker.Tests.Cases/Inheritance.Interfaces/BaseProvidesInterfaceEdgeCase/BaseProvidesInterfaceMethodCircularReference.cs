// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.BaseProvidesInterfaceEdgeCase.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.BaseProvidesInterfaceEdgeCase
{
	/// <summary>
	/// Reproduces the issue found in https://github.com/dotnet/linker/issues/3112.
	/// <see cref="Derived1"/> derives from <see cref="Base"/> and uses <see cref="Base"/>'s method to implement <see cref="IFoo"/>, 
	/// creating a psuedo-circular assembly reference (but not quite since <see cref="Base"/> doesn't implement IFoo itself).
	/// In the linker, IsMethodNeededByInstantiatedTypeDueToPreservedScope would iterate through <see cref="Base"/>'s method's base methods,
	/// and in the process would trigger the assembly of <see cref="IFoo"/> to be processed. Since that assembly also has <see cref="Derived2"/> that 
	/// inherits from <see cref="Base"/> and implements <see cref="IBar"/> using <see cref="Base"/>'s methods, the linker adds 
	/// <see cref="IBar"/>'s method as a base to <see cref="Base"/>'s method, which modifies the collection as it's being iterated, causing an exception.
	/// </summary>
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/Base.cs" })] // Base Implements IFoo.Method (psuedo-reference to ifoo.dll)
	[SetupCompileBefore ("ifoo.dll", new[] { "Dependencies/IFoo.cs" }, references: new[] { "base.dll" })] // Derived2 references Base from base.dll (circular reference)
	[SetupCompileBefore ("derived1.dll", new[] { "Dependencies/Derived1.cs" }, references: new[] { "ifoo.dll", "base.dll" })]
	[KeptMemberInAssembly ("base.dll", typeof (Base), "Method()")]
	[RemovedMemberInAssembly ("ifoo", "Derived2")]
	public class BaseProvidesInterfaceMethodCircularReference
	{
		[Kept]
		public static void Main ()
		{
			_ = new Derived1 ();
			Foo ();
		}

		[Kept]
		public static void Foo ()
		{
			((IFoo) null).Method ();
		}
	}
}
