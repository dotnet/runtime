// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;

using TestLibrary;

namespace BinderTracingTests
{
    partial class BinderTracingTest
    {
        // Assembly found in app path:
        //   KnownPathProbed : AppNativeImagePaths  (DLL)   [COR_E_FILENOTFOUND]
        //   KnownPathProbed : AppNativeImagePaths  (EXE)   [COR_E_FILENOTFOUND]
        //   KnownPathProbed : AppPaths             (DLL)   [S_OK]
        // Note: corerun always sets APP_PATH and APP_NI_PATH. In regular use cases,
        // the customer would have to explicitly configure the app to set those.
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

        // Assembly not found:
        //   KnownPathProbed : AppNativeImagePaths  (DLL)   [COR_E_FILENOTFOUND]
        //   KnownPathProbed : AppNativeImagePaths  (EXE)   [COR_E_FILENOTFOUND]
        //   KnownPathProbed : AppPaths             (DLL)   [COR_E_FILENOTFOUND]
        //   KnownPathProbed : AppPaths             (EXE)   [COR_E_FILENOTFOUND]
        // Note: corerun always sets APP_PATH and APP_NI_PATH. In regular use cases,
        // the customer would have to explicitly configure the app to set those.
        [BinderTest(additionalLoadsToTrack: new string[] { "DoesNotExist" })]
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

        // Satellite assembly found in app path:
        //   KnownPathProbed : AppPaths             (DLL)   [S_OK]
        // Note: corerun always sets APP_PATH and APP_NI_PATH. In regular use cases,
        // the customer would have to explicitly configure the app to set those.
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

        // Satellite assembly found in culture subdirectory (custom ALC):
        //   KnownPathProbed : SatelliteSubdirectory    [S_OK]
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

        // Satellite assembly found in culture subdirectory (default ALC):
        //   KnownPathProbed : AppPaths                 [COR_E_FILENOTFOUND]
        //   KnownPathProbed : SatelliteSubdirectory    [S_OK]
        // Note: corerun always sets APP_PATH and APP_NI_PATH. In regular use cases,
        // the customer would have to explicitly configure the app to set those.
        [BinderTest(isolate: true)]
        public static BindOperation SatelliteAssembly_CultureSubdirectory_DefaultALC()
        {
            AssemblyName assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}.resources");
            assemblyName.CultureInfo = SatelliteCulture;

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

        // Satellite assembly found in lower-case culture subdirectory
        // On non-Linux:
        //   KnownPathProbed : SatelliteSubdirectory    (exact case)    [S_OK]
        // On Linux (case-sensitive):
        //   KnownPathProbed : SatelliteSubdirectory    (exact case)    [COR_E_FILENOTFOUND]
        //   KnownPathProbed : SatelliteSubdirectory    (lower-case)    [S_OK]
        [BinderTest]
        public static BindOperation SatelliteAssembly_CultureSubdirectory_Lowercase()
        {
            AssemblyName assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}.resources");
            assemblyName.CultureInfo = SatelliteCulture;
            CustomALC alc = new CustomALC(nameof(SatelliteAssembly_CultureSubdirectory));
            alc.LoadFromAssemblyPath(Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName));

            Assembly asm;
            string subdirectoryPath = Helpers.GetSubdirectoryPath();
            string cultureSubdirectory = Path.Combine(subdirectoryPath, SatelliteCulture.Name);
            string cultureSubdirectoryLower = Path.Combine(subdirectoryPath, SatelliteCulture.Name.ToLowerInvariant());
            try
            {
                Directory.Move(cultureSubdirectory, cultureSubdirectoryLower);
                asm = alc.LoadFromAssemblyName(assemblyName);
            }
            finally
            {
                Directory.Move(cultureSubdirectoryLower, cultureSubdirectory);
            }

            var probedPaths = new List<ProbedPath>()
            {
                new ProbedPath()
                {
                    FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, SatelliteCulture.Name, subdirectoryPath),
                    Source = ProbedPath.PathSource.SatelliteSubdirectory,
                    Result = OperatingSystem.IsLinux() ? COR_E_FILENOTFOUND : S_OK
                }
            };

            // On Linux, the path with a lower-case culture name should also be probed
            if (OperatingSystem.IsLinux())
            {
                probedPaths.Add(new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, SatelliteCulture.Name.ToLowerInvariant(), subdirectoryPath),
                        Source = ProbedPath.PathSource.SatelliteSubdirectory,
                        Result = S_OK
                    });
            }

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = probedPaths
            };
        }

        // Satellite assembly not found
        // On non-Linux:
        //   KnownPathProbed : SatelliteSubdirectory    (exact case)    [COR_E_FILENOTFOUND]
        // On Linux (case-sensitive):
        //   KnownPathProbed : SatelliteSubdirectory    (exact case)    [COR_E_FILENOTFOUND]
        //   KnownPathProbed : SatelliteSubdirectory    (lower-case)    [COR_E_FILENOTFOUND]
        [BinderTest]
        public static BindOperation SatelliteAssembly_NotFound()
        {
            string cultureName = "en-GB";
            AssemblyName assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}.resources");
            assemblyName.CultureInfo = new CultureInfo(cultureName);
            CustomALC alc = new CustomALC(nameof(SatelliteAssembly_CultureSubdirectory));
            alc.LoadFromAssemblyPath(Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName));
            Assert.Throws<FileNotFoundException>(() => alc.LoadFromAssemblyName(assemblyName));

            var probedPaths = new List<ProbedPath>()
            {
                new ProbedPath()
                {
                    FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, cultureName, Helpers.GetSubdirectoryPath()),
                    Source = ProbedPath.PathSource.SatelliteSubdirectory,
                    Result = COR_E_FILENOTFOUND
                }
            };

            // On Linux (case-sensitive), the path with a lower-case culture name should also be probed
            if (OperatingSystem.IsLinux())
            {
                probedPaths.Add(new ProbedPath()
                    {
                        FilePath = Helpers.GetProbingFilePath(ProbedPath.PathSource.SatelliteSubdirectory, assemblyName.Name, cultureName.ToLowerInvariant(), Helpers.GetSubdirectoryPath()),
                        Source = ProbedPath.PathSource.SatelliteSubdirectory,
                        Result = COR_E_FILENOTFOUND
                    });
            }

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = false,
                Cached = false,
                ProbedPaths = probedPaths
            };
        }
    }
}
