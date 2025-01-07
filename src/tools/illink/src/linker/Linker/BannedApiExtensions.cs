// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker;

/// <summary>
/// Extension methods to help make working with banned apis easier and more discoverable
/// </summary>
public static class BannedApiExtensions
{
	public static Collection<Instruction> Instructions (this MethodBody body, LinkContext context)
		=> context.GetMethodIL(body.Method).Instructions;

	public static Collection<ExceptionHandler> ExceptionHandlers (this MethodBody body, LinkContext context)
		=> context.GetMethodIL (body.Method).ExceptionHandlers;

	public static Collection<VariableDefinition> Variables (this MethodBody body, LinkContext context)
		=> context.GetMethodIL(body.Method).Variables;

	public static MethodIL GetMethodIL (this MethodDefinition method, LinkContext context)
		=> context.GetMethodIL (method);

	public static MethodIL GetMethodIL (this MethodBody body, LinkContext context)
		=> context.GetMethodIL (body);

	public static MethodDefinition? Resolve (this MethodReference method, LinkContext context)
		=> context.Resolve (method);

	public static MethodDefinition? TryResolve (this MethodReference method, LinkContext context)
		=> context.TryResolve (method);

	public static TypeDefinition? Resolve (this TypeReference type, LinkContext context)
		=> context.Resolve (type);

	public static TypeDefinition? TryResolve (this TypeReference type, LinkContext context)
		=> context.TryResolve (type);

	public static TypeDefinition? Resolve (this ExportedType type, LinkContext context)
		=> context.Resolve (type);

	public static TypeDefinition? TryResolve (this ExportedType type, LinkContext context)
		=> context.TryResolve (type);

	public static LinkerILProcessor GetLinkerILProcessor (this MethodBody body)
		=> new (body);
}
