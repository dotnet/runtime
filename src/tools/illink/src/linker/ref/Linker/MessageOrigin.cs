// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{
	public readonly struct MessageOrigin
	{
		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int? ilOffset = null)
		{
		}
	}
}
