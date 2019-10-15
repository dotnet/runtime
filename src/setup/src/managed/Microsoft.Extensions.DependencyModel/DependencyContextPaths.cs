// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

#if !NETSTANDARD1_3

namespace Microsoft.Extensions.DependencyModel
{
    internal class DependencyContextPaths
    {
        private static readonly string DepsFilesProperty = "APP_CONTEXT_DEPS_FILES";
        private static readonly string FxDepsFileProperty = "FX_DEPS_FILE";

        public static DependencyContextPaths Current { get; } = GetCurrent();

        public string Application { get; }

        public string SharedRuntime { get; }

        public IEnumerable<string> NonApplicationPaths { get; }

        public DependencyContextPaths(
            string application,
            string sharedRuntime,
            IEnumerable<string> nonApplicationPaths)
        {
            Application = application;
            SharedRuntime = sharedRuntime;
            NonApplicationPaths = nonApplicationPaths ?? Enumerable.Empty<string>();
        }

        private static DependencyContextPaths GetCurrent()
        {
#if NETSTANDARD1_6
            var deps = AppContext.GetData(DepsFilesProperty);
            var fxDeps = AppContext.GetData(FxDepsFileProperty);
#else
            var deps = AppDomain.CurrentDomain.GetData(DepsFilesProperty);
            var fxDeps = AppDomain.CurrentDomain.GetData(FxDepsFileProperty);
#endif
            return Create(deps as string, fxDeps as string);
        }

        internal static DependencyContextPaths Create(string depsFiles, string sharedRuntime)
        {
            var files = depsFiles?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var application = files != null && files.Length > 0 ? files[0] : null;

            var nonApplicationPaths = files?
                .Skip(1) // the application path
                .ToArray();

            return new DependencyContextPaths(
                application,
                sharedRuntime,
                nonApplicationPaths);
        }
    }
}
#endif
