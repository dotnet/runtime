// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresOnVirtualsAndInterfaces
	{
		public static void Main ()
		{
			TestBaseTypeVirtualMethodRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequires ();
			TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ();
			TestTypeWhichOverridesVirtualPropertyRequires ();
			TestInterfaceMethodWithRequires ();
			TestCovariantReturnCallOnDerived ();
			CovariantReturnViaLdftn.Test ();
		}

		class BaseType
		{
			[RequiresUnreferencedCode ("Message for --BaseType.VirtualMethodRequires--")]
			[RequiresAssemblyFiles ("Message for --BaseType.VirtualMethodRequires--")]
			[RequiresDynamicCode ("Message for --BaseType.VirtualMethodRequires--")]
			public virtual void VirtualMethodRequires ()
			{
			}
		}

		class TypeWhichOverridesMethod : BaseType
		{
			[RequiresUnreferencedCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
			[RequiresAssemblyFiles ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
			[RequiresDynamicCode ("Message for --TypeWhichOverridesMethod.VirtualMethodRequires--")]
			public override void VirtualMethodRequires ()
			{
			}
		}

		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestBaseTypeVirtualMethodRequires ()
		{
			var tmp = new BaseType ();
			tmp.VirtualMethodRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequires")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequires ()
		{
			var tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequires ();
		}

		[LogDoesNotContain ("TypeWhichOverridesMethod.VirtualMethodRequires")]
		[ExpectedWarning ("IL2026", "--BaseType.VirtualMethodRequires--")]
		[ExpectedWarning ("IL3002", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--BaseType.VirtualMethodRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesMethodVirtualMethodRequiresOnBase ()
		{
			BaseType tmp = new TypeWhichOverridesMethod ();
			tmp.VirtualMethodRequires ();
		}

		class PropertyBaseType
		{
			public virtual int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				[RequiresDynamicCode ("Message for --PropertyBaseType.VirtualPropertyRequires--")]
				get;
			}
		}

		class TypeWhichOverridesProperty : PropertyBaseType
		{
			public override int VirtualPropertyRequires {
				[RequiresUnreferencedCode ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				[RequiresAssemblyFiles ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				[RequiresDynamicCode ("Message for --TypeWhichOverridesProperty.VirtualPropertyRequires--")]
				get { return 1; }
			}
		}

		[LogDoesNotContain ("TypeWhichOverridesProperty.VirtualPropertyRequires")]
		[ExpectedWarning ("IL2026", "--PropertyBaseType.VirtualPropertyRequires--")]
		[ExpectedWarning ("IL3002", "--PropertyBaseType.VirtualPropertyRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--PropertyBaseType.VirtualPropertyRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestTypeWhichOverridesVirtualPropertyRequires ()
		{
			var tmp = new TypeWhichOverridesProperty ();
			_ = tmp.VirtualPropertyRequires;
		}

		[LogDoesNotContain ("ImplementationClass.MethodWithRequires")]
		[ExpectedWarning ("IL2026", "--IRequires.MethodWithRequires--")]
		[ExpectedWarning ("IL3002", "--IRequires.MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--IRequires.MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestInterfaceMethodWithRequires ()
		{
			IRequires inst = new ImplementationClass ();
			inst.MethodWithRequires ();
		}

		class BaseReturnType { }
		class DerivedReturnType : BaseReturnType { }

		interface IRequires
		{
			[RequiresUnreferencedCode ("Message for --IRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("Message for --IRequires.MethodWithRequires--")]
			[RequiresDynamicCode ("Message for --IRequires.MethodWithRequires--")]
			public void MethodWithRequires ();
		}

		class ImplementationClass : IRequires
		{
			[RequiresUnreferencedCode ("Message for --ImplementationClass.RequiresMethod--")]
			[RequiresAssemblyFiles ("Message for --ImplementationClass.RequiresMethod--")]
			[RequiresDynamicCode ("Message for --ImplementationClass.RequiresMethod--")]
			public void MethodWithRequires ()
			{
			}
		}

		abstract class CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnBase.GetRequires--")]
			[RequiresAssemblyFiles ("Message for --CovariantReturnBase.GetRequires--")]
			[RequiresDynamicCode ("Message for --CovariantReturnBase.GetRequires--")]
			public abstract BaseReturnType GetRequires ();
		}

		class CovariantReturnDerived : CovariantReturnBase
		{
			[RequiresUnreferencedCode ("Message for --CovariantReturnDerived.GetRequires--")]
			[RequiresAssemblyFiles ("Message for --CovariantReturnDerived.GetRequires--")]
			[RequiresDynamicCode ("Message for --CovariantReturnDerived.GetRequires--")]
			public override DerivedReturnType GetRequires ()
			{
				return null;
			}
		}

		[LogDoesNotContain ("--CovariantReturnBase.GetRequires--")]
		[ExpectedWarning ("IL2026", "--CovariantReturnDerived.GetRequires--")]
		[ExpectedWarning ("IL3002", "--CovariantReturnDerived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
		[ExpectedWarning ("IL3050", "--CovariantReturnDerived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
		static void TestCovariantReturnCallOnDerived ()
		{
			var tmp = new CovariantReturnDerived ();
			tmp.GetRequires ();
		}

		class CovariantReturnViaLdftn
		{
			abstract class Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Base.GetRequires--")]
				[RequiresAssemblyFiles ("Message for --CovariantReturnViaLdftn.Base.GetRequires--")]
				[RequiresDynamicCode ("Message for --CovariantReturnViaLdftn.Base.GetRequires--")]
				public abstract BaseReturnType GetRequires ();
			}

			class Derived : Base
			{
				[RequiresUnreferencedCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequires--")]
				[RequiresAssemblyFiles ("Message for --CovariantReturnViaLdftn.Derived.GetRequires--")]
				[RequiresDynamicCode ("Message for --CovariantReturnViaLdftn.Derived.GetRequires--")]
				public override DerivedReturnType GetRequires ()
				{
					return null;
				}
			}

			[ExpectedWarning ("IL2026", "--CovariantReturnViaLdftn.Derived.GetRequires--")]
			[ExpectedWarning ("IL3002", "--CovariantReturnViaLdftn.Derived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--CovariantReturnViaLdftn.Derived.GetRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static void Test ()
			{
				var tmp = new Derived ();
				var _ = new Func<DerivedReturnType> (tmp.GetRequires);
			}
		}
	}
}
