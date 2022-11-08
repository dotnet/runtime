// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ComputeManagedAssemblies : Task
	{
		/// <summary>
		///   Paths to assemblies.
		/// </summary>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		///   This will contain the output list of managed
		///   assemblies. Metadata from the input parameter
		///   Assemblies is preserved.
		/// </summary>
		[Output]
		public ITaskItem[] ManagedAssemblies { get; set; }

		public override bool Execute ()
		{
			ManagedAssemblies = Assemblies
				.Where (f => Utils.IsManagedAssembly (f.ItemSpec))
				.ToArray ();
			return true;
		}
	}
}
