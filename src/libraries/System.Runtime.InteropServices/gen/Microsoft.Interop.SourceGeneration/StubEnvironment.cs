// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed record StubEnvironment(
        Compilation Compilation,
        TargetFramework TargetFramework,
        Version TargetFrameworkVersion,
        bool ModuleSkipLocalsInit)
    {
        /// <summary>
        /// Override for determining if two StubEnvironment instances are
        /// equal. This intentionally excludes the Compilation instance
        /// since that represents the actual compilation and not just the settings.
        /// </summary>
        /// <param name="env1">The first StubEnvironment</param>
        /// <param name="env2">The second StubEnvironment</param>
        /// <returns>True if the settings are equal, otherwise false.</returns>
        public static bool AreCompilationSettingsEqual(StubEnvironment env1, StubEnvironment env2)
        {
            return env1.TargetFramework == env2.TargetFramework
                && env1.TargetFrameworkVersion == env2.TargetFrameworkVersion
                && env1.ModuleSkipLocalsInit == env2.ModuleSkipLocalsInit;
        }
    }
}
