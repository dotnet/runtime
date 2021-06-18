// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Provides methods to select from multiple bitmaps depending on a "bitmapSuffix" config setting.
    /// </summary>
    internal static class BitmapSelector
    {
        /// <summary>
        /// Gets the bitmap ID suffix defined in the application configuration, or string.Empty if
        /// the suffix is not specified.  Internal for unit tests
        /// </summary>
        /// <remarks>
        /// For performance, the suffix is cached in a static variable so it only has to be read
        /// once per appdomain.
        /// </remarks>
        private static string? s_suffix;
        internal static string? Suffix
        {
            get
            {
                // NOTE: This value is read from the "SystemDrawingSection" of the ConfigurationManager on
                // the .NET Framework. To avoid pulling in a direct dependency to that assembly, we are not
                // reading the value in this implementation.
                return s_suffix;
            }
            set
            {
                // So unit tests can clear the cached suffix
                s_suffix = value;
            }
        }

        /// <summary>
        /// Appends the current suffix to <paramref name="filePath"/>.  The suffix is appended
        /// before the existing extension (if any).  Internal for unit tests.
        /// </summary>
        /// <returns>
        /// The new path with the suffix included.  If there is no suffix defined or there are
        /// invalid characters in the original path, the original path is returned.
        /// </returns>
        internal static string AppendSuffix(string filePath)
        {
            try
            {
                return Path.ChangeExtension(filePath, Suffix + Path.GetExtension(filePath));
            }
            catch (ArgumentException)
            { // there are invalid characters in the path
                return filePath;
            }
        }

        /// <summary>
        /// Returns <paramref name="originalPath"/> with the current suffix appended (before the
        /// existing extension) if the resulting file path exists; otherwise the original path is
        /// returned.
        /// </summary>
        public static string GetFileName(string originalPath)
        {
            if (string.IsNullOrEmpty(Suffix))
                return originalPath;

            string newPath = AppendSuffix(originalPath);
            return File.Exists(newPath) ? newPath : originalPath;
        }

        // Calls assembly.GetManifestResourceStream in a try/catch and returns null if not found
        private static Stream? GetResourceStreamHelper(Assembly assembly, Type type, string name)
        {
            Stream? stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(type, name);
            }
            catch (FileNotFoundException)
            {
            }
            return stream;
        }

        [RequiresUnreferencedCode("Calls Assembly.GetType which may be trimmed")]
        private static bool DoesAssemblyHaveCustomAttribute(Assembly assembly, string typeName)
        {
            return DoesAssemblyHaveCustomAttribute(assembly, assembly.GetType(typeName));
        }

        private static bool DoesAssemblyHaveCustomAttribute(Assembly assembly, Type? attrType)
        {
            if (attrType != null)
            {
                var attr = assembly.GetCustomAttributes(attrType, false);
                if (attr.Length > 0)
                {
                    return true;
                }
            }
            return false;
        }

        // internal for unit tests
        internal static bool SatelliteAssemblyOptIn(Assembly assembly)
        {
            // Try 4.5 public attribute type first
            if (DoesAssemblyHaveCustomAttribute(assembly, typeof(BitmapSuffixInSatelliteAssemblyAttribute)))
            {
                return true;
            }

            // Also load attribute type by name for dlls compiled against older frameworks
            return DoesAssemblyHaveCustomAttribute(assembly, "System.Drawing.BitmapSuffixInSatelliteAssemblyAttribute");
        }

        // internal for unit tests
        internal static bool SameAssemblyOptIn(Assembly assembly)
        {
            // Try 4.5 public attribute type first
            if (DoesAssemblyHaveCustomAttribute(assembly, typeof(BitmapSuffixInSameAssemblyAttribute)))
            {
                return true;
            }

            // Also load attribute type by name for dlls compiled against older frameworks
            return DoesAssemblyHaveCustomAttribute(assembly, "System.Drawing.BitmapSuffixInSameAssemblyAttribute");
        }

        /// <summary>
        /// Returns a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="assembly">The assembly from which the stream is loaded</param>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The manifest resource stream corresponding to <paramref name="originalName"/> with the
        /// current suffix applied; or if that is not found, the stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Stream? GetResourceStream(Assembly assembly, Type type, string originalName)
        {
            if (Suffix != string.Empty)
            {
                try
                {
                    // Resource with suffix has highest priority
                    if (SameAssemblyOptIn(assembly))
                    {
                        string newName = AppendSuffix(originalName);
                        Stream? stream = GetResourceStreamHelper(assembly, type, newName);
                        if (stream != null)
                        {
                            return stream;
                        }
                    }
                }
                catch
                {
                    // Ignore failures and continue to try other options
                }

                try
                {
                    // Satellite assembly has second priority, using the original name
                    if (SatelliteAssemblyOptIn(assembly))
                    {
                        AssemblyName assemblyName = assembly.GetName();
                        assemblyName.Name += Suffix;
                        assemblyName.ProcessorArchitecture = ProcessorArchitecture.None;
                        Assembly satellite = Assembly.Load(assemblyName);
                        if (satellite != null)
                        {
                            Stream? stream = GetResourceStreamHelper(satellite, type, originalName);
                            if (stream != null)
                            {
                                return stream;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore failures and continue to try other options
                }
            }

            // Otherwise fall back to specified assembly and original name requested
            return assembly.GetManifestResourceStream(type, originalName);
        }

        /// <summary>
        /// Returns a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="type">The type from whose assembly the stream is loaded and whose namespace is used to scope the resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The manifest resource stream corresponding to <paramref name="originalName"/> with the
        /// current suffix applied; or if that is not found, the stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Stream? GetResourceStream(Type type, string originalName)
        {
            return GetResourceStream(type.Module.Assembly, type, originalName);
        }

        /// <summary>
        /// Returns an Icon created  from a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="type">The type from whose assembly the stream is loaded and whose namespace is used to scope the resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The icon created from a manifest resource stream corresponding to <paramref name="originalName"/> with the
        /// current suffix applied; or if that is not found, the stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Icon CreateIcon(Type type, string originalName)
        {
            return new Icon(GetResourceStream(type, originalName)!);
        }

        /// <summary>
        /// Returns an Bitmap created  from a resource stream loaded from the appropriate location according to the current
        /// suffix.
        /// </summary>
        /// <param name="type">The type from whose assembly the stream is loaded and whose namespace is used to scope the resource name</param>
        /// <param name="originalName">The name of the manifest resource being requested</param>
        /// <returns>
        /// The bitmap created from a manifest resource stream corresponding to <paramref name="originalName"/> with the
        /// current suffix applied; or if that is not found, the stream corresponding to <paramref name="originalName"/>.
        /// </returns>
        public static Bitmap CreateBitmap(Type type, string originalName)
        {
            return new Bitmap(GetResourceStream(type, originalName)!);
        }
    }
}
