// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public static class CompilationExtensions
	{
		public static MetadataReference EmitToImageReference (
			this Compilation comp,
			EmitOptions? options = null,
			bool embedInteropTypes = false,
			ImmutableArray<string> aliases = default) => EmitToPortableExecutableReference (comp, options, embedInteropTypes, aliases);

		public static PortableExecutableReference EmitToPortableExecutableReference (
			this Compilation comp,
			EmitOptions? options = null,
			bool embedInteropTypes = false,
			ImmutableArray<string> aliases = default)
		{
			var image = comp.EmitToArray (options);
			if (comp.Options.OutputKind == OutputKind.NetModule) {
				return ModuleMetadata.CreateFromImage (image).GetReference (display: comp.MakeSourceModuleName ());
			} else {
				return AssemblyMetadata.CreateFromImage (image).GetReference (aliases: aliases, embedInteropTypes: embedInteropTypes, display: comp.MakeSourceAssemblySimpleName ());
			}
		}

		internal static ImmutableArray<byte> EmitToArray (
			this Compilation compilation,
			EmitOptions? options = null,
			Stream? pdbStream = null,
			IMethodSymbol? debugEntryPoint = null,
			Stream? sourceLinkStream = null,
			IEnumerable<EmbeddedText>? embeddedTexts = null,
			IEnumerable<ResourceDescription>? manifestResources = null,
			Stream? metadataPEStream = null)
		{
			var peStream = new MemoryStream ();

			var emitResult = compilation.Emit (
				peStream: peStream,
				metadataPEStream: metadataPEStream,
				pdbStream: pdbStream,
				xmlDocumentationStream: null,
				win32Resources: null,
				manifestResources: manifestResources,
				options: options,
				debugEntryPoint: debugEntryPoint,
				sourceLinkStream: sourceLinkStream,
				embeddedTexts: embeddedTexts,
				cancellationToken: default (CancellationToken));

			Assert.True (emitResult.Success, "Diagnostics:\r\n" + string.Join ("\r\n", emitResult.Diagnostics.Select (d => d.ToString ())));

			return peStream.ToImmutable ();
		}

		/// <summary>
		/// Reads bytes from specified <see cref="MemoryStream"/>.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>Read-only content of the stream.</returns>
		private static ImmutableArray<byte> ToImmutable (this MemoryStream stream)
		{
			return ImmutableArray.Create<byte> (stream.ToArray ());
		}

		internal static string MakeSourceModuleName (this Compilation compilation)
		{
			var UnspecifiedModuleAssemblyName = "?";
			return compilation.Options.ModuleName ??
				   (compilation.AssemblyName != null ? compilation.AssemblyName + compilation.Options.OutputKind.GetDefaultExtension () : UnspecifiedModuleAssemblyName);
		}

		internal static string MakeSourceAssemblySimpleName (this Compilation compilation)
		{
			var UnspecifiedModuleAssemblyName = "?";
			return compilation.AssemblyName ?? UnspecifiedModuleAssemblyName;
		}

		internal static string GetDefaultExtension (this OutputKind kind)
		{
			switch (kind) {
			case OutputKind.ConsoleApplication:
			case OutputKind.WindowsApplication:
			case OutputKind.WindowsRuntimeApplication:
				return ".exe";

			case OutputKind.DynamicallyLinkedLibrary:
				return ".dll";

			case OutputKind.NetModule:
				return ".netmodule";

			case OutputKind.WindowsRuntimeMetadata:
				return ".winmdobj";

			default:
				return ".dll";
			}
		}
	}
}
