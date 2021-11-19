// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal sealed class DependencyContextPaths
    {
        private const string DepsFilesProperty = "APP_CONTEXT_DEPS_FILES";
        private const string FxDepsFileProperty = "FX_DEPS_FILE";

        public static DependencyContextPaths Current { get; } = GetCurrent();

        public string? Application { get; }

        public string? SharedRuntime { get; }

        public IEnumerable<string> NonApplicationPaths { get; }

        public DependencyContextPaths(
            string? application,
            string? sharedRuntime,
            IEnumerable<string>? nonApplicationPaths)
        {
            Application = application;
            SharedRuntime = sharedRuntime;
            NonApplicationPaths = nonApplicationPaths ?? Enumerable.Empty<string>();
        }

        private static DependencyContextPaths GetCurrent()
        {
            object? deps = AppDomain.CurrentDomain.GetData(DepsFilesProperty);
            object? fxDeps = AppDomain.CurrentDomain.GetData(FxDepsFileProperty);

            return Create(deps as string, fxDeps as string);
        }

        internal static DependencyContextPaths Create(string? depsFiles, string? sharedRuntime)
        {
            string[]? files = depsFiles?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string? application = files != null && files.Length > 0 ? files[0] : null;

            string[]? nonApplicationPaths = files?
                .Skip(1) // the application path
                .ToArray();

            return new DependencyContextPaths(
                application,
                sharedRuntime,
                nonApplicationPaths);
        }
    }
}
