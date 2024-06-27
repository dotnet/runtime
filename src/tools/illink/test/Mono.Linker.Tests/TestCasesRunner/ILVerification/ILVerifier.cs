// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ILVerify;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestCasesRunner.ILVerification;

#nullable enable
public class ILVerifier : IResolver, IDisposable
{
	readonly Verifier _verifier;
	readonly Dictionary<string, PEReader> _assemblyCache = new();
	readonly NPath[] _searchDirectories;

	public ILVerifier (NPath[] searchDirectories, string systemModuleName)
	{
		_searchDirectories = searchDirectories;

		_verifier = new Verifier (
			this,
			new VerifierOptions {
				SanityChecks = true,
				IncludeMetadataTokensInErrorMessages = true
			});

		_verifier.SetSystemModuleName (new AssemblyNameInfo (systemModuleName));
	}

	public ILVerifierResult[] VerifyByName (string assemblyName)
	{
		var reader = Resolve (assemblyName);
		if (reader == null) {
			Assert.Fail ($"Failed to resolve : {assemblyName}");
		}

		return Verify (reader!);
	}

	public ILVerifierResult[] Verify (NPath assemblyPath) => Verify (LoadAssemblyFromPath (assemblyPath));

	public ILVerifierResult[] Verify (PEReader reader)
	{
		var results = _verifier.Verify (reader);

		var metadataReader = reader.GetMetadataReader ();

		return FilterResults (results)
			.Select (r => CreateResult (r, metadataReader))
			.ToArray ();
	}

	private static ILVerifierResult CreateResult (VerificationResult r, MetadataReader metadataReader) =>
		new(
			r,
			r.Type.IsNil
				? r.Method.GetMethodDeclaringTypeFullName (metadataReader)
				: r.Type.GetTypeFullName (metadataReader),
			r.Method.IsNil
				? string.Empty
				: r.Method.GetMethodSignature (metadataReader));

	protected virtual IEnumerable<VerificationResult> FilterResults (IEnumerable<VerificationResult> results)
	{
		return results.Where (r => r.Code switch {
			VerifierError.None
				// ex. localloc cannot be statically verified by ILVerify
				or VerifierError.Unverifiable
				// initlocals must be set for verifiable methods with one or more local variables - Lots of these in class libraries
				or VerifierError.InitLocals
				=> false,
			_ => true
		});
	}

	PEReader LoadAssemblyFromPath (NPath pathToAssembly)
		=> LoadAssemblyFromPath (pathToAssembly.FileNameWithoutExtension, pathToAssembly);

	PEReader LoadAssemblyFromPath (string assemblyName, NPath pathToAssembly)
	{
		if (_assemblyCache.TryGetValue (assemblyName, out PEReader? reader))
			return reader;
		reader = new PEReader (File.OpenRead (pathToAssembly));
		_assemblyCache.Add (assemblyName, reader);
		return reader;
	}

	bool TryLoadAssemblyFromFolder (string assemblyName, NPath folder, [NotNullWhen (true)] out PEReader? peReader)
	{
		NPath? assemblyPath = null;
		foreach (var extension in PossibleAssemblyExtensions) {
			var candidate = folder.Combine ($"{assemblyName}{extension}");
			if (candidate.FileExists ()) {
				assemblyPath = candidate;
				break;
			}
		}

		if (assemblyPath == null) {
			peReader = null;
			return false;
		}

		peReader = new PEReader (File.OpenRead (assemblyPath));
		_assemblyCache.Add (assemblyName, peReader);
		return true;
	}

	protected string[] PossibleAssemblyExtensions => new[] { ".dll", ".exe", ".winmd" };

	PEReader? Resolve (string assemblyName)
	{
		PEReader? reader;
		if (_assemblyCache.TryGetValue (assemblyName, out reader)) {
			return reader;
		}

		foreach (var searchDirectory in _searchDirectories) {
			if (TryLoadAssemblyFromFolder (assemblyName, searchDirectory, out reader))
				return reader;
		}

		return null;
	}

	PEReader? IResolver.ResolveAssembly (AssemblyNameInfo assemblyName)
		=> Resolve (assemblyName.Name ?? assemblyName.FullName);

	PEReader? IResolver.ResolveModule (AssemblyNameInfo referencingModule, string fileName)
		=> Resolve (Path.GetFileNameWithoutExtension (fileName));

	public void Dispose ()
	{
		foreach (var reader in _assemblyCache.Values)
			reader.Dispose ();
		_assemblyCache.Clear ();
	}
}
#nullable restore
