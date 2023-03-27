// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.RequiresCapability.Dependencies
{
	public class ReferenceInterfaces
	{
		public interface IBaseWithRequiresInReference
		{
			[RequiresUnreferencedCode ("Message")]
			[RequiresAssemblyFiles ("Message")]
			[RequiresDynamicCode ("Message")]
			public void Method ();

			public string PropertyAnnotationInAccesor {
				[RequiresUnreferencedCode ("Message")]
				[RequiresAssemblyFiles ("Message")]
				[RequiresDynamicCode ("Message")]
				get;
				set;
			}

			[RequiresAssemblyFiles ("Message")]
			public string PropertyAnnotationInProperty { get; set; }
		}

		public interface IBaseWithoutRequiresInReference
		{
			public void Method ();

			public string PropertyAnnotationInAccesor {
				get;
				set;
			}

			public string PropertyAnnotationInProperty { get; set; }
		}
	}
}
