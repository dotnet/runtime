// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	/// <summary>
	/// Use to compile an assembly after compiling the main test case executabe
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupCompileAfterAttribute : BaseMetadataAttribute
	{
		public SetupCompileAfterAttribute (string outputName, string[] sourceFiles, string[] references = null, string[] defines = null, object[] resources = null, string additionalArguments = null, string compilerToUse = null, bool addAsReference = true, bool removeFromLinkerInput = false)
		{
			if (sourceFiles == null)
				throw new ArgumentNullException (nameof (sourceFiles));

			if (string.IsNullOrEmpty (outputName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (outputName));

			if (resources != null) {
				foreach (var res in resources) {
					if (res is string)
						continue;
					if (res is string[] stringArray) {
						if (stringArray.Length != 2)
							throw new ArgumentException ("Entry in object[] cannot be a string[] unless it has exactly two elements, for the resource path and name", nameof (resources));
						continue;
					}
					throw new ArgumentException ("Each value in the object[] must be a string or a string[], either a resource path, or a path and name", nameof (resources));
				}
			}
		}
	}
}
