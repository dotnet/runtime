// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_APPX
using Internal.Resources;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text; // For UriEncode
using Windows.ApplicationModel.Resources.Core;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace System.Resources
{
#if FEATURE_APPX
    // Please see the comments regarding thread safety preceding the implementations
    // of Initialize() and GetString() below.
    internal sealed class WindowsRuntimeResourceManager : WindowsRuntimeResourceManagerBase
    {
        // Setting invariant culture to Windows Runtime doesn't work because Windows Runtime expects language name in the form of BCP-47 tags while
        // invariant name is an empty string. We will use the private invariant culture name x-VL instead.
        private const string c_InvariantCulturePrivateName = "x-VL";

        private ResourceMap _resourceMap;
        private ResourceContext _clonedResourceContext;
        private string _clonedResourceContextFallBackList;

        private static char[] s_charCultureSeparator;

        private struct PackageInfo
        {
            public string Path;
            public string Name;
            public string FullName;
        }

        private static PackageInfo s_currentPackageInfo;

        private static List<PackageInfo> s_dependentPackageInfoList;

        private static ResourceContext s_globalResourceContext; // Read from it but do not modify it or call Reset() on it as that would affect the app-wide state

        private static volatile string s_globalResourceContextFallBackList;
        private static volatile CultureInfo s_globalResourceContextBestFitCultureInfo;
        private static volatile global::Windows.ApplicationModel.Resources.Core.ResourceManager s_globalResourceManager;

        private static Object s_objectForLock = new Object(); // Used by InitializeStatics

        private static bool InitializeStatics()
        {
            global::Windows.ApplicationModel.Resources.Core.ResourceManager globalResourceManager = null;

            if (s_globalResourceManager == null)
            {
                lock (s_objectForLock)
                {
                    if (s_globalResourceManager == null)
                    {
                        globalResourceManager = global::Windows.ApplicationModel.Resources.Core.ResourceManager.Current;

                        InitializeStaticGlobalResourceContext(globalResourceManager);

                        s_charCultureSeparator = new char[] { ';' };

                        Package currentPackage = Package.Current;
                        if (currentPackage != null)
                        {
                            StorageFolder currentPackageLocation = currentPackage.InstalledLocation;

                            s_currentPackageInfo.Path = null;
                            s_currentPackageInfo.Name = null;
                            s_currentPackageInfo.FullName = null;

                            if (currentPackageLocation != null)
                                s_currentPackageInfo.Path = currentPackageLocation.Path;

                            PackageId id = currentPackage.Id;
                            if (id != null)
                            {
                                s_currentPackageInfo.Name = id.Name; // We do not enforce this to be non-null.
                                s_currentPackageInfo.FullName = id.FullName;
                            }
                        }

                        if (globalResourceManager != null &&
                            s_globalResourceContext != null &&
                            s_globalResourceContextFallBackList != null &&
                            s_globalResourceContextFallBackList.Length > 0 && // Should never be empty
                            s_charCultureSeparator != null &&
                            s_currentPackageInfo.Path != null)
                            s_globalResourceManager = globalResourceManager;
                    }
                }
            }

            return s_globalResourceManager != null;
        }

        private static void InitializeStaticGlobalResourceContext(global::Windows.ApplicationModel.Resources.Core.ResourceManager resourceManager)
        {
            if (s_globalResourceContext == null)
            {
                lock (s_objectForLock)
                {
                    if (s_globalResourceContext == null)
                    {
                        resourceManager = resourceManager ?? global::Windows.ApplicationModel.Resources.Core.ResourceManager.Current;

                        if (resourceManager != null)
                        {
#pragma warning disable 618
                            ResourceContext globalResourceContext = resourceManager.DefaultContext;
#pragma warning restore 618
                            if (globalResourceContext != null)
                            {
                                List<String> languages = new List<string>(globalResourceContext.Languages);

                                s_globalResourceContextBestFitCultureInfo = GetBestFitCultureFromLanguageList(languages);
                                s_globalResourceContextFallBackList = ReadOnlyListToString(languages);
                                s_globalResourceContext = globalResourceContext;
                                s_globalResourceContext.QualifierValues.MapChanged += new MapChangedEventHandler<string, string>(GlobalResourceContextChanged);
                            }
                        }
                    }
                }
            }
        }


        // Returns the CultureInfo representing the first language in the list that we can construct a CultureInfo for or null if
        // no such culture exists.
        private static unsafe CultureInfo GetBestFitCultureFromLanguageList(List<string> languages)
        {
            char* localeNameBuffer = stackalloc char[Interop.Kernel32.LOCALE_NAME_MAX_LENGTH]; // LOCALE_NAME_MAX_LENGTH includes null terminator

            for (int i = 0; i < languages.Count; i++)
            {
                if (WindowsRuntimeResourceManagerBase.IsValidCulture(languages[i]))
                {
                    return new CultureInfo(languages[i]);
                }

                int result = Interop.Kernel32.ResolveLocaleName(languages[i], localeNameBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH); 
                if (result != 0)
                {
                    string localeName = new string(localeNameBuffer, 0, result - 1); // result length includes null terminator

                    if (WindowsRuntimeResourceManagerBase.IsValidCulture(localeName))
                    {
                        return new CultureInfo(localeName);
                    }
                }
            }

            return null;
        }
        // Can be called independently of/simultaneously with InitializeStatics.
        // Does not use the results of InitializeStatics.
        // Does not require the use of a lock because, given that the results of all the callees of
        // this function are idempotent for the process lifetime, this function will always store
        // the same information in s_dependentPackagePath no matter how many times it is called.
        private static void InitializeStaticsForDependentPackages()
        {
            if (s_dependentPackageInfoList == null)
            {
                // Create an empty list. If there are no dependencies, this will be our way of knowing that we ran
                // through this function once.
                List<PackageInfo> dependentPackageInfoList = new List<PackageInfo>();

                // We call Package.Current here and in InitializeStatics. This may cause a small perf hit.
                // In theory we could have cached it as a static variable.
                // However, we don't want to have to keep a reference to it alive for the lifetime of the AppDomain.
                // Also having InitializeStaticsForDependentPackages not depend on InitializeStatics leads to a simpler design.
                Package currentPackage = Package.Current;

                if (currentPackage != null)
                {
                    IReadOnlyList<Package> dependencies = currentPackage.Dependencies;
                    if (dependencies != null)
                    {
                        int dependenciesCount = dependencies.Count;

                        if (dependenciesCount > 0)
                        {
                            // We have dependencies. Throw away the old empty list, and create a list with
                            // capacity exactly equal to the number of dependencies.
                            dependentPackageInfoList = new List<PackageInfo>(dependenciesCount);

                            foreach (Package package in dependencies)
                            {
                                if (package != null)
                                {
                                    StorageFolder dependentPackageLocation = package.InstalledLocation;
                                    PackageInfo dependentPackageInfo;

                                    dependentPackageInfo.Path = null;
                                    dependentPackageInfo.Name = null;
                                    dependentPackageInfo.FullName = null;

                                    if (dependentPackageLocation != null)
                                        dependentPackageInfo.Path = dependentPackageLocation.Path;

                                    PackageId id = package.Id;
                                    if (id != null)
                                    {
                                        dependentPackageInfo.Name = id.Name;
                                        dependentPackageInfo.FullName = id.FullName;
                                    }

                                    dependentPackageInfoList.Add(dependentPackageInfo);
                                }
                            }
                        }
                    }
                }

                // Assign even if the list is empty. That way we know we ran through this function once.
                s_dependentPackageInfoList = dependentPackageInfoList;
            }

            Debug.Assert(s_dependentPackageInfoList != null);
        }

        private static void GlobalResourceContextChanged(object sender, IMapChangedEventArgs<string> e)
        {
            Debug.Assert(s_globalResourceContextFallBackList != null);
            Debug.Assert(s_globalResourceContext != null);

            IReadOnlyList<string> langs;

            try
            {
                langs = s_globalResourceContext.Languages;
            }
            catch (ArgumentException)
            {
                // Sometimes Windows Runtime fails and we get Argument Exception which can fail fast the whole app
                // to avoid that we ignore the exception.
                return;
            }

            List<String> languages = new List<string>(langs);
            
            if (languages.Count > 0 && languages[0] == c_InvariantCulturePrivateName)
            {
                languages[0] = CultureInfo.InvariantCulture.Name;
            }
            
            s_globalResourceContextBestFitCultureInfo = GetBestFitCultureFromLanguageList(languages);
            s_globalResourceContextFallBackList = ReadOnlyListToString(languages);
        }

        private static bool LibpathMatchesPackagepath(String libpath, String packagepath)
        {
            Debug.Assert(libpath != null);
            Debug.Assert(packagepath != null);

            return packagepath.Length < libpath.Length &&
                   String.Compare(packagepath, 0,
                                  libpath, 0,
                                  packagepath.Length,
                                  StringComparison.OrdinalIgnoreCase) == 0 &&
                   // Ensure wzPackagePath is not just a prefix, but a path prefix
                   // This says: packagepath is c:\foo || c:\foo\
                   (libpath[packagepath.Length] == '\\' || packagepath.EndsWith("\\"));
        }

#if netstandard
        /* Returns true if libpath is path to an ni image and if the path contains packagename as a subfolder */
        private static bool LibpathContainsPackagename(String libpath, String packagename)
        {
            Debug.Assert(libpath != null);
            Debug.Assert(packagename != null);

            return libpath.IndexOf("\\" + packagename + "\\", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   (libpath.EndsWith("ni.dll", StringComparison.OrdinalIgnoreCase) || libpath.EndsWith("ni.exe", StringComparison.OrdinalIgnoreCase));
        }
#endif

        private static string FindPackageSimpleNameForFilename(string libpath)
        {
            Debug.Assert(libpath != null);
            Debug.Assert(s_currentPackageInfo.Path != null); // Set in InitializeStatics()
            // s_currentPackageInfo.Name may be null (see note below)

            if (LibpathMatchesPackagepath(libpath, s_currentPackageInfo.Path))
                return s_currentPackageInfo.Name; // This may be null, in which case we failed to get the name (in InitializeStatics), but matched the path, so stop looking.
            else // Look at dependent packages
            {
                InitializeStaticsForDependentPackages();

                // s_dependentPackageInfoList is empty (but non-null) if there are no dependent packages.
                foreach (PackageInfo dependentPackageInfo in s_dependentPackageInfoList)
                {
                    if (LibpathMatchesPackagepath(libpath, dependentPackageInfo.Path))
                        return dependentPackageInfo.Name; // This may be null, in which case we failed to get the name (in InitializeStaticsForDependentPackages), but matched the path, so stop looking.
                }
            }

#if netstandard
            /* On phone libpath is usually ni path and not IL path as we do not touch the IL on phone.
               On Phone NI images are no longer under package root. Due to this above logic fails to
               find the package to which the library belongs. We assume that NI paths usually have
               package name as subfolder in its path. Based on this assumption we can find the package
               to which an NI belongs. Below code does that.
              */
            if (LibpathContainsPackagename(libpath, s_currentPackageInfo.FullName))
                return s_currentPackageInfo.Name;
            else // Look at dependent packages
            {
                // s_dependentPackageInfoList is empty (but non-null) if there are no dependent packages.
                foreach (PackageInfo dependentPackageInfo in s_dependentPackageInfoList)
                {
                    if (LibpathContainsPackagename(libpath, dependentPackageInfo.FullName))
                        return dependentPackageInfo.Name;
                }
            }
#endif
            return null;
        }

        // Obtain instances of the Resource Map and Resource Context provided by
        // the Windows Modern Resource Manager (MRM).

        // Not thread-safe. Only call this once on one thread for each object instance.
        // For example, System.Runtime.ResourceManager only calls this from its constructors,
        // guaranteeing that this only gets called once, on one thread, for each new instance
        // of System.Runtime.ResourceManager.

        // Throws exceptions
        // Only returns true if the function succeeded completely.
        // Outputs exceptionInfo since it may be needed for debugging purposes
        // if an exception is thrown by one of Initialize's callees.
        public override bool Initialize(string libpath, string reswFilename, out PRIExceptionInfo exceptionInfo)
        {
            Debug.Assert(libpath != null);
            Debug.Assert(reswFilename != null);
            exceptionInfo = null;

            if (InitializeStatics())
            {
                // AllResourceMaps can throw ERROR_MRM_MAP_NOT_FOUND,
                // although in that case we are not sure for which package it failed to find
                // resources (if we are looking for resources for a framework package,
                // it might throw ERROR_MRM_MAP_NOT_FOUND if the app package
                // resources could not be loaded, even if the framework package
                // resources are properly configured). So we will not fill in the
                // exceptionInfo structure at this point since we don't have any
                // reliable information to include in it.

                IReadOnlyDictionary<String, ResourceMap>
                    resourceMapDictionary = s_globalResourceManager.AllResourceMaps;

                if (resourceMapDictionary != null)
                {
                    string packageSimpleName = FindPackageSimpleNameForFilename(libpath);

#if netstandard
                    // If we have found a simple package name for the assembly, lets make sure it is not *.resource.dll that
                    // an application may have packaged in its AppX. This is to enforce AppX apps to use PRI resources.
                    if (packageSimpleName != null)
                    {
                        if (packageSimpleName.EndsWith(".resources.dll", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Pretend we didn't get a package name. When an attempt is made to get resource string, GetString implementation
                            // will see that we are going to use modern resource manager but we don't have PRI and will thrown an exception indicating
                            // so. This will force the developer to have a valid PRI based resource.
                            packageSimpleName = null;
                        }
                    }
#endif //  netstandard
                    if (packageSimpleName != null)
                    {
                        ResourceMap packageResourceMap = null;

                        // Win8 enforces that package simple names are unique (for example, the App Store will not
                        // allow two apps with the same package simple name). That is why the Modern Resource Manager
                        // keys access to resources based on the package simple name.
                        if (resourceMapDictionary.TryGetValue(packageSimpleName, out packageResourceMap))
                        {
                            if (packageResourceMap != null)
                            {
                                // GetSubtree returns null when it cannot find resource strings
                                // named "reswFilename/*" for the package we are looking up.

                                reswFilename = UriUtility.UriEncode(reswFilename);
                                _resourceMap = packageResourceMap.GetSubtree(reswFilename);

                                if (_resourceMap == null)
                                {
                                    exceptionInfo = new PRIExceptionInfo();
                                    exceptionInfo.PackageSimpleName = packageSimpleName;
                                    exceptionInfo.ResWFile = reswFilename;
                                }
                                else
                                {
                                    _clonedResourceContext = s_globalResourceContext.Clone();

                                    if (_clonedResourceContext != null)
                                    {
                                        // Will need to be changed the first time it is used. But we can't set it to "" since we will take a lock on it.
                                        _clonedResourceContextFallBackList = ReadOnlyListToString(_clonedResourceContext.Languages);

                                        if (_clonedResourceContextFallBackList != null)
                                            return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static IReadOnlyList<string> StringToReadOnlyList(string s)
        {
            Debug.Assert(s != null);
            Debug.Assert(s_charCultureSeparator != null);

            List<string> newList = new List<string>(s.Split(s_charCultureSeparator));
            return newList.AsReadOnly();
        }

        private static string ReadOnlyListToString(IReadOnlyList<string> list)
        {
            Debug.Assert(list != null);

            return String.Join(";", list);
        }

        public override CultureInfo GlobalResourceContextBestFitCultureInfo
        {
            get
            {
                InitializeStaticGlobalResourceContext(null);
                return s_globalResourceContextBestFitCultureInfo;
            }
        }

        // This method will set the culture ci as default language in the default resource context
        // which is global for the whole app
        public override bool SetGlobalResourceContextDefaultCulture(CultureInfo ci)
        {
            Debug.Assert(ci != null);
            InitializeStaticGlobalResourceContext(null);

            if (s_globalResourceContext == null)
            {
                return false;
            }

            if (s_globalResourceContextBestFitCultureInfo != null && s_globalResourceContextBestFitCultureInfo.Name.Equals(ci.Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!ReferenceEquals(s_globalResourceContextBestFitCultureInfo, ci))
                {
                    // We have same culture name but different reference, we'll need to update s_globalResourceContextBestFitCultureInfo only as ci can 
                    // be a customized subclassed culture which setting different values for NFI, DTFI...etc.
                    s_globalResourceContextBestFitCultureInfo = ci;
                }

                // the default culture is already set. nothing more need to be done
                return true;
            }

            List<String> languages = new List<String>(s_globalResourceContext.Languages);
            languages.Insert(0, ci.Name == CultureInfo.InvariantCulture.Name ? c_InvariantCulturePrivateName : ci.Name);

            // remove any duplication in the list
            int i = languages.Count - 1;
            while (i > 0)
            {
                if (languages[i].Equals(ci.Name, StringComparison.OrdinalIgnoreCase))
                {
                    languages.RemoveAt(i);
                }
                i--;
            }
            s_globalResourceContext.Languages = languages;

            // update s_globalResourceContextBestFitCultureInfo and don't wait to be overridden by GlobalResourceContextChanged
            // to avoid any timing problem
            s_globalResourceContextBestFitCultureInfo = ci;
            return true;
        }

        // Obtains a resource string given the name of the string resource, and optional starting
        // and neutral resource cultures (e.g. "de-DE").

        // Thread-safe provided that the call to Initialize() happened only once on one thread
        // and has already completed successfully, and that the WinRT APIs called below
        // continue to be thread-safe.

        // Throws exceptions
        public override String GetString(String stringName,
                 String startingCulture, String neutralResourcesCulture)
        {
            Debug.Assert(stringName != null);
            Debug.Assert(_resourceMap != null); // Should have been initialized by now

            ResourceCandidate resourceCandidate = null;

            stringName = UriUtility.UriEncode(stringName);

            if (startingCulture == null && neutralResourcesCulture == null)
            {
#pragma warning disable 618
                resourceCandidate = _resourceMap.GetValue(stringName);
#pragma warning restore 618
            }
            else
            {
                Debug.Assert(_clonedResourceContext != null); // Should have been initialized by now
                Debug.Assert(_clonedResourceContextFallBackList != null); // Should have been initialized by now
                Debug.Assert(s_globalResourceContextFallBackList != null); // Should have been initialized by now
                Debug.Assert(s_globalResourceContextFallBackList.Length > 0); // Should never be empty

                // We need to modify the culture fallback list used by the Modern Resource Manager
                // The starting culture has to be looked up first, and neutral resources culture has
                // to be looked up last.

                string newResourceFallBackList = null;

                newResourceFallBackList =
                    (startingCulture == null ? "" : startingCulture + ";") +
                       s_globalResourceContextFallBackList + // Our tests do not test this line of code, so be extra careful if you modify or move it.
                    (neutralResourcesCulture == null ? "" : ";" + neutralResourcesCulture);

                lock (_clonedResourceContext)
                {
                    // s_globalResourceContextFallBackList may have changed on another thread by now.
                    // We cannot prevent that from happening because it may have happened on a native
                    // thread, and we do not share the same lock mechanisms with native code.
                    // The worst that can happen is that a string is unexpectedly missing
                    // or in the wrong language.

                    if (!String.Equals(newResourceFallBackList, _clonedResourceContextFallBackList, StringComparison.Ordinal))
                    {
                        _clonedResourceContext.Languages = StringToReadOnlyList(newResourceFallBackList);
                        _clonedResourceContextFallBackList = newResourceFallBackList;
                    }

                    resourceCandidate = _resourceMap.GetValue(stringName, _clonedResourceContext);
                }
            }

            if (resourceCandidate != null)
                return resourceCandidate.ValueAsString;

            return null;
        }
    }

    // Adapted from the UrlEncode functions at file:alm\tfs_core\framework\common\UriUtility\HttpUtility.cs
    // using methods which were in turn borrowed from
    // file:ndp\fx\src\xsp\system\web\util\httpencoder.cs
    // file:ndp\fx\src\xsp\system\web\httpserverutility.cs
    // and file:ndp\fx\src\xsp\system\web\util\httpencoderutility.cs
    internal static class UriUtility
    {
        // This method percent encodes the UTF8 encoding of a string.
        // All characters are percent encoded except the unreserved characters from RFC 3986.

        // We cannot use the System.Uri class since by default it respects
        // the set of unreserved characters from RFC 2396, which is different
        // by the 5 characters !*'()

        // Adapted from the UrlEncode methods originally
        // in file:ndp\fx\src\xsp\system\web\httpserverutility.cs
        // and file:ndp\fx\src\xsp\system\web\util\httpencoder.cs
        public static string UriEncode(string str)
        {
            if (str == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(str);

            int count = bytes.Length;

            if (count == 0)
                return null;

            int cBytesToEncode = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                if (!IsUriUnreservedChar((char)bytes[i]))
                    cBytesToEncode++;
            }

            byte[] expandedBytes = null;

            // nothing to expand?
            if (cBytesToEncode == 0)
                expandedBytes = bytes;
            else
            {
                // expand not 'safe' characters into %xx
                expandedBytes = new byte[count + cBytesToEncode * 2];
                int pos = 0;

                for (int i = 0; i < count; i++)
                {
                    byte b = bytes[i];

                    if (IsUriUnreservedChar((char)b))
                    {
                        expandedBytes[pos++] = b;
                    }
                    else
                    {
                        expandedBytes[pos++] = (byte)'%';
                        expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                        expandedBytes[pos++] = (byte)IntToHex(b & 0xf);
                    }
                }
            }

            // Since all the bytes in expandedBytes are ascii characters UTF8 encoding can be used
            return Encoding.UTF8.GetString(expandedBytes);
        }

        // Adapted from IntToHex originally in file:ndp\fx\src\xsp\system\web\util\httpencoderutility.cs
        private static char IntToHex(int n)
        {
            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'a');
        }

        // Modified from IsUrlSafeChar originally in file:ndp\fx\src\xsp\system\web\util\httpencoderutility.cs
        // Set of unreserved characters from RFC 3986
        private static bool IsUriUnreservedChar(char ch)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                return true;

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '~':
                    return true;
            }

            return false;
        }
    }
#endif //FEATURE_APPX
}
