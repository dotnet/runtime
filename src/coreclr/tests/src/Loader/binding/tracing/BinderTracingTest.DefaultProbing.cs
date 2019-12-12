// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace BinderTracingTests
{
    partial class BinderTracingTest
    {
        private static CultureInfo SatelliteCulture = CultureInfo.CreateSpecificCulture("fr-FR");

        [BinderTest]
        public static BindOperation AssemblyInAppPath()
        {
            AssemblyName assemblyName = new AssemblyName(DependentAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyInAppPath));
            Assembly asm = alc.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppNativeImagePaths, assemblyName.Name, isExe: false),
                        Source = ProbedPath.PathSource.AppNativeImagePaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppNativeImagePaths, assemblyName.Name, isExe: true),
                        Source = ProbedPath.PathSource.AppNativeImagePaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppPaths, assemblyName.Name, isExe: false),
                        Source = ProbedPath.PathSource.AppPaths,
                        Result = S_OK
                    }
                }
            };
        }

        [BinderTest]
        public static BindOperation NonExistentAssembly()
        {
            string assemblyName = "DoesNotExist";
            try
            {
                Assembly.Load(assemblyName);
            }
            catch { }

            return new BindOperation()
            {
                AssemblyName = new AssemblyName(assemblyName),
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = false,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppNativeImagePaths, assemblyName, isExe: false),
                        Source = ProbedPath.PathSource.AppNativeImagePaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppNativeImagePaths, assemblyName, isExe: true),
                        Source = ProbedPath.PathSource.AppNativeImagePaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppPaths, assemblyName, isExe: false),
                        Source = ProbedPath.PathSource.AppPaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppPaths, assemblyName, isExe: true),
                        Source = ProbedPath.PathSource.AppPaths,
                        Result = COR_E_FILENOTFOUND
                    }
                }
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation SatelliteAssembly_AppPath()
        {
            AssemblyName assemblyName = new AssemblyName($"{DependentAssemblyName}.resources");
            assemblyName.CultureInfo = SatelliteCulture;
            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppPaths, assemblyName.Name, SatelliteCulture.Name),
                        Source = ProbedPath.PathSource.AppPaths,
                        Result = S_OK
                    }
                }
            };
        }

        [BinderTest]
        public static BindOperation SatelliteAssembly_CultureSubdirectory()
        {
            AssemblyName assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}.resources");
            assemblyName.CultureInfo = SatelliteCulture;
            CustomALC alc = new CustomALC(nameof(SatelliteAssembly_CultureSubdirectory));
            alc.LoadFromAssemblyPath(Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName));
            Assembly asm = alc.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, SatelliteCulture.Name, Helpers.GetSubdirectoryPath()),
                        Source = ProbedPath.PathSource.SatelliteSubdirectory,
                        Result = S_OK
                    }
                }
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation SatelliteAssembly_CultureSubdirectory_DefaultALC()
        {
            AssemblyName assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}.resources");
            assemblyName.CultureInfo = SatelliteCulture;

            // https://github.com/dotnet/corefx/issues/42477
            _ = AssemblyLoadContext.Default;

            Assembly OnAppDomainAssemblyResolve(object sender, ResolveEventArgs args)
            {
                AssemblyName requested = new AssemblyName(args.Name);
                return requested.Name == SubdirectoryAssemblyName
                    ? Assembly.LoadFile(Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName))
                    : null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += OnAppDomainAssemblyResolve;
            Assembly asm = Assembly.Load(assemblyName);
            AppDomain.CurrentDomain.AssemblyResolve -= OnAppDomainAssemblyResolve;

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.AppPaths, assemblyName.Name, SatelliteCulture.Name),
                        Source = ProbedPath.PathSource.AppPaths,
                        Result = COR_E_FILENOTFOUND
                    },
                    new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, SatelliteCulture.Name, Helpers.GetSubdirectoryPath()),
                        Source = ProbedPath.PathSource.SatelliteSubdirectory,
                        Result = S_OK
                    }
                }
            };
        }
    }
}
