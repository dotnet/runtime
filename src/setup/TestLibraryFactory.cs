﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    static class TestLibraryFactory
    {
        public static readonly string DefaultType = "package";
        public static readonly string DefaultPackageName = "My.Package";
        public static readonly string DefaultVersion = "1.2.3.7";
        public static readonly Dependency[] DefaultDependencies = { };
        public static readonly bool DefaultServiceable = true;

        public static readonly string DefaultAssembly = "My.Package.dll";
        public static readonly string SecondAssembly = "My.PackageEx.dll";
        public static readonly string DefaultAssemblyPath = Path.Combine("ref", DefaultAssembly);
        public static readonly string SecondAssemblyPath = Path.Combine("ref", SecondAssembly);
        public static readonly string[] EmptyAssemblies = { };
        public static readonly string[] DefaultAssemblies = { DefaultAssemblyPath };
        public static readonly string[] TwoAssemblies = { DefaultAssemblyPath, SecondAssemblyPath };

        public static readonly string DefaultHashValue = "HASHVALUE";
        public static readonly string DefaultHashAlgoritm = "ALG";
        public static readonly string DefaultHash = DefaultHashAlgoritm + "-" + DefaultHashValue;

        public static readonly string ProjectType = "project";
        public static readonly string ReferenceAssemblyType = "referenceassembly";
        public static readonly string PackageType = "package";

        public static CompilationLibrary Create(
            string libraryType = null,
            string packageName = null,
            string version = null,
            string hash = null,
            string[] assemblies = null,
            Dependency[] dependencies = null,
            bool? serviceable = null)
        {
            return new CompilationLibrary(
                libraryType ?? DefaultType,
                packageName ?? DefaultPackageName,
                version ?? DefaultVersion,
                hash ?? DefaultHash,
                assemblies ?? DefaultAssemblies,
                dependencies ?? DefaultDependencies,
                serviceable ?? DefaultServiceable
                );
        }
    }

}
