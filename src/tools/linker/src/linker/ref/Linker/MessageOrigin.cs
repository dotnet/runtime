// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
