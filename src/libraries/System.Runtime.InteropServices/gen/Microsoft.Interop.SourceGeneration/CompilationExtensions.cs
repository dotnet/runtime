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
        public static StubEnvironment CreateStubEnvironment(this Compilation compilation)
        {
            TargetFramework targetFramework = DetermineTargetFramework(compilation, out Version targetFrameworkVersion);
            return new StubEnvironment(
                            compilation,
                            targetFramework,
                            targetFrameworkVersion,
                            compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute));

            static TargetFramework DetermineTargetFramework(Compilation compilation, out Version version)
            {
                IAssemblySymbol systemAssembly = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
                version = systemAssembly.Identity.Version;

                return systemAssembly.Identity.Name switch
                {
                    // .NET Framework
                    "mscorlib" => TargetFramework.Framework,
                    // .NET Standard
                    "netstandard" => TargetFramework.Standard,
                    // .NET Core (when version < 5.0) or .NET
                    "System.Runtime" or "System.Private.CoreLib" =>
                        (version.Major < 5) ? TargetFramework.Core : TargetFramework.Net,
                    _ => TargetFramework.Unknown,
                };
            }
        }
    }
}
