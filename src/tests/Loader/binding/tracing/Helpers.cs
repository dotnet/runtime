// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Xunit;

namespace BinderTracingTests
{
    internal class Helpers
    {
        public static void ValidateBindOperation(BindOperation expected, BindOperation actual)
        {
            ValidateAssemblyName(expected.AssemblyName, actual.AssemblyName, nameof(BindOperation.AssemblyName));
            Assert.Equal(expected.AssemblyPath ?? string.Empty, actual.AssemblyPath);
            Assert.Equal(expected.AssemblyLoadContext, actual.AssemblyLoadContext);
            Assert.Equal(expected.RequestingAssemblyLoadContext ?? string.Empty, actual.RequestingAssemblyLoadContext);
            ValidateAssemblyName(expected.RequestingAssembly, actual.RequestingAssembly, nameof(BindOperation.RequestingAssembly));

            Assert.Equal(expected.Success, actual.Success);
            Assert.Equal(expected.ResultAssemblyPath ?? string.Empty, actual.ResultAssemblyPath);
            Assert.Equal(expected.Cached, actual.Cached);
            ValidateAssemblyName(expected.ResultAssemblyName, actual.ResultAssemblyName, nameof(BindOperation.ResultAssemblyName));

            ValidateResolutionAttempts(expected.ResolutionAttempts, actual.ResolutionAttempts);

            ValidateHandlerInvocations(expected.AssemblyLoadContextResolvingHandlers, actual.AssemblyLoadContextResolvingHandlers, "AssemblyLoadContextResolving");
            ValidateHandlerInvocations(expected.AppDomainAssemblyResolveHandlers, actual.AppDomainAssemblyResolveHandlers, "AppDomainAssemblyResolve");
            ValidateLoadFromHandlerInvocation(expected.AssemblyLoadFromHandler, actual.AssemblyLoadFromHandler);

            ValidateProbedPaths(expected.ProbedPaths, actual.ProbedPaths);

            ValidateNestedBinds(expected.NestedBinds, actual.NestedBinds);
        }

        public static string GetAssemblyInAppPath(string assemblyName)
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(appPath, $"{assemblyName}.dll");
        }

        public static string GetAssemblyInSubdirectoryPath(string assemblyName)
        {
            return Path.Combine(GetSubdirectoryPath(), $"{assemblyName}.dll");
        }

