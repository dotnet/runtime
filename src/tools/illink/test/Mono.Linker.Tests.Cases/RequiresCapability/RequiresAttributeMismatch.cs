// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		// Base/Derived and Implementation/Interface differs between linker and analyzer https://github.com/dotnet/linker/issues/2533
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL2026", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualMethod()")]
		[ExpectedWarning ("IL2026", "DerivedClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "IBaseWithRequires.Method()")]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequiresInSource.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL2026", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
		[ExpectedWarning ("IL2026", "PropertyAnnotationInPropertyAndAccessor.set")]

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
			[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "DerivedClassWithRequires.VirtualMethod()", "BaseClassWithoutRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "DerivedClassWithRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "DerivedClassWithRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithoutRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInProperty { get; set; }
		}

		class DerivedClassWithoutRequires : BaseClassWithRequires
		{
			[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "DerivedClassWithoutRequires.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public override void VirtualMethod ()
			{
			}

			private string name;
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInProperty { get; set; }

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInPropertyAndAccessor", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInPropertyAndAccessor {
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithoutRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				set;
			}
		}

		class DerivedClassWithAllWarnings : BaseClassWithRequires
		{
			[ExpectedWarning ("IL2046", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()")]
			[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "DerivedClassWithAllWarnings.VirtualMethod()", "BaseClassWithRequires.VirtualMethod()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public override void VirtualMethod ()
			{
			}

			private string name;

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get { return name; }
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInAccesor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.set")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInAccesor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInAccesor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInProperty {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInProperty.get", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.get", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInProperty.set", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInProperty.set", "BaseClassWithRequires.VirtualPropertyAnnotationInProperty.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				set;
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			public override string VirtualPropertyAnnotationInPropertyAndAccessor {
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor.get", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "VirtualPropertyAnnotationInPropertyAndAccessor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set")]
				[ExpectedWarning ("IL3003", "DerivedClassWithAllWarnings.VirtualPropertyAnnotationInPropertyAndAccessor.set", "BaseClassWithRequires.VirtualPropertyAnnotationInPropertyAndAccessor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
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
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "ImplementationClassWithRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "ImplementationClassWithRequires.PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				set;
			}
		}

		class ExplicitImplementationClassWithRequires : IBaseWithoutRequires
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			// Linker member string format includes namespace of explicit interface method.
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.NativeAot)]
			[ExpectedWarning ("IL2046", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", "IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3051", "IBaseWithoutRequires.Method()", "ExplicitImplementationClassWithRequires.IBaseWithoutRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			void IBaseWithoutRequires.Method ()
			{
			}

			private string name;
			string IBaseWithoutRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "PropertyAnnotationInAccesor.get", "IBaseWithoutRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithoutRequires.PropertyAnnotationInProperty", "IBaseWithoutRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithoutRequires.PropertyAnnotationInProperty { get; set; }

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[RequiresAssemblyFiles ("Message")]
			[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				set;
			}
		}

		class ImplementationClassWithoutRequires : IBaseWithRequires
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequires.Method()", "IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequires.PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInPropertyAndAccessor {
				[RequiresAssemblyFiles ("Message")]
				[RequiresUnreferencedCode ("Message")]
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.get", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get;
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequires.PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				set;
			}
		}

		class ExplicitImplementationClassWithoutRequires : IBaseWithRequires
		{
			// Linker member string format includes namespace of explicit interface method.
			[ExpectedWarning ("IL2046", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3003", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", ProducedBy = ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.Method()", ProducedBy = ProducedBy.NativeAot)]
			[ExpectedWarning ("IL2046", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3003", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3051", "IBaseWithRequires.Method()", "ExplicitImplementationClassWithoutRequires.IBaseWithRequires.Method()", ProducedBy = ProducedBy.Analyzer)]
			void IBaseWithRequires.Method ()
			{
			}

			private string name;
			string IBaseWithRequires.PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "PropertyAnnotationInAccesor.get", "IBaseWithRequires.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInProperty", "IBaseWithRequires.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithRequires.PropertyAnnotationInProperty { get; set; }

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ExplicitImplementationClassWithoutRequires.Mono.Linker.Tests.Cases.RequiresCapability.RequiresAttributeMismatch.IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor", ProducedBy = ProducedBy.Analyzer)]
			string IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor {
				get;
				[ExpectedWarning ("IL2046", "PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set")]
				[ExpectedWarning ("IL3003", "PropertyAnnotationInPropertyAndAccessor.set", "IBaseWithRequires.PropertyAnnotationInPropertyAndAccessor.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				set;
			}
		}

		class ImplementationClassWithoutRequiresInSource : ReferenceInterfaces.IBaseWithRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequiresInSource.Method()", "IBaseWithRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ImplementationClassWithoutRequiresInSource.PropertyAnnotationInProperty", "IBaseWithRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			public string PropertyAnnotationInProperty { get; set; }
		}

		class ImplementationClassWithRequiresInSource : ReferenceInterfaces.IBaseWithoutRequiresInReference
		{
			[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()")]
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[ExpectedWarning ("IL3051", "ImplementationClassWithRequiresInSource.Method()", "IBaseWithoutRequiresInReference.Method()", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			public void Method ()
			{
			}

			private string name;
			public string PropertyAnnotationInAccesor {
				[ExpectedWarning ("IL2046", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get")]
				[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL3051", "ImplementationClassWithRequiresInSource.PropertyAnnotationInAccesor.get", "IBaseWithoutRequiresInReference.PropertyAnnotationInAccesor.get", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get { return name; }
				set { name = value; }
			}

			// NativeAOT does not look at associated Property/Event to produce warnings
			// https://github.com/dotnet/runtime/issues/71985
			[ExpectedWarning ("IL3003", "ImplementationClassWithRequiresInSource.PropertyAnnotationInProperty", "IBaseWithoutRequiresInReference.PropertyAnnotationInProperty", ProducedBy = ProducedBy.Analyzer)]
			[RequiresAssemblyFiles ("Message")]
			public string PropertyAnnotationInProperty { get; set; }
		}
	}
}
