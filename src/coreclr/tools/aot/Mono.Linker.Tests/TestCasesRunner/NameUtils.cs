// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using ILCompiler;
using Internal.TypeSystem;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	internal static class NameUtils
	{
		internal static string? GetActualOriginDisplayName (TypeSystemEntity? entity) => entity switch {
			DefType defType => TrimAssemblyNamePrefix (defType.GetDisplayName ()),
			MethodDesc method => TrimAssemblyNamePrefix (method.GetDisplayName ()),
			FieldDesc field => TrimAssemblyNamePrefix (field.ToString ()),
			ModuleDesc module => module.Assembly.GetName ().Name,
			_ => null
		};

		private static string? TrimAssemblyNamePrefix (string? name)
		{
			if (name == null)
				return null;

			if (name.StartsWith ('[')) {
				int i = name.IndexOf (']');
				if (i > 0) {
					return name.Substring (i + 1);
				}
			}

			return name;
		}

		internal static string GetExpectedOriginDisplayName (ICustomAttributeProvider provider) =>
			ConvertSignatureToIlcFormat (provider switch {
				MethodDefinition method => method.GetDisplayName (),
				FieldDefinition field => field.GetDisplayName (),
				TypeDefinition type => type.GetDisplayName (),
				IMemberDefinition member => member.FullName,
				AssemblyDefinition asm => asm.Name.Name,
				_ => throw new NotImplementedException ()
			});

		internal static string ConvertSignatureToIlcFormat (string value)
		{
			if (value.Contains ('(') || value.Contains ('<')) {
				value = value.Replace (", ", ",");
			}

			if (value.Contains ('/')) {
				value = value.Replace ('/', '+');
			}

			// Split it into . separated parts and if one is ending with > rewrite it to `1 format
			// ILC folows the reflection format which doesn't actually use generic instantiations on anything but the last type
			// in nested hierarchy - it's difficult to replicate this with Cecil as it has different representation so just strip that info
			var parts = value.Split ('.');
			StringBuilder sb = new StringBuilder ();
			foreach (var part in parts) {
				if (sb.Length > 0)
					sb.Append ('.');

				if (part.EndsWith ('>')) {
					int i = part.LastIndexOf ('<');
					if (i >= 0) {
						sb.Append (part.AsSpan (0, i));
						sb.Append ('`');
						sb.Append (part.Substring (i + 1).Where (c => c == ',').Count () + 1);
						continue;
					}
				}

				sb.Append (part);
			}

			return sb.ToString ();
		}
	}
}
