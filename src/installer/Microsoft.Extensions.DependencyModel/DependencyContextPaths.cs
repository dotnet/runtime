using System;

#if !NETSTANDARD1_3

namespace Microsoft.Extensions.DependencyModel
{
    internal class DependencyContextPaths
    {
        private static readonly string DepsFilesProperty = "APP_CONTEXT_DEPS_FILES";

        public static DependencyContextPaths Current { get; } = GetCurrent();

        public string Application { get; }

        public string SharedRuntime { get; }

        public DependencyContextPaths(string application, string sharedRuntime)
        {
            Application = application;
            SharedRuntime = sharedRuntime;
        }

        private static DependencyContextPaths GetCurrent()
        {
#if NETSTANDARD1_6
            var deps = AppContext.GetData(DepsFilesProperty);
#else
            var deps = AppDomain.CurrentDomain.GetData(DepsFilesProperty);
#endif
            var files = (deps as string)?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return new DependencyContextPaths(
                files != null && files.Length > 0 ? files[0] : null,
                files != null && files.Length > 1 ? files[1] : null
                );
        }
    }
}
#endif
