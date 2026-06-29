// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace FullPdbTestAssembly
{
    public static class FullPdbThrower
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetExceptionString()
        {
            try
            {
                Throw();
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void Throw()
        {
            throw new InvalidOperationException("Exception from full PDB test assembly.");
        }
    }
}
