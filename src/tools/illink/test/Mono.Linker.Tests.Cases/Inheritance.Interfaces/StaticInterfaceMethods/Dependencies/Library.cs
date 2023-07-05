// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.Dependencies
{
	public interface IStaticInterfaceWithDefaultImpls
	{
		static virtual int Property { get => 0; set => _ = value; }
		static virtual int Method () => 0;
		virtual int InstanceMethod () => 0;
	}

	public interface IStaticAbstractMethods
	{
		static abstract int Property { get; set; }
		static abstract int Method ();
		int InstanceMethod ();
	}
}
