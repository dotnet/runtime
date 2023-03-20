// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public static class DependencyResolutionCommandResultExtensions
    {
        // App asset resolution extensions
        public const string TRUSTED_PLATFORM_ASSEMBLIES = "TRUSTED_PLATFORM_ASSEMBLIES";
        public const string NATIVE_DLL_SEARCH_DIRECTORIES = "NATIVE_DLL_SEARCH_DIRECTORIES";

        public static AndConstraint<CommandResultAssertions> HaveRuntimePropertyContaining(this CommandResultAssertions assertion, string propertyName, params string[] values)
        {
            string propertyValue = GetAppMockPropertyValue(assertion, propertyName);

            foreach (string value in values)
            {
                Execute.Assertion.ForCondition(propertyValue != null && propertyValue.Contains(value))
                    .FailWith($"The property {propertyName} doesn't contain expected value: '{value}'{Environment.NewLine}" +
                        $"{propertyName}='{propertyValue}'" +
                        $"{assertion.GetDiagnosticsInfo()}");
            }

            return new AndConstraint<CommandResultAssertions>(assertion);
        }

        public static AndConstraint<CommandResultAssertions> NotHaveRuntimePropertyContaining(this CommandResultAssertions assertion, string propertyName, params string[] values)
        {
            string propertyValue = GetAppMockPropertyValue(assertion, propertyName);

            foreach (string value in values)
            {
                Execute.Assertion.ForCondition(propertyValue != null && !propertyValue.Contains(value))
                    .FailWith($"The property {propertyName} contains unexpected value: '{value}'{Environment.NewLine}" +
                        $"{propertyName}='{propertyValue}'" +
                        $"{assertion.GetDiagnosticsInfo()}");
            }

            return new AndConstraint<CommandResultAssertions>(assertion);
        }

        public static AndConstraint<CommandResultAssertions> HaveResolvedAssembly(this CommandResultAssertions assertion, string assemblyPath, TestApp app = null)
        {
            return assertion.HaveRuntimePropertyContaining(TRUSTED_PLATFORM_ASSEMBLIES, RelativePathsToAbsoluteAppPaths(assemblyPath, app));
        }

        public static AndConstraint<CommandResultAssertions> NotHaveResolvedAssembly(this CommandResultAssertions assertion, string assemblyPath, TestApp app = null)
        {
            return assertion.NotHaveRuntimePropertyContaining(TRUSTED_PLATFORM_ASSEMBLIES, RelativePathsToAbsoluteAppPaths(assemblyPath, app));
        }

        public static AndConstraint<CommandResultAssertions> HaveResolvedNativeLibraryPath(this CommandResultAssertions assertion, string path, TestApp app = null)
        {
            return assertion.HaveRuntimePropertyContaining(NATIVE_DLL_SEARCH_DIRECTORIES, RelativePathsToAbsoluteAppPaths(path, app));
        }

        public static AndConstraint<CommandResultAssertions> NotHaveResolvedNativeLibraryPath(this CommandResultAssertions assertion, string path, TestApp app = null)
        {
            return assertion.NotHaveRuntimePropertyContaining(NATIVE_DLL_SEARCH_DIRECTORIES, RelativePathsToAbsoluteAppPaths(path, app));
        }

        // Component asset resolution extensions
        private const string assemblies = "assemblies";
        private const string native_search_paths = "native_search_paths";

        public static AndConstraint<CommandResultAssertions> HaveSuccessfullyResolvedComponentDependencies(this CommandResultAssertions assertion)
        {
            return assertion.HaveStdOutContaining("corehost_resolve_component_dependencies:Success");
        }

        public static AndConstraint<CommandResultAssertions> HaveResolvedComponentDependencyContaining(
            this CommandResultAssertions assertion,
            string propertyName,
            params string[] values)
        {
            string propertyValue = GetComponentMockPropertyValue(assertion, propertyName);

            foreach (string value in values)
            {
                Execute.Assertion.ForCondition(propertyValue != null && propertyValue.Contains(value))
                    .FailWith($"The resolved {propertyName} doesn't contain expected value: '{value}'{Environment.NewLine}" +
                        $"{propertyName}='{propertyValue}'" +
                        $"{assertion.GetDiagnosticsInfo()}");
            }

            return new AndConstraint<CommandResultAssertions>(assertion);
        }

        public static AndConstraint<CommandResultAssertions> NotHaveResolvedComponentDependencyContaining(
            this CommandResultAssertions assertion,
            string propertyName,
            params string[] values)
        {
            string propertyValue = GetComponentMockPropertyValue(assertion, propertyName);

            foreach (string value in values)
            {
                Execute.Assertion.ForCondition(propertyValue != null && !propertyValue.Contains(value))
                    .FailWith($"The resolved {propertyName} contains unexpected value: '{value}'{Environment.NewLine}" +
                        $"{propertyName}='{propertyValue}'" +
                        $"{assertion.GetDiagnosticsInfo()}");
            }

            return new AndConstraint<CommandResultAssertions>(assertion);
        }

        public static AndConstraint<CommandResultAssertions> HaveResolvedComponentDependencyAssembly(
            this CommandResultAssertions assertion,
            string assemblyPath,
            TestApp app = null)
        {
            return assertion.HaveResolvedComponentDependencyContaining(assemblies, RelativePathsToAbsoluteAppPaths(assemblyPath, app));
        }

        public static AndConstraint<CommandResultAssertions> NotHaveResolvedComponentDependencyAssembly(
            this CommandResultAssertions assertion,
            string assemblyPath,
            TestApp app = null)
        {
            return assertion.NotHaveResolvedComponentDependencyContaining(assemblies, RelativePathsToAbsoluteAppPaths(assemblyPath, app));
        }

        public static AndConstraint<CommandResultAssertions> HaveResolvedComponentDependencyNativeLibraryPath(
            this CommandResultAssertions assertion,
            string path,
            TestApp app = null)
        {
            return assertion.HaveResolvedComponentDependencyContaining(native_search_paths, RelativePathsToAbsoluteAppPaths(path, app));
        }

        public static AndConstraint<CommandResultAssertions> NotHaveResolvedComponentDependencyNativeLibraryPath(
            this CommandResultAssertions assertion,
            string path,
            TestApp app = null)
        {
            return assertion.NotHaveResolvedComponentDependencyContaining(native_search_paths, RelativePathsToAbsoluteAppPaths(path, app));
        }

        public static AndConstraint<CommandResultAssertions> ErrorWithMissingAssembly(this CommandResultAssertions assertion, string depsFileName, string dependencyName, string dependencyVersion)
        {
            return assertion.HaveStdErrContaining(
                $"Error:{Environment.NewLine}" +
                $"  An assembly specified in the application dependencies manifest ({depsFileName}) was not found:" + Environment.NewLine +
                $"    package: \'{dependencyName}\', version: \'{dependencyVersion}\'" + Environment.NewLine +
                $"    path: \'{dependencyName}.dll\'");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedAdditionalDeps(this CommandResultAssertions assertion, string depsFilePath)
        {
            return assertion.HaveStdErrContaining($"Using specified additional deps.json: '{depsFilePath}'");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedAdditionalProbingPath(this CommandResultAssertions assertion, string path)
        {
            return assertion.HaveStdErrContaining($"Additional probe dir: {path}")
                .And.HaveStdErrContaining($"probe type=lookup dir=[{path}]");
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedFallbackRid(this CommandResultAssertions assertion, bool usedFallbackRid)
        {
            string msg = "Falling back to base HostRID";
            return usedFallbackRid ? assertion.HaveStdErrContaining(msg) : assertion.NotHaveStdErrContaining(msg);
        }

        public static AndConstraint<CommandResultAssertions> HaveUsedFrameworkProbe(this CommandResultAssertions assertion, string path, int level)
        {
            return assertion.HaveStdErrContaining($"probe type=framework dir=[{path}] fx_level={level}");
        }

        public static AndConstraint<CommandResultAssertions> NotHaveUsedFrameworkProbe(this CommandResultAssertions assertion, string path)
        {
            return assertion.NotHaveStdErrContaining($"probe type=framework dir=[{path}]");
        }

        private static string GetAppMockPropertyValue(CommandResultAssertions assertion, string propertyName) =>
            GetMockPropertyValue(assertion, $"mock property[{propertyName}] = ");

        private static string GetComponentMockPropertyValue(CommandResultAssertions assertion, string propertyName) =>
            GetMockPropertyValue(assertion, $"corehost_resolve_component_dependencies {propertyName}:");

        private static string GetMockPropertyValue(CommandResultAssertions assertion, string propertyHeader)
        {
            string stdout = assertion.Result.StdOut;
            int i = stdout.IndexOf(propertyHeader);
            if (i >= 0)
            {
                i += propertyHeader.Length;
                int end = assertion.Result.StdOut.IndexOf(Environment.NewLine, i);
                if (end >= i)
                {
                    return stdout.Substring(i, end - i);
                }
            }

            return null;
        }

        private static string[] RelativePathsToAbsoluteAppPaths(string relativePaths, TestApp app)
        {
            if (string.IsNullOrEmpty(relativePaths))
            {
                return Array.Empty<string>();
            }

            List<string> paths = new List<string>();
            foreach (string relativePath in relativePaths.Split(';'))
            {
                string path = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (app != null)
                {
                    path = Path.Combine(app.Location, path);
                }

                paths.Add(path);
            }

            return paths.ToArray();
        }
    }
}
