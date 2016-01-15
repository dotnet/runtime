// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Reflection
{
    /// <summary>
    /// Extension methods offering source-code compatibility with certain instance methods of <see cref="System.Reflection.Assembly"/> on other platforms.
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Gets the public types defined in this assembly that are visible outside the assembly.
        /// </summary>
        /// <param name="assembly">Assembly on which to perform lookup</param>
        /// <returns>An array that represents the types defined in this assembly that are visible outside the assembly.</returns>
        public static Type[] GetExportedTypes(this Assembly assembly)
        {
            Requires.NotNull(assembly, "assembly");
            return assembly.ExportedTypes.ToArray();
        }

        /// <summary>
        /// Gets all the modules that are part of this assembly.
        /// </summary>
        /// <param name="assembly">Assembly on which to perform lookup</param>
        /// <returns>An array of modules.</returns>
        public static Module[] GetModules(this Assembly assembly)
        {
            Requires.NotNull(assembly, "assembly");
            return assembly.Modules.ToArray();
        }

        /// <summary>
        /// Gets the types defined in this assembly.
        /// </summary>
        /// <param name="assembly">Assembly on which to perform lookup</param>
        /// <returns>An array that contains all the types that are defined in this assembly.</returns>
        public static Type[] GetTypes(this Assembly assembly)
        {
            Requires.NotNull(assembly, "assembly");
            return assembly.DefinedTypes.Select(t => t.AsType()).ToArray();
        }
    }
}
