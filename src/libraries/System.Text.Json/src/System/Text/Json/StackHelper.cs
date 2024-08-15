// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    /// <summary>Provides tools for avoiding stack overflows.</summary>
    internal static class StackHelper
    {
        /// <summary>Tries to ensure there is sufficient stack to execute the average .NET function.</summary>
        public static bool TryEnsureSufficientExecutionStack()
        {
#if NET
            return RuntimeHelpers.TryEnsureSufficientExecutionStack();
#else
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                return true;
            }
            catch
            {
                return false;
            }
#endif
        }
    }
}
