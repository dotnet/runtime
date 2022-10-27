// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public override bool Execute()
        {
            ManagedAssemblies = Assemblies
                .Where(f => Utils.IsManagedAssembly(f.ItemSpec))
                .ToArray();
            return true;
        }
    }
}
