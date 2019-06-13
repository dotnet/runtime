// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mono;

namespace System
{
	partial class Environment
	{
		static string GetEnvironmentVariableCore (string variable)
		{
			using (var h = RuntimeMarshal.MarshalString (variable)) {
				return internalGetEnvironmentVariable_native (h.Value);
			}
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static string internalGetEnvironmentVariable_native (IntPtr variable);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string [] GetEnvironmentVariableNames ();

		public static IDictionary GetEnvironmentVariables ()
		{
			Hashtable vars = new Hashtable ();
			foreach (string name in GetEnvironmentVariableNames ()) {
				vars [name] = GetEnvironmentVariableCore (name);
			}
			return vars;
		}

		static unsafe void SetEnvironmentVariableCore (string variable, string value)
		{
			fixed (char *fixed_variable = variable)
			fixed (char *fixed_value = value)
				InternalSetEnvironmentVariable (fixed_variable, variable.Length, fixed_value, value?.Length ?? 0);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern unsafe void InternalSetEnvironmentVariable (char *variable, int variable_length, char *value, int value_length);
	}
}
