// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class CompilationExtensions
    {
        public static EnvironmentFlags GetEnvironmentFlags(this Compilation compilation)
        {
            EnvironmentFlags flags = EnvironmentFlags.None;
            if (compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute))
            {
                flags |= EnvironmentFlags.SkipLocalsInit;
            }
            if (compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute))
            {
                flags |= EnvironmentFlags.DisableRuntimeMarshalling;
            }
            return flags;
        }
    }
}
