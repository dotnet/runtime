// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata;

/// <summary>
/// Deletes a file before the linker is ran
/// </summary>
[AttributeUsage (AttributeTargets.Class)]
public class DeleteBeforeAttribute : BaseMetadataAttribute
{
	public DeleteBeforeAttribute (string fileName)
	{
	}
}
