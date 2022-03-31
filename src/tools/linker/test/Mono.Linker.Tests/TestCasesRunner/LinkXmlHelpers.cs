// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Mono.Cecil;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class LinkXmlHelpers
	{
		public static void WriteXmlFileToPreserveEntryPoint (NPath targetProgram, NPath xmlFile)
		{
			using (var assembly = AssemblyDefinition.ReadAssembly (targetProgram.ToString ())) {
				var method = assembly.EntryPoint;

				var sb = new StringBuilder ();
				sb.AppendLine ("<linker>");

				sb.AppendLine (" <assembly fullname=\"" + assembly.FullName + "\">");

				if (method != null) {
					sb.AppendLine ("  <type fullname=\"" + method.DeclaringType.FullName + "\">");
					sb.AppendLine ("   <method name=\"" + method.Name + "\"/>");
					sb.AppendLine ("  </type>");
				}

				sb.AppendLine (" </assembly>");

				sb.AppendLine ("</linker>");
				xmlFile.WriteAllText (sb.ToString ());
			}
		}
	}
}