// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
	partial class Stream
	{
		bool HasOverriddenBeginEndRead () => true;

		bool HasOverriddenBeginEndWrite () => true;
	}
}
