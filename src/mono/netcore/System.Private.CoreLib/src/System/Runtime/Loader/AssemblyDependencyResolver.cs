// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Loader
{
	public sealed class AssemblyDependencyResolver
	{
		public AssemblyDependencyResolver (string componentAssemblyPath)
		{
		}

		public string ResolveAssemblyToPath (System.Reflection.AssemblyName assemblyName)
		{
			throw new NotImplementedException ();
		}

		public string ResolveUnmanagedDllToPath (string unmanagedDllName)
		{
			throw new NotImplementedException ();
		}		
	}
}
