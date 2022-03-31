// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared.TypeSystemProxy
{
	interface IMemberProxy
	{
		public string Name { get; }

		public string GetDisplayName ();
	}
}
