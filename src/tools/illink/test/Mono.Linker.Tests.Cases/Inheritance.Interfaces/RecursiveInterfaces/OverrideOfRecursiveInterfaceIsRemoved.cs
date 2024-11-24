// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/OverrideOfRecursiveInterfaceIsRemoved.il" })]
	[Kept]
	[KeptTypeInAssembly ("library.dll", "Program/A")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/A", "library.dll", "Program/IDerived")]
	[KeptTypeInAssembly ("library.dll", "Program/IDerived")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/IDerived", "library.dll", "Program/IMiddleUnused")]
	[KeptTypeInAssembly ("library.dll", "Program/IMiddleUnused")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/IMiddleUnused", "library.dll", "Program/IBaseUsed")]
	[KeptTypeInAssembly ("library.dll", "Program/IBaseUsed")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", "Program/IMiddleUnused", "library.dll", "Program/IBaseUnused")]
	[KeptTypeInAssembly ("library.dll", "Program/IBaseUnused")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "Program/A", "N", "System.Void Program/IDerived::N()")]
	[KeptOverrideOnMethodInAssembly ("library.dll", "Program/A", "M", "System.Void Program/IBaseUsed::M()")]
	[RemovedOverrideOnMethodInAssembly ("library.dll", "Program/A", "M", "System.Void Program/IBaseUnused::M()")]
	[RemovedOverrideOnMethodInAssembly ("library.dll", "Program/A", "O", "System.Void Program/IMiddleUnused::O()")]
	[RemovedMemberInAssembly ("library.dll", "Program/IBaseUnused", "M()")]
	[RemovedMemberInAssembly ("library.dll", "Program/IMiddleUnused", "O()")]
	public class OverrideOfRecursiveInterfaceIsRemoved
	{
		[Kept]
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Program.MyTest();
			_ = typeof(Program.IBaseUnused);
#endif
		}
	}
	//public class Program
	//{
	//    public static void MyTest()
	//    {
	//        UseNThroughIDerived<A>();
	//        A.M();
	//        A.O();
	//    }

	//    static void UseNThroughIDerived<T>() where T : IDerived  {
	//        T.N();
	//    }

	//    static void UseMThroughIDerived<T>() where T : IBaseUsed  {
	//        T.M();
	//    }

	//    interface IBaseUnused
	//    {
	//        static abstract void M();
	//    }

	//    interface IBaseUsed
	//    {
	//        static abstract void M();
	//    }

	//    interface IMiddleUnused : IBaseUnused, IBaseUsed
	//    {
	//        static abstract void O();
	//    }

	//    interface IDerived : IMiddleUnused
	//    {
	//        static abstract void N();
	//    }

	//    class A : IDerived {
	//        public static void M() {}
	//        public static void N() {}
	//        public static void O() {}
	//    }
	//}
}
