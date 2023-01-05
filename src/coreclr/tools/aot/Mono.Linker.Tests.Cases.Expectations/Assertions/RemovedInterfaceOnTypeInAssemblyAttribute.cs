// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RemovedInterfaceOnTypeInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public RemovedInterfaceOnTypeInAssemblyAttribute (string assemblyFileName, Type type, string interfaceAssemblyFileName, Type interfaceType)
		{
			ArgumentNullException.ThrowIfNull (type);
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);

			ArgumentException.ThrowIfNullOrEmpty (interfaceAssemblyFileName);
			ArgumentNullException.ThrowIfNull (interfaceType);
		}

		public RemovedInterfaceOnTypeInAssemblyAttribute (string assemblyFileName, string typeName, string interfaceAssemblyFileName, string interfaceTypeName)
		{
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (typeName);

			ArgumentException.ThrowIfNullOrEmpty (interfaceAssemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (interfaceTypeName);
		}
	}
}
