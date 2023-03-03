// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Mono.Linker
{
	[DataContract]
	public class PInvokeInfo : IComparable<PInvokeInfo>
	{
		[DataMember (Name = "assembly")]
		internal string AssemblyName { get; set; }

		[DataMember (Name = "entryPoint")]
		internal string EntryPoint { get; set; }

		[DataMember (Name = "fullName")]
		internal string FullName { get; set; }

		[DataMember (Name = "moduleName")]
		internal string ModuleName { get; set; }

		public PInvokeInfo (string assemblyName, string entryPoint, string fullName, string moduleName)
		{
			AssemblyName = assemblyName;
			EntryPoint = entryPoint;
			FullName = fullName;
			ModuleName = moduleName;
		}

		public int CompareTo (PInvokeInfo? other)
		{
			if (other == null) return 1;

			int compareField = string.Compare (this.AssemblyName, other.AssemblyName, StringComparison.Ordinal);
			if (compareField != 0) return compareField;

			compareField = string.Compare (this.ModuleName, other.ModuleName, StringComparison.Ordinal);
			if (compareField != 0) return compareField;

			compareField = string.Compare (this.FullName, other.FullName, StringComparison.Ordinal);
			if (compareField != 0) return compareField;

			return string.Compare (this.EntryPoint, other.EntryPoint, StringComparison.Ordinal);
		}
	}
}