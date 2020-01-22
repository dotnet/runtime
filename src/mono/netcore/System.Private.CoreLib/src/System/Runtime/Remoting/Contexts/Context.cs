// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.Remoting.Contexts {

	[StructLayout (LayoutKind.Sequential)]
	/* FIXME: Mono: this was public in mscorlib */
	internal class Context 
	{
#pragma warning disable 169, 414
		#region Sync with domain-internals.h
		int domain_id;
		int context_id;
		UIntPtr static_data; /* GC-tracked */
		UIntPtr data;
		#endregion
#pragma warning restore 169, 414

		[MethodImpl (MethodImplOptions.InternalCall)]
		extern static void RegisterContext (Context ctx);

		[MethodImpl (MethodImplOptions.InternalCall)]
		extern static void ReleaseContext (Context ctx);
		
		public Context ()
		{
#if false
			domain_id = Thread.GetDomainID();
			context_id = Interlocked.Increment (ref global_count);

			RegisterContext (this);
#endif
		}

		~Context ()
		{
			ReleaseContext (this);
		}
	}
}
