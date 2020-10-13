// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Globalization;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides design-time support for licensing.
    /// </summary>
    public class DesigntimeLicenseContext : LicenseContext
    {
        internal Hashtable _savedLicenseKeys = new Hashtable();

        /// <summary>
        /// Gets or sets the license usage mode.
        /// </summary>
        public override LicenseUsageMode UsageMode => LicenseUsageMode.Designtime;

        /// <summary>
        /// Gets a saved license key.
        /// </summary>
        public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly) => null;

        /// <summary>
        /// Sets a saved license key.
        /// </summary>
        public override void SetSavedLicenseKey(Type type, string key)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _savedLicenseKeys[type.AssemblyQualifiedName] = key;
        }
    }

    internal class RuntimeLicenseContext : LicenseContext
    {
        internal Hashtable _savedLicenseKeys;

        /// <summary>
        /// This method takes a file URL and converts it to a local path. The trick here is that
        /// if there is a '#' in the path, everything after this is treated as a fragment. So
        /// we need to append the fragment to the end of the path.
        /// </summary>
        private string GetLocalPath(string fileName)
        {
            Debug.Assert(fileName is not null && fileName.Length > 0, "Cannot get local path, fileName is not valid");

            Uri uri = new Uri(fileName);
            return uri.LocalPath + uri.Fragment;
        }

        public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
        {
            if (_savedLicenseKeys is null || _savedLicenseKeys[type.AssemblyQualifiedName] is null)
            {
                if (_savedLicenseKeys is null)
                {
                    _savedLicenseKeys = new Hashtable();
                }

                if (resourceAssembly is null)
                {
                    resourceAssembly = Assembly.GetEntryAssembly();
                }

                if (resourceAssembly is null)
                {
                    // If Assembly.EntryAssembly returns null, then we will
                    // try everything.
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        // Assemblies loaded in memory return empty string from Location.
                        string location = asm.Location;
                        if (location == string.Empty)
                            continue;

                        string fileName = new FileInfo(location).Name;

                        Stream s = asm.GetManifestResourceStream(fileName + ".licenses");
                        if (s is null)
                        {
                            // Since the casing may be different depending on how the assembly was loaded,
                            // we'll do a case insensitive lookup for this manifest resource stream...
                            s = CaseInsensitiveManifestResourceStreamLookup(asm, fileName + ".licenses");
                        }

                        if (s is not null)
                        {
                            DesigntimeLicenseContextSerializer.Deserialize(s, fileName.ToUpperInvariant(), this);
                            break;
                        }
                    }
                }
                else
                {
                    string location = resourceAssembly.Location;
                    if (location != string.Empty)
                    {
                        string fileName = Path.GetFileName(location);
                        string licResourceName = fileName + ".licenses";

                        // First try the filename
                        Stream s = resourceAssembly.GetManifestResourceStream(licResourceName);
                        if (s is null)
                        {
                            string resolvedName = null;
                            CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;
                            string shortAssemblyName = resourceAssembly.GetName().Name;
                            // If the assembly has been renamed, we try our best to find a good match in the available resources
                            // by looking at the assembly name (which doesn't change even after a file rename) + ".exe.licenses" or + ".dll.licenses"
                            foreach (string existingName in resourceAssembly.GetManifestResourceNames())
                            {
                                if (comparer.Compare(existingName, licResourceName, CompareOptions.IgnoreCase) == 0 ||
                                 comparer.Compare(existingName, shortAssemblyName + ".exe.licenses", CompareOptions.IgnoreCase) == 0 ||
                                 comparer.Compare(existingName, shortAssemblyName + ".dll.licenses", CompareOptions.IgnoreCase) == 0)
                                {
                                    resolvedName = existingName;
                                    break;
                                }
                            }
                            if (resolvedName is not null)
                            {
                                s = resourceAssembly.GetManifestResourceStream(resolvedName);
                            }
                        }
                        if (s is not null)
                        {
                            DesigntimeLicenseContextSerializer.Deserialize(s, fileName.ToUpperInvariant(), this);
                        }
                    }
                }
            }
            return (string)_savedLicenseKeys[type.AssemblyQualifiedName];
        }

        /**
        * Looks up a .licenses file in the assembly manifest using
        * case-insensitive lookup rules. We do this because the name
        * we are attempting to locate could have different casing
        * depending on how the assembly was loaded.
        **/
        private Stream CaseInsensitiveManifestResourceStreamLookup(Assembly satellite, string name)
        {
            CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;

            // Loop through the resource names in the assembly.
            // We try to handle the case where the assembly file name has been renamed
            // by trying to guess the original file name based on the assembly name.
            string assemblyShortName = satellite.GetName().Name;
            foreach (string existingName in satellite.GetManifestResourceNames())
            {
                if (comparer.Compare(existingName, name, CompareOptions.IgnoreCase) == 0 ||
                    comparer.Compare(existingName, assemblyShortName + ".exe.licenses") == 0 ||
                    comparer.Compare(existingName, assemblyShortName + ".dll.licenses") == 0)
                {
                    name = existingName;
                    break;
                }
            }

            // Finally, attempt to return our stream based on the
            // case insensitive match we found.
            return satellite.GetManifestResourceStream(name);
        }
    }
}
