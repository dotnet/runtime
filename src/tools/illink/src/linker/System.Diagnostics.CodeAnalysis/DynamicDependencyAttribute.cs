// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
	/// This is an internal version of the attribute in the framework at https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/DynamicDependencyAttribute.cs
	/// We currently only use the type as a generic parameter, so the implementation isn't copied.
	internal sealed class DynamicDependencyAttribute : Attribute
	{
	}
}