        public static string GetSubdirectoryPath()
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(appPath, "DependentAssemblies");
        }

        public static string GetProbingFilePath(ProbedPath.PathSource pathSource, string assemblyName, string culture, string baseAssemblyDirectory = null)
        {
            return GetProbingFilePath(pathSource, assemblyName, false, culture, baseAssemblyDirectory);
        }

        public static string GetProbingFilePath(ProbedPath.PathSource pathSource, string assemblyName, bool isExe)
        {
            return GetProbingFilePath(pathSource, assemblyName, isExe, null, null);
        }

        private static string GetProbingFilePath(ProbedPath.PathSource pathSource, string assemblyName, bool isExe, string culture, string baseAssemblyDirectory)
        {
            string baseDirectory = baseAssemblyDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(culture))
                baseDirectory = Path.Combine(baseDirectory, culture);

            string extension = isExe ? "exe" : "dll";
            switch (pathSource)
            {
                case ProbedPath.PathSource.AppPaths:
                case ProbedPath.PathSource.SatelliteSubdirectory:
                    return Path.Combine(baseDirectory, $"{assemblyName}.{extension}");
                default:
                    throw new ArgumentOutOfRangeException(nameof(pathSource));
            }
        }

        public static bool AssemblyNamesMatch(AssemblyName name1, AssemblyName name2)
        {
            if (name1 == null)
            {
                return name2 == null || name2.Name == "NULL";
            }
            else if (name2 == null)
            {
                return name1 == null || name1.Name == "NULL";
            }

            return name1.Name == name2.Name
                && ((name1.Version == null && name2.Version == null) || name1.Version == name2.Version)
                && ((string.IsNullOrEmpty(name1.CultureName) && string.IsNullOrEmpty(name2.CultureName)) || name1.CultureName == name2.CultureName);
        }

        private static void ValidateAssemblyName(AssemblyName expected, AssemblyName actual, string propertyName)
        {
            Assert.True(AssemblyNamesMatch(expected, actual), $"Unexpected value for {propertyName} on event - expected: {expected}, actual: {actual}");
        }

        private static void ValidateResolutionAttempts(List<ResolutionAttempt> expected, List<ResolutionAttempt> actual)
        {
            if (expected.Count > 0)
                Assert.Equal(expected.Count, actual.Count);

            for (var i = 0; i < expected.Count; i++)
            {
                var a = actual[i];
                var e = expected[i];

                string expectedActualMessage = $"{Environment.NewLine}Expected resolution attempt:{Environment.NewLine}{e.ToString()}{Environment.NewLine}Actual resolution attempt:{Environment.NewLine}{a.ToString()}";
                ValidateAssemblyName(e.AssemblyName, a.AssemblyName, nameof(ResolutionAttempt.AssemblyName));
                Assert.Equal(e.Stage, a.Stage);
                Assert.Equal(e.AssemblyLoadContext, a.AssemblyLoadContext);
                Assert.Equal(e.Result, a.Result);
                ValidateAssemblyName(e.ResultAssemblyName, a.ResultAssemblyName, nameof(ResolutionAttempt.ResultAssemblyName));
                Assert.Equal(e.ResultAssemblyPath ?? string.Empty, a.ResultAssemblyPath);
                Assert.Equal(e.ErrorMessage ?? string.Empty, a.ErrorMessage);
            }
        }

        private static void ValidateHandlerInvocations(List<HandlerInvocation> expected, List<HandlerInvocation> actual, string eventName)
        {
            Assert.Equal(expected.Count, actual.Count);

            foreach (var match in expected)
            {
                Predicate<HandlerInvocation> pred = h =>
                    AssemblyNamesMatch(h.AssemblyName, match.AssemblyName)
                        && h.HandlerName == match.HandlerName
                        && h.AssemblyLoadContext == match.AssemblyLoadContext
                        && AssemblyNamesMatch(h.ResultAssemblyName, match.ResultAssemblyName)
                        && (h.ResultAssemblyPath == match.ResultAssemblyPath || h.ResultAssemblyPath == "NULL" && match.ResultAssemblyPath == null);
                Assert.True(actual.Exists(pred), $"Handler invocation not found: {match.ToString()}");
            }
        }

        private static void ValidateLoadFromHandlerInvocation(LoadFromHandlerInvocation expected, LoadFromHandlerInvocation actual)
        {
            if (expected == null || actual == null)
            {
                Assert.Null(expected);
                Assert.Null(actual);
                return;
            }

            ValidateAssemblyName(expected.AssemblyName, actual.AssemblyName, nameof(LoadFromHandlerInvocation.AssemblyName));
            Assert.Equal(expected.IsTrackedLoad, actual.IsTrackedLoad);
            Assert.Equal(expected.RequestingAssemblyPath, actual.RequestingAssemblyPath);

            if (expected.ComputedRequestedAssemblyPath == null)
            {
                Assert.Equal("NULL", actual.ComputedRequestedAssemblyPath);
            }
            else
            {
                Assert.Equal(expected.ComputedRequestedAssemblyPath, actual.ComputedRequestedAssemblyPath);
            }
        }

        private static void ValidateProbedPaths(List<ProbedPath> expected, List<ProbedPath> actual)
        {
            foreach (var match in expected)
            {
                Predicate<ProbedPath> pred = p => p.FilePath == match.FilePath
                    && p.Source == match.Source
                    && p.Result == match.Result;
                Assert.True(actual.Exists(pred), $"Probed path not found: {match.ToString()}");
            }
        }

        private static bool BindOperationsMatch(BindOperation bind1, BindOperation bind2)
        {
            try
            {
                ValidateBindOperation(bind1, bind2);
            }
            catch (Xunit.Sdk.XunitException e)
            {
                return false;
            }

            return true;
        }

        private static void ValidateNestedBinds(List<BindOperation> expected, List<BindOperation> actual)
        {
            foreach (var match in expected)
            {
                Predicate<BindOperation> pred = b => BindOperationsMatch(match, b);
                Assert.True(actual.Exists(pred), $"Nested bind operation not found: {match.ToString()}");
            }
        }
    }
}
