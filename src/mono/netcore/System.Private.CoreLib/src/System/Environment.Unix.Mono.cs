// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Mono;

namespace System
{
	partial class Environment
	{
		private static Dictionary<string, string> s_environment;

		static string GetEnvironmentVariableCore (string variable)
		{
			Debug.Assert(variable != null);

			if (s_environment == null) {
				using (var h = RuntimeMarshal.MarshalString (variable)) {
					return internalGetEnvironmentVariable_native (h.Value);
				}
			}

			variable = TrimStringOnFirstZero (variable);
			lock (s_environment) {
				s_environment.TryGetValue (variable, out string value);
				return value;
			}
		}

		static unsafe void SetEnvironmentVariableCore (string variable, string? value)
		{
			Debug.Assert(variable != null);

			EnsureEnvironmentCached ();
			lock (s_environment) {
				variable = TrimStringOnFirstZero (variable);
				value = value == null ? null : TrimStringOnFirstZero (value);
				if (string.IsNullOrEmpty (value)) {
					s_environment.Remove (variable);
				} else {
					s_environment[variable] = value;
				}
			}
		}

		public static IDictionary GetEnvironmentVariables ()
		{
			var results = new Hashtable();

			EnsureEnvironmentCached();
			lock (s_environment) {
				foreach (var keyValuePair in s_environment) {
					results.Add(keyValuePair.Key, keyValuePair.Value);
				}
			}

			return results;
		}

		private static string TrimStringOnFirstZero (string value)
		{
			int index = value.IndexOf ('\0');
			if (index >= 0) {
				return value.Substring (0, index);
			}
			return value;
		}

		private static void EnsureEnvironmentCached ()
		{
			if (s_environment == null) {
				Interlocked.CompareExchange (ref s_environment, GetSystemEnvironmentVariables (), null);
			}
		}

		private static Dictionary<string, string> GetSystemEnvironmentVariables ()
		{
			var results = new Dictionary<string, string>();

			foreach (string name in GetEnvironmentVariableNames ()) {
				if (name != null) {
					using (var h = RuntimeMarshal.MarshalString (name)) {
						results.Add (name, internalGetEnvironmentVariable_native (h.Value));
					}
				}
			}

			return results;
		}


		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static string internalGetEnvironmentVariable_native (IntPtr variable);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private extern static string [] GetEnvironmentVariableNames ();
	}
}
