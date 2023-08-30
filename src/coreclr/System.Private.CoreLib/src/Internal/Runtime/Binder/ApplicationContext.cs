// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Internal.Runtime.Binder
{
    internal struct TPAEntry
    {
        public string? ILFileName;
        public string? NIFileName;
    }

    internal record struct FailureCacheKey(string SimpleName, AssemblyVersion Version)
    {
        public FailureCacheKey(AssemblyName assemblyName) : this(assemblyName.SimpleName, assemblyName.Version) { }
    }

    internal class ApplicationContext
    {
        private volatile int _version;

        public int Version => _version;

        private readonly Dictionary<AssemblyName, Assembly> _executionContext = new Dictionary<AssemblyName, Assembly>();

        public Dictionary<FailureCacheKey, Exception?> FailureCache { get; } = new Dictionary<FailureCacheKey, Exception?>();

        public object ContextCriticalSection { get; } = new object();

        private readonly List<string> _platformResourceRoots = new List<string>();
        private readonly List<string> _appPaths = new List<string>();

        public Dictionary<string, TPAEntry>? TrustedPlatformAssemblyMap { get; private set; }

        private void IncrementVersion() => Interlocked.Increment(ref _version);

        private const char PATH_SEPARATOR_CHAR = ';';

        private static bool GetNextPath(string paths, ref int startPos, out string outPath)
        {
            bool wrappedWithQuotes = false;

            // Skip any leading spaces or path separators
            while (startPos < paths.Length && paths[startPos] is ' ' or PATH_SEPARATOR_CHAR)
                startPos++;

            if (startPos == paths.Length)
            {
                // No more paths in the string and we just skipped over some white space
                outPath = string.Empty;
                return false;
            }

            // Support paths being wrapped with quotations
            while (startPos < paths.Length && paths[startPos] == '\"')
            {
                startPos++;
                wrappedWithQuotes = true;
            }

            int iEnd = startPos; // Where current path ends
            int iNext;           // Where next path starts

            if (wrappedWithQuotes)
            {
                iEnd = paths.AsSpan(iEnd).IndexOf('\"');
                if (iEnd != -1)
                {
                    // Find where the next path starts - there should be a path separator right after the closing quotation mark
                    iNext = paths.AsSpan(iEnd).IndexOf(PATH_SEPARATOR_CHAR);
                    if (iNext != -1)
                    {
                        iNext++;
                    }
                    else
                    {
                        iNext = paths.Length;
                    }
                }
                else
                {
                    // There was no terminating quotation mark - that's bad
                    throw new ArgumentException(nameof(paths));
                }
            }
            else if ((iEnd = paths.AsSpan(iEnd).IndexOf(PATH_SEPARATOR_CHAR)) != -1)
            {
                iNext = iEnd + 1;
            }
            else
            {
                iNext = iEnd = paths.Length;
            }

            // Skip any trailing spaces
            while (paths[iEnd - 1] == ' ')
            {
                iEnd--;
            }

            Debug.Assert(startPos < iEnd);

            outPath = paths[startPos..iEnd];
            startPos = iNext;
            return true;
        }

        private static bool IsRelativePath(ReadOnlySpan<char> path)
        {
            // ported from coreclr/inc/clr/fs/path.h

#if TARGET_WINDOWS
            // Check for a paths like "C:\..." or "\\...". Additional notes:
            // - "\\?\..." - long format paths are considered as absolute paths due to the "\\" prefix
            // - "\..." - these paths are relative, as they depend on the current drive
            // - "C:..." and not "C:\..." - these paths are relative, as they depend on the current directory for drive C
            if (path is [(>= 'A' and <= 'Z') or (>= 'a' and <= 'z'), PathInternal.VolumeSeparatorChar, PathInternal.DirectorySeparatorChar, ..])
            {
                return false;
            }
            if (path is [PathInternal.DirectorySeparatorChar, PathInternal.DirectorySeparatorChar, ..])
            {
                return false;
            }
#else
            if (path is [PathInternal.DirectorySeparatorChar, ..])
            {
                return false;
            }
#endif
            return true;
        }

        private static bool GetNextTPAPath(string paths, ref int startPos, bool dllOnly, out string outPath, out string simpleName, out bool isNativeImage)
        {
            isNativeImage = false;

            while (true)
            {
                if (!GetNextPath(paths, ref startPos, out outPath))
                {
                    simpleName = string.Empty;
                    return false;
                }

                if (IsRelativePath(outPath))
                {
                    throw new ArgumentException(nameof(paths));
                }

                // Find the beginning of the simple name
                int iSimpleNameStart = outPath.LastIndexOf(PathInternal.DirectorySeparatorChar);
                if (iSimpleNameStart == -1)
                {
                    iSimpleNameStart = 0;
                }
                else
                {
                    // Advance past the directory separator to the first character of the file name
                    iSimpleNameStart++;
                }

                if (iSimpleNameStart == outPath.Length)
                {
                    throw new ArgumentException(nameof(paths));
                }

                if (dllOnly && (outPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    || outPath.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase)))
                {
                    // Skip exe files when the caller requested only dlls
                    continue;
                }

                if (outPath.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase)
                    || outPath.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase))
                {
                    simpleName = outPath[iSimpleNameStart..^7];
                    isNativeImage = true;
                }
                else if (outPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || outPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    simpleName = outPath[iSimpleNameStart..^4];
                }
                else
                {
                    // Invalid filename
                    throw new ArgumentException(nameof(paths));
                }

                return true; ;
            }
        }

        public void SetupBindingPaths(string trustedPlatformAssemblies, string platformResourceRoots, string appPaths, bool acquireLock)
        {
            if (acquireLock)
            {
                lock (ContextCriticalSection)
                {
                    Core(trustedPlatformAssemblies, platformResourceRoots, appPaths);
                }
            }
            else
            {
                Core(trustedPlatformAssemblies, platformResourceRoots, appPaths);
            }

            void Core(string trustedPlatformAssemblies, string platformResourceRoots, string appPaths)
            {
                if (TrustedPlatformAssemblyMap != null)
                {
                    return;
                }

                //
                // Parse TrustedPlatformAssemblies
                //

                TrustedPlatformAssemblyMap = new Dictionary<string, TPAEntry>(StringComparer.InvariantCultureIgnoreCase);
                for (int i = 0; i < trustedPlatformAssemblies.Length;)
                {
                    if (!GetNextTPAPath(trustedPlatformAssemblies, ref i, dllOnly: false, out string fileName, out string simpleName, out bool isNativeImage))
                    {
                        break;
                    }

                    if (TrustedPlatformAssemblyMap.TryGetValue(simpleName, out TPAEntry existingEntry))
                    {
                        //
                        // We want to store only the first entry matching a simple name we encounter.
                        // The exception is if we first store an IL reference and later in the string
                        // we encounter a native image.  Since we don't touch IL in the presence of
                        // native images, we replace the IL entry with the NI.
                        //
                        if ((existingEntry.ILFileName != null && !isNativeImage) ||
                            (existingEntry.NIFileName != null && isNativeImage))
                        {
                            continue;
                        }
                    }

                    if (isNativeImage)
                    {
                        existingEntry.NIFileName = fileName;
                    }
                    else
                    {
                        existingEntry.ILFileName = fileName;
                    }

                    TrustedPlatformAssemblyMap[simpleName] = existingEntry;
                }

                //
                // Parse PlatformResourceRoots
                //

                for (int i = 0; i < platformResourceRoots.Length;)
                {
                    if (!GetNextPath(platformResourceRoots, ref i, out string pathName))
                    {
                        break;
                    }

                    if (IsRelativePath(pathName))
                    {
                        throw new ArgumentException(nameof(pathName));
                    }

                    _platformResourceRoots.Add(pathName);
                }

                //
                // Parse AppPaths
                //

                for (int i = 0; i < appPaths.Length;)
                {
                    if (!GetNextPath(appPaths, ref i, out string pathName))
                    {
                        break;
                    }


                    if (IsRelativePath(pathName))
                    {
                        throw new ArgumentException(nameof(pathName));
                    }

                    _appPaths.Add(pathName);
                }
            }
        }

        public void AddToFailureCache(AssemblyName assemblyName, Exception? failure)
        {
            FailureCache.Add(new FailureCacheKey(assemblyName), failure);
            IncrementVersion();
        }
    }
}
