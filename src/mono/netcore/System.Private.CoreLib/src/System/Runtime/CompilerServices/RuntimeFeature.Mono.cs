// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
	partial class RuntimeFeature
	{
		public static bool IsDynamicCodeSupported
		{
			[Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
			get { throw new NotImplementedException (); }
		}

		public static bool IsDynamicCodeCompiled
		{
			[Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
			get { throw new NotImplementedException (); }
		}
	}
}
