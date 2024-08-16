// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AbstractBaseWithNoMethodsImplementsInterface.il" })]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	class BaseProvidesInterfaceMethodRequiresMismatch
	{
		[RequiresUnreferencedCode ("Message")]
		public static void Main ()
		{
			BaseDoesNotImplementINoRuc.Run ();
			BaseDoesNotImplementIRuc.Run ();
			BaseImplementsINoRuc.Run ();
			ThreeLevelsInheritance.Run ();
			DerivedOverridesImplementingMethod.Run ();
#if IL_ASSEMBLY_AVAILABLE
			AbstractBaseWithNoMethodsImplementsInterface.Run ();
#endif
		}

		interface INoRuc
		{
			void M ();
		}

		interface IRuc
		{
			[RequiresUnreferencedCode ("Message")]
			void MRuc ();
		}

		public class BaseDoesNotImplementINoRuc
		{
			public class C
			{
				[RequiresUnreferencedCode ("Message")]
				public void M ()
				{
				}
			}

			[ExpectedWarning ("IL2046", "C.M()", "INoRuc.M()")]
			public class D : C, INoRuc
			{
			}

			public static void Run ()
			{
				((INoRuc) new D ()).M ();
			}
		}

		public class BaseDoesNotImplementIRuc
		{
			public class C
			{
				public void MRuc () { }
			}

			[ExpectedWarning ("IL2046", "C.MRuc()", "IRuc.MRuc()")]
			public class D : C, IRuc
			{
			}

			[RequiresUnreferencedCode ("Message")]
			public static void Run ()
			{
				((IRuc) new D ()).MRuc ();
			}
		}

		public class BaseImplementsINoRuc
		{
			class C : INoRuc
			{
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "C.M()", "INoRuc.M()")]
				public void M ()
				{
				}
			}

			[ExpectedWarning ("IL2046", "C.M()", "INoRuc.M()")]
			class D : C, INoRuc
			{
			}

			class D2 : C
			{
			}

			public static void Run ()
			{
				((INoRuc) new D ()).M ();
				((INoRuc) new D2 ()).M ();
			}
		}

		public class ThreeLevelsInheritance
		{
			public class Base
			{
				[RequiresUnreferencedCode ("Message")]
				public void M () { }
			}

			[ExpectedWarning ("IL2046", "Base.M()", "INoRuc.M()")]
			public class Middle : Base, INoRuc
			{
			}

			public class Derived : Middle
			{
			}

			public static void Run ()
			{
				((INoRuc) new Derived ()).M ();
			}
		}

		public class DerivedOverridesImplementingMethod
		{
			class C : INoRuc
			{
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "C.M()", "INoRuc.M()")]
				public virtual void M ()
				{
				}
			}

			class D : C, INoRuc
			{
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "D.M()", "INoRuc.M()")]
				public override void M () { }
			}

			class D2 : C
			{
				[RequiresUnreferencedCode ("Message")]
				public override void M () { }
			}

			public static void Run ()
			{
				((INoRuc) new D ()).M ();
				((INoRuc) new D2 ()).M ();
			}
		}
	}
}
