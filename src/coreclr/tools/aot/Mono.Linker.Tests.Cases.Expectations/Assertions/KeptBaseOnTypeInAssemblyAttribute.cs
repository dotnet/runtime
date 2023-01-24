// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class KeptBaseOnTypeInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public KeptBaseOnTypeInAssemblyAttribute (string assemblyFileName, Type type, string baseAssemblyFileName, Type baseType)
		{
			ArgumentNullException.ThrowIfNull (type);
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);

			ArgumentException.ThrowIfNullOrEmpty (baseAssemblyFileName);
			ArgumentNullException.ThrowIfNull (baseType);
		}

		public KeptBaseOnTypeInAssemblyAttribute (string assemblyFileName, string typeName, string baseAssemblyFileName, string baseTypeName)
		{
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (typeName);

			ArgumentException.ThrowIfNullOrEmpty (baseAssemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (baseTypeName);
		}
	}
}
