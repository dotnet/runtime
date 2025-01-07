// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib1")]
	[SetupCompileBefore ("lib1.dll", new[] { "Dependencies/ReferenceInterfaces.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib1.dll")]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class RequiresAttributeMismatch
	{
		// General comment about IL3003
		// NativeAOT doesn't really see properties and events, only methods. So it is much easier to handle everything
		// on the method level. While it's a discrepancy in behavior, it's small and should have no adverse effects
		// as suppressing the warning on the property level will suppress it for the accessors as well.
		// So NativeAOT will report all mismatches on the accessors. If the mismatch is on the property
		// then both accessors will report IL3003 (the attribute on the property is treated as if it was specified
		// on all accessors, always).
		// The analyzer matches this behavior, treating the get/set methods as annotated if the property is annotated,
		// and warning only on the get/set methods.
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.set")]
		[ExpectedWarning ("IL3002", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.get")]
		[ExpectedWarning ("IL3002", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.set")]
		[ExpectedWarning ("IL3002", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor.set")]
		[ExpectedWarning ("IL3002", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "BaseClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "DerivedClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL3002", "DerivedClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "DerivedClassWithRequires.VirtualMethod()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.PropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.PropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.Method()")]
		[ExpectedWarning ("IL3002", "IBaseWithRequires.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "IBaseWithRequires.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.Method()")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "ImplementationClassWithRequires.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.PropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequires.PropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequiresInSource.Method()")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequiresInSource.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "ImplementationClassWithRequiresInSource.Method()", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3050", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set", Tool.NativeAot, "")]
		[ExpectedWarning ("IL3002", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set", Tool.NativeAot, "")]

		public static void Main ()
		{
			typeof (BaseClassWithRequires).RequiresPublicMethods ();
			typeof (BaseClassWithoutRequires).RequiresPublicMethods ();
			typeof (DerivedClassWithRequires).RequiresPublicMethods ();
			typeof (DerivedClassWithoutRequires).RequiresPublicMethods ();
			typeof (DerivedClassWithAllWarnings).RequiresPublicMethods ();
			typeof (IBaseWithRequires).RequiresPublicMethods ();
			typeof (IBaseWithoutRequires).RequiresPublicMethods ();
			typeof (ImplementationClassWithRequires).RequiresPublicMethods ();
			typeof (ImplementationClassWithoutRequires).RequiresPublicMethods ();
			typeof (ExplicitImplementationClassWithRequires).RequiresPublicMethods ();
			typeof (ExplicitImplementationClassWithoutRequires).RequiresPublicMethods ();
			typeof (ImplementationClassWithoutRequiresInSource).RequiresPublicMethods ();
			typeof (ImplementationClassWithRequiresInSource).RequiresPublicMethods ();
			typeof (StaticInterfaceMethods).RequiresPublicMethods ();

			DerivedClassWithRequiresOnRequires.Test ();
		}

		class BaseClassWithRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			public virtual void VirtualMethod ()
			{
			}

			public virtual string VirtualPropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles ("Message")]
			public virtual string VirtualPropertyAnnotationInProperty { get; set; }

			[RequiresAssemblyFiles ("Message")]
			public virtual string VirtualPropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				get;
				set;
			}
		}

		class BaseClassWithoutRequires
		{
			public virtual void VirtualMethod ()
			{
			}

			public virtual string VirtualPropertyAnnotationInAccesor { get; set; }

			public virtual string VirtualPropertyAnnotationInProperty { get; set; }
		}

		class DerivedClassWithRequires : BaseClassWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles ("Message")]
			public override string VirtualPropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithoutRequires.VirtualPropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithoutRequires.VirtualPropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class DerivedClassWithRequiresOnRequires : BaseClassWithRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles ("Message")]
			public override string VirtualPropertyAnnotationInProperty {
				get;
				set;
			}

			[UnconditionalSuppressMessage ("test", "IL2026")]
			[UnconditionalSuppressMessage ("test", "IL3002")]
			[UnconditionalSuppressMessage ("test", "IL3050")]
			public static void Test()
			{
				typeof (DerivedClassWithRequiresOnRequires).RequiresPublicMethods ();
			}
		}

		class DerivedClassWithoutRequires : BaseClassWithRequires
		{
			[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get { return name; }
				set { name = value; }
			}

			public override string VirtualPropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}

			public override string VirtualPropertyAnnotationInPropertyAndAccessor {
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInPropertyAndAccessor", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class DerivedClassWithAllWarnings : BaseClassWithRequires
		{
			[ExpectedWarning ("IL2046", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", Tool.Analyzer | Tool.NativeAot, "")]
			public override void VirtualMethod ()
			{
			}

			private string name;

			[RequiresAssemblyFiles ("Message")]
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3051", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get { return name; }
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInAccesor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.set")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.set", Tool.Analyzer | Tool.NativeAot, "")]
				set { name = value; }
			}

			public override string VirtualPropertyAnnotationInProperty {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInProperty.get", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get")]
				get;
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInProperty.set", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set")]
				set;
			}

			public override string VirtualPropertyAnnotationInPropertyAndAccessor {
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set")]
				set;
			}
		}

		public interface IBaseWithRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			void Method ();

			string PropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles ("Message")]
			string PropertyAnnotationInProperty { get; set; }

			[RequiresAssemblyFiles ("Message")]
			string PropertyAnnotationInPropertyAndAccessor {
				get;
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				set;
			}
		}

		public interface IBaseWithoutRequires
		{
			void Method ();

			string PropertyAnnotationInAccesor { get; set; }

			string PropertyAnnotationInProperty { get; set; }

			string PropertyAnnotationInPropertyAndAccessor { get; set; }
		}

		class ImplementationClassWithRequires : IBaseWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			public string PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.NativeAot | Tool.Analyzer, "")]
				get;
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.NativeAot | Tool.Analyzer, "")]
				set;
			}

			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer, "")]
			public string PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class ExplicitImplementationClassWithRequires : IBaseWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			// ILLink member string format includes namespace of explicit interface method.
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", Tool.Trimmer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", Tool.Analyzer, "")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", Tool.Analyzer, "")]
			[ExpectedWarning ("IL3051", "IBaseWithoutRequires.Method()", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", Tool.Analyzer, "")]
			void IBaseWithoutRequires.Method ()
			{
			}

			private string name;
			string IBaseWithoutRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			string IBaseWithoutRequires.PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}

			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer, "")]
			string IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class ImplementationClassWithoutRequires : IBaseWithRequires
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			public string PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer, "")]
			public string PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				get;
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class ExplicitImplementationClassWithoutRequires : IBaseWithRequires
		{
			// ILLink member string format includes namespace of explicit interface method.
			[ExpectedWarning ("IL2046", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", Tool.Trimmer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3003", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2046", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", Tool.Analyzer, "")]
			[ExpectedWarning ("IL3003", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", Tool.Analyzer, "")]
			[ExpectedWarning ("IL3051", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", Tool.Analyzer, "")]
			void IBaseWithRequires.Method ()
			{
			}

			private string name;
			string IBaseWithRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			string IBaseWithRequires.PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}

			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer, "")]
			string IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor {
				[ExpectedWarning ("IL3003", "Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL2046", "PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class ImplementationClassWithoutRequiresInSource : ReferenceInterfaces.IBaseWithRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty", "IBaseWithRequiresInReference.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			public string PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty.get", "IBaseWithRequiresInReference.PropertyAnnotationInProperty.get", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty.set", "IBaseWithRequiresInReference.PropertyAnnotationInProperty.set", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class ImplementationClassWithRequiresInSource : ReferenceInterfaces.IBaseWithoutRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			[ExpectedWarning ("IL3051", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", Tool.Analyzer | Tool.NativeAot, "")]
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", Tool.Analyzer, "")]
			[RequiresAssemblyFiles ("Message")]
			public string PropertyAnnotationInProperty {
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				get;
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", Tool.Analyzer | Tool.NativeAot, "")]
				set;
			}
		}

		class StaticInterfaceMethods
		{
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", Tool.NativeAot, "")]
			[ExpectedWarning ("IL3050", Tool.NativeAot, "")]
			public static void Test ()
			{
				typeof (IRequires).RequiresPublicMethods ();
				typeof (INoRequires).RequiresPublicMethods ();
				typeof (ImplINoRequiresMatching).RequiresPublicMethods ();
				typeof (ImplINoRequiresMismatching).RequiresPublicMethods ();
				typeof (ImplIRequiresMismatching).RequiresPublicMethods ();
				typeof (ImplIRequiresMatching).RequiresPublicMethods ();
			}

			interface IRequires
			{
				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.IRequires.VirtualMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.IRequires.VirtualMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.IRequires.VirtualMethod--")]
				static virtual void VirtualMethod () { }
				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.IRequires.AbstractMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.IRequires.AbstractMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.IRequires.AbstractMethod--")]
				static abstract void AbstractMethod ();
			}

			interface INoRequires
			{
				static virtual void VirtualMethod () { }
				static abstract void AbstractMethod ();
			}

			class ImplIRequiresMatching : IRequires
			{
				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.VirtualMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.VirtualMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.VirtualMethod--")]
				public static void VirtualMethod () { }

				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.AbstractMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.AbstractMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.ImplIRequiresMatching.AbstractMethod--")]
				public static void AbstractMethod () { }
			}

			class ImplIRequiresMismatching : IRequires
			{
				[ExpectedWarning ("IL2046", "ImplIRequiresMismatching.VirtualMethod", "IRequires.VirtualMethod")]
				[ExpectedWarning ("IL3003", "ImplIRequiresMismatching.VirtualMethod", "IRequires.VirtualMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplIRequiresMismatching.VirtualMethod", "IRequires.VirtualMethod", Tool.Analyzer | Tool.NativeAot, "")]
				public static void VirtualMethod () { }

				[ExpectedWarning ("IL2046", "ImplIRequiresMismatching.AbstractMethod", "IRequires.AbstractMethod")]
				[ExpectedWarning ("IL3003", "ImplIRequiresMismatching.AbstractMethod", "IRequires.AbstractMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplIRequiresMismatching.AbstractMethod", "IRequires.AbstractMethod", Tool.Analyzer | Tool.NativeAot, "")]
				public static void AbstractMethod () { }
			}
			class ImplINoRequiresMatching : INoRequires
			{
				public static void VirtualMethod () { }

				public static void AbstractMethod () { }
			}

			class ImplINoRequiresMismatching : INoRequires
			{
				[ExpectedWarning ("IL2046", "ImplINoRequiresMismatching.VirtualMethod", "INoRequires.VirtualMethod")]
				[ExpectedWarning ("IL3003", "ImplINoRequiresMismatching.VirtualMethod", "INoRequires.VirtualMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplINoRequiresMismatching.VirtualMethod", "INoRequires.VirtualMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.VirtualMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.VirtualMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.VirtualMethod--")]
				public static void VirtualMethod () { }

				[ExpectedWarning ("IL2046", "ImplINoRequiresMismatching.AbstractMethod", "INoRequires.AbstractMethod")]
				[ExpectedWarning ("IL3003", "ImplINoRequiresMismatching.AbstractMethod", "INoRequires.AbstractMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[ExpectedWarning ("IL3051", "ImplINoRequiresMismatching.AbstractMethod", "INoRequires.AbstractMethod", Tool.Analyzer | Tool.NativeAot, "")]
				[RequiresUnreferencedCode ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.AbstractMethod--")]
				[RequiresAssemblyFiles ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.AbstractMethod--")]
				[RequiresDynamicCode ("Message for --StaticInterfaceMethods.ImplINoRequiresMatching.AbstractMethod--")]
				public static void AbstractMethod () { }
			}
		}
	}
}
