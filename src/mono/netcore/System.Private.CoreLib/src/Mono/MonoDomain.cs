// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mono
{
	[StructLayout (LayoutKind.Sequential)]
	internal sealed partial class MonoDomain
	{
        #pragma warning disable 169
        #region Sync with object-internals.h
		IntPtr _mono_app_domain;
		#endregion
        #pragma warning restore 169

		public event AssemblyLoadEventHandler AssemblyLoad;

		private void DoAssemblyLoad (Assembly assembly)
		{
			return; /* FIXME */
		}
		
		private Assembly? DoAssemblyResolve (string name, Assembly requestingAssembly, bool refonly)
		{
			return null;
		}

		internal Assembly? DoTypeResolve (string name)
		{
			return null;
		}

		internal Assembly? DoTypeBuilderResolve (System.Reflection.Emit.TypeBuilder tb)
		{
			return null;
		}

		public event UnhandledExceptionEventHandler UnhandledException;

		public event EventHandler ProcessExit;
	}
}
