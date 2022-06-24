// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Class that provides cached information about method body importation errors that occurred in previous phases.
    /// </summary>
    public class MethodImportationErrorProvider
    {
        /// <summary>
        /// Returns the error discovered while processing the method body for the given method in the past.
        /// </summary>
        /// <returns>Relevant <see cref="TypeSystemException"/> or null if no error was seen.</returns>
        public virtual TypeSystemException GetCompilationError(MethodDesc method) => null;
    }
}
