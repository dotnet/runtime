// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace BinderTracingTests
{
    internal class Helpers
    {
        public static void ValidateBindOperation(BindOperation expected, BindOperation actual)
        {
            ValidateAssemblyName(expected.AssemblyName, actual.AssemblyName, nameof(BindOperation.AssemblyName));
            Assert.AreEqual(expected.AssemblyPath ?? string.Empty, actual.AssemblyPath, $"Unexpected value for {nameof(BindOperation.AssemblyPath)} on event");
            Assert.AreEqual(expected.AssemblyLoadContext, actual.AssemblyLoadContext, $"Unexpected value for {nameof(BindOperation.AssemblyLoadContext)} on event");
            Assert.AreEqual(expected.RequestingAssemblyLoadContext ?? string.Empty, actual.RequestingAssemblyLoadContext, $"Unexpected value for {nameof(BindOperation.RequestingAssemblyLoadContext)} on event");
            ValidateAssemblyName(expected.RequestingAssembly, actual.RequestingAssembly, nameof(BindOperation.RequestingAssembly));

            Assert.AreEqual(expected.Success, actual.Success, $"Unexpected value for {nameof(BindOperation.Success)} on event");
            Assert.AreEqual(expected.ResultAssemblyPath ?? string.Empty, actual.ResultAssemblyPath, $"Unexpected value for {nameof(BindOperation.ResultAssemblyPath)} on event");
            Assert.AreEqual(expected.Cached, actual.Cached, $"Unexpected value for {nameof(BindOperation.Cached)} on event");
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
            Assert.IsTrue(AssemblyNamesMatch(expected, actual), $"Unexpected value for {propertyName} on event - expected: {expected}, actual: {actual}");
        }

        private static void ValidateResolutionAttempts(List<ResolutionAttempt> expected, List<ResolutionAttempt> actual)
        {
            if (expected.Count > 0)
                Assert.AreEqual(expected.Count, actual.Count,
                    $"Unexpected resolution attempt count. Actual events:{Environment.NewLine}{string.Join(Environment.NewLine, actual.Select(a => a.ToString()))}");

            for (var i = 0; i < expected.Count; i++)
            {
                var a = actual[i];
                var e = expected[i];

                string expectedActualMessage = $"{Environment.NewLine}Expected resolution attempt:{Environment.NewLine}{e.ToString()}{Environment.NewLine}Actual resolution attempt:{Environment.NewLine}{a.ToString()}";
                ValidateAssemblyName(e.AssemblyName, a.AssemblyName, nameof(ResolutionAttempt.AssemblyName));
                Assert.AreEqual(e.Stage, a.Stage, $"Unexpected value for {nameof(ResolutionAttempt.Stage)} {expectedActualMessage}");
                Assert.AreEqual(e.AssemblyLoadContext, a.AssemblyLoadContext, $"Unexpected value for {nameof(ResolutionAttempt.AssemblyLoadContext)} {expectedActualMessage}");
                Assert.AreEqual(e.Result, a.Result, $"Unexpected value for {nameof(ResolutionAttempt.Result)} {expectedActualMessage}");
                ValidateAssemblyName(e.ResultAssemblyName, a.ResultAssemblyName, nameof(ResolutionAttempt.ResultAssemblyName));
                Assert.AreEqual(e.ResultAssemblyPath ?? string.Empty, a.ResultAssemblyPath, $"Unexpected value for {nameof(ResolutionAttempt.ResultAssemblyPath)} {expectedActualMessage}");
                Assert.AreEqual(e.ErrorMessage ?? string.Empty, a.ErrorMessage, $"Unexpected value for {nameof(ResolutionAttempt.ErrorMessage)} {expectedActualMessage}");
            }
        }

        private static void ValidateHandlerInvocations(List<HandlerInvocation> expected, List<HandlerInvocation> actual, string eventName)
        {
            Assert.AreEqual(expected.Count, actual.Count, $"Unexpected handler invocation count for {eventName}");

            foreach (var match in expected)
            {
                Predicate<HandlerInvocation> pred = h =>
                    AssemblyNamesMatch(h.AssemblyName, match.AssemblyName)
                        && h.HandlerName == match.HandlerName
                        && h.AssemblyLoadContext == match.AssemblyLoadContext
                        && AssemblyNamesMatch(h.ResultAssemblyName, match.ResultAssemblyName)
                        && (h.ResultAssemblyPath == match.ResultAssemblyPath || h.ResultAssemblyPath == "NULL" && match.ResultAssemblyPath == null);
                Assert.IsTrue(actual.Exists(pred), $"Handler invocation not found: {match.ToString()}");
            }
        }

        private static void ValidateLoadFromHandlerInvocation(LoadFromHandlerInvocation expected, LoadFromHandlerInvocation actual)
        {
            if (expected == null || actual == null)
            {
                Assert.IsNull(expected);
                Assert.IsNull(actual);
                return;
            }

            ValidateAssemblyName(expected.AssemblyName, actual.AssemblyName, nameof(LoadFromHandlerInvocation.AssemblyName));
            Assert.AreEqual(expected.IsTrackedLoad, actual.IsTrackedLoad, $"Unexpected value for {nameof(LoadFromHandlerInvocation.IsTrackedLoad)} on event");
            Assert.AreEqual(expected.RequestingAssemblyPath, actual.RequestingAssemblyPath, $"Unexpected value for {nameof(LoadFromHandlerInvocation.RequestingAssemblyPath)} on event");

            if (expected.ComputedRequestedAssemblyPath == null)
            {
                Assert.AreEqual("NULL", actual.ComputedRequestedAssemblyPath, $"Unexpected value for {nameof(LoadFromHandlerInvocation.ComputedRequestedAssemblyPath)} on event");
            }
            else
            {
                Assert.AreEqual(expected.ComputedRequestedAssemblyPath, actual.ComputedRequestedAssemblyPath, $"Unexpected value for {nameof(LoadFromHandlerInvocation.ComputedRequestedAssemblyPath)} on event");
            }
        }

        private static void ValidateProbedPaths(List<ProbedPath> expected, List<ProbedPath> actual)
        {
            foreach (var match in expected)
            {
                Predicate<ProbedPath> pred = p => p.FilePath == match.FilePath
                    && p.Source == match.Source
                    && p.Result == match.Result;
                Assert.IsTrue(actual.Exists(pred), $"Probed path not found: {match.ToString()}");
            }
        }

        private static bool BindOperationsMatch(BindOperation bind1, BindOperation bind2)
        {
            try
            {
                ValidateBindOperation(bind1, bind2);
            }
            catch (AssertTestException e)
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
                Assert.IsTrue(actual.Exists(pred), $"Nested bind operation not found: {match.ToString()}");
            }
        }
    }
}
