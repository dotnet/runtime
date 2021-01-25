// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Resources
{
    // Resource Manager exposes an assembly's resources to an application for
    // the correct CultureInfo.  An example would be localizing text for a
    // user-visible message.  Create a set of resource files listing a name
    // for a message and its value, compile them using ResGen, put them in
    // an appropriate place (your assembly manifest(?)), then create a Resource
    // Manager and query for the name of the message you want.  The Resource
    // Manager will use CultureInfo.GetCurrentUICulture() to look
    // up a resource for your user's locale settings.
    //
    // Users should ideally create a resource file for every culture, or
    // at least a meaningful subset.  The filenames will follow the naming
    // scheme:
    //
    // basename.culture name.resources
    //
    // The base name can be the name of your application, or depending on
    // the granularity desired, possibly the name of each class.  The culture
    // name is determined from CultureInfo's Name property.
    // An example file name may be MyApp.en-US.resources for
    // MyApp's US English resources.
    //
    // -----------------
    // Refactoring Notes
    // -----------------
    // In Feb 08, began first step of refactoring ResourceManager to improve
    // maintainability (sd changelist 3012100). This resulted in breaking
    // apart the InternalGetResourceSet "big loop" so that the file-based
    // and manifest-based lookup was located in separate methods.
    // In Apr 08, continued refactoring so that file-based and manifest-based
    // concerns are encapsulated by separate classes. At construction, the
    // ResourceManager creates one of these classes based on whether the
    // RM will need to use file-based or manifest-based resources, and
    // afterwards refers to this through the interface IResourceGroveler.
    //
    // Serialization Compat: Ideally, we could have refactored further but
    // this would have broken serialization compat. For example, the
    // ResourceManager member UseManifest and UseSatelliteAssem are no
    // longer relevant on ResourceManager. Similarly, other members could
    // ideally be moved to the file-based or manifest-based classes
    // because they are only relevant for those types of lookup.
    //
    // Solution now / in the future:
    // For now, we simply use a mediator class so that we can keep these
    // members on ResourceManager but allow the file-based and manifest-
    // based classes to access/set these members in a uniform way. See
    // ResourceManagerMediator.
    // We encapsulate fallback logic in a fallback iterator class, so that
    // this logic isn't duplicated in several methods.
    //
    // In the future, we can also look into further factoring and better
    // design of IResourceGroveler interface to accommodate unused parameters
    // that don't make sense for either file-based or manifest-based lookup paths.
    //
    // Benefits of this refactoring:
    // - Makes it possible to understand what the ResourceManager does,
    // which is key for maintainability.
    // - Makes the ResourceManager more extensible by identifying and
    // encapsulating what varies
    // - Unearthed a bug that's been lurking a while in file-based
    // lookup paths for InternalGetResourceSet if createIfNotExists is
    // false.
    // - Reuses logic, e.g. by breaking apart the culture fallback into
    // the fallback iterator class, we don't have to repeat the
    // sometimes confusing fallback logic across multiple methods
    // - Fxcop violations reduced to 1/5th of original count. Most
    // importantly, code complexity violations disappeared.
    // - Finally, it got rid of dead code paths. Because the big loop was
    // so confusing, it masked unused chunks of code. Also, dividing
    // between file-based and manifest-based allowed functionaliy
    // unused in silverlight to fall out.
    //
    // Note: this type is integral to the construction of exception objects,
    // and sometimes this has to be done in low memory situtations (OOM) or
    // to create TypeInitializationExceptions due to failure of a static class
    // constructor. This type needs to be extremely careful and assume that
    // any type it references may have previously failed to construct, so statics
    // belonging to that type may not be initialized. FrameworkEventSource.Log
    // is one such example.
    //

    public partial class ResourceManager
    {
        internal class CultureNameResourceSetPair
        {
            public string? lastCultureName;
            public ResourceSet? lastResourceSet;
        }

        protected string BaseNameField;
        protected Assembly? MainAssembly;    // Need the assembly manifest sometimes.

        private Dictionary<string, ResourceSet>? _resourceSets;
        private readonly string? _moduleDir;          // For assembly-ignorant directory location
        private readonly Type? _locationInfo;         // For Assembly or type-based directory layout

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        private readonly Type? _userResourceSet;      // Which ResourceSet instance to create

        private CultureInfo? _neutralResourcesCulture;  // For perf optimizations.

        private CultureNameResourceSetPair? _lastUsedResourceCache;

        private bool _ignoreCase;   // Whether case matters in GetString & GetObject

        private bool _useManifest;  // Use Assembly manifest, or grovel disk.

        // Whether to fall back to the main assembly or a particular
        // satellite for the neutral resources.
        private UltimateResourceFallbackLocation _fallbackLoc;
        // Version number of satellite assemblies to look for.  May be null.
        private Version? _satelliteContractVersion;
        private bool _lookedForSatelliteContractVersion;

        private IResourceGroveler _resourceGroveler;

        public static readonly int MagicNumber = unchecked((int)0xBEEFCACE);  // If only hex had a K...

        // Version number so ResMgr can get the ideal set of classes for you.
        // ResMgr header is:
        // 1) MagicNumber (little endian Int32)
        // 2) HeaderVersionNumber (little endian Int32)
        // 3) Num Bytes to skip past ResMgr header (little endian Int32)
        // 4) IResourceReader type name for this file (bytelength-prefixed UTF-8 String)
        // 5) ResourceSet type name for this file (bytelength-prefixed UTF8 String)
        public static readonly int HeaderVersionNumber = 1;

        // It would be better if we could use a _neutralCulture instead of calling
        // CultureInfo.InvariantCulture everywhere, but we run into problems with the .cctor.  CultureInfo
        // initializes assembly, which initializes ResourceManager, which tries to get a CultureInfo which isn't
        // there yet because CultureInfo's class initializer hasn't finished.  If we move SystemResMgr off of
        // Assembly (or at least make it an internal property) we should be able to circumvent this problem.

        // This is our min required ResourceSet type.
        private static readonly Type s_minResourceSet = typeof(ResourceSet);
        // These Strings are used to avoid using Reflection in CreateResourceSet.
        internal const string ResReaderTypeName = "System.Resources.ResourceReader";
        internal const string ResSetTypeName = "System.Resources.RuntimeResourceSet";
        internal const string ResFileExtension = ".resources";
        internal const int ResFileExtensionLength = 10;

        protected ResourceManager()
        {
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            _resourceGroveler = new ManifestBasedResourceGroveler(mediator);
            BaseNameField = string.Empty;
        }

        // Constructs a Resource Manager for files beginning with
        // baseName in the directory specified by resourceDir
        // or in the current directory.  This Assembly-ignorant constructor is
        // mostly useful for testing your own ResourceSet implementation.
        //
        // A good example of a baseName might be "Strings".  BaseName
        // should not end in ".resources".
        //
        // Note: System.Windows.Forms uses this method at design time.
        //
        private ResourceManager(string baseName, string resourceDir,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type? userResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));
            if (null == resourceDir)
                throw new ArgumentNullException(nameof(resourceDir));

            BaseNameField = baseName;

            _moduleDir = resourceDir;
            _userResourceSet = userResourceSet;
            _resourceSets = new Dictionary<string, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();

            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            _resourceGroveler = new FileBasedResourceGroveler(mediator);
        }

        public ResourceManager(string baseName, Assembly assembly)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));
            if (null == assembly)
                throw new ArgumentNullException(nameof(assembly));
            if (!assembly.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            MainAssembly = assembly;
            BaseNameField = baseName;

            CommonAssemblyInit();
        }

        public ResourceManager(string baseName, Assembly assembly,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        Type? usingResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));
            if (null == assembly)
                throw new ArgumentNullException(nameof(assembly));
            if (!assembly.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            MainAssembly = assembly;
            BaseNameField = baseName;

            if (usingResourceSet != null && (usingResourceSet != s_minResourceSet) && !usingResourceSet.IsSubclassOf(s_minResourceSet))
                throw new ArgumentException(SR.Arg_ResMgrNotResSet, nameof(usingResourceSet));
            _userResourceSet = usingResourceSet;

            CommonAssemblyInit();
        }

        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
                throw new ArgumentNullException(nameof(resourceSource));
            if (!resourceSource.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeType);

            _locationInfo = resourceSource;
            MainAssembly = _locationInfo.Assembly;
            BaseNameField = resourceSource.Name;

            CommonAssemblyInit();
        }

        // Trying to unify code as much as possible, even though having to do a
        // security check in each constructor prevents it.
        [MemberNotNull(nameof(_resourceGroveler))]
        private void CommonAssemblyInit()
        {
            // Now we can use the managed resources even when using PRI's to support the APIs GetObject, GetStream...etc.
            _useManifest = true;

            _resourceSets = new Dictionary<string, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();

            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            _resourceGroveler = new ManifestBasedResourceGroveler(mediator);

            Debug.Assert(MainAssembly != null);
            _neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(MainAssembly, out _fallbackLoc);
        }

        // Gets the base name for the ResourceManager.
        public virtual string BaseName => BaseNameField;

        // Whether we should ignore the capitalization of resources when calling
        // GetString or GetObject.
        public virtual bool IgnoreCase
        {
            get => _ignoreCase;
            set => _ignoreCase = value;
        }

        // Returns the Type of the ResourceSet the ResourceManager uses
        // to construct ResourceSets.
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public virtual Type ResourceSetType => _userResourceSet ?? typeof(RuntimeResourceSet);

        protected UltimateResourceFallbackLocation FallbackLocation
        {
            get => _fallbackLoc;
            set => _fallbackLoc = value;
        }

        // Tells the ResourceManager to call Close on all ResourceSets and
        // release all resources.  This will shrink your working set by
        // potentially a substantial amount in a running application.  Any
        // future resource lookups on this ResourceManager will be as
        // expensive as the very first lookup, since it will need to search
        // for files and load resources again.
        //
        // This may be useful in some complex threading scenarios, where
        // creating a new ResourceManager isn't quite the correct behavior.
        public virtual void ReleaseAllResources()
        {
            Debug.Assert(_resourceSets != null);
            Dictionary<string, ResourceSet> localResourceSets = _resourceSets;

            // If any calls to Close throw, at least leave ourselves in a
            // consistent state.
            _resourceSets = new Dictionary<string, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();

            lock (localResourceSets)
            {
                foreach ((_, ResourceSet resourceSet) in localResourceSets)
                {
                    resourceSet.Close();
                }
            }
        }

        public static ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type? usingResourceSet)
        {
            return new ResourceManager(baseName, resourceDir, usingResourceSet);
        }

        // Given a CultureInfo, GetResourceFileName generates the name for
        // the binary file for the given CultureInfo.  This method uses
        // CultureInfo's Name property as part of the file name for all cultures
        // other than the invariant culture.  This method does not touch the disk,
        // and is used only to construct what a resource file name (suitable for
        // passing to the ResourceReader constructor) or a manifest resource file
        // name should look like.
        //
        // This method can be overriden to look for a different extension,
        // such as ".ResX", or a completely different format for naming files.
        protected virtual string GetResourceFileName(CultureInfo culture)
        {
            // If this is the neutral culture, don't include the culture name.
            if (culture.HasInvariantCultureName)
            {
                return BaseNameField + ResFileExtension;
            }
            else
            {
                CultureInfo.VerifyCultureName(culture.Name, throwException: true);
                return BaseNameField + "." + culture.Name + ResFileExtension;
            }
        }

        // WARNING: This function must be kept in sync with ResourceFallbackManager.GetEnumerator()
        // Return the first ResourceSet, based on the first culture ResourceFallbackManager would return
        internal ResourceSet? GetFirstResourceSet(CultureInfo culture)
        {
            // Logic from ResourceFallbackManager.GetEnumerator()
            if (_neutralResourcesCulture != null && culture.Name == _neutralResourcesCulture.Name)
            {
                culture = CultureInfo.InvariantCulture;
            }

            if (_lastUsedResourceCache != null)
            {
                lock (_lastUsedResourceCache)
                {
                    if (culture.Name == _lastUsedResourceCache.lastCultureName)
                        return _lastUsedResourceCache.lastResourceSet;
                }
            }

            // Look in the ResourceSet table
            Dictionary<string, ResourceSet>? localResourceSets = _resourceSets;
            ResourceSet? rs = null;
            if (localResourceSets != null)
            {
                lock (localResourceSets)
                {
                    localResourceSets.TryGetValue(culture.Name, out rs);
                }
            }

            if (rs != null)
            {
                // update the cache with the most recent ResourceSet
                if (_lastUsedResourceCache != null)
                {
                    lock (_lastUsedResourceCache)
                    {
                        _lastUsedResourceCache.lastCultureName = culture.Name;
                        _lastUsedResourceCache.lastResourceSet = rs;
                    }
                }
                return rs;
            }

            return null;
        }

        // Looks up a set of resources for a particular CultureInfo.  This is
        // not useful for most users of the ResourceManager - call
        // GetString() or GetObject() instead.
        //
        // The parameters let you control whether the ResourceSet is created
        // if it hasn't yet been loaded and if parent CultureInfos should be
        // loaded as well for resource inheritance.
        //
        public virtual ResourceSet? GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            if (null == culture)
                throw new ArgumentNullException(nameof(culture));

            Dictionary<string, ResourceSet>? localResourceSets = _resourceSets;
            ResourceSet? rs;
            if (localResourceSets != null)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(culture.Name, out rs))
                        return rs;
                }
            }

            if (_useManifest && culture.HasInvariantCultureName)
            {
                string fileName = GetResourceFileName(culture);
                Debug.Assert(MainAssembly != null);
                Stream? stream = MainAssembly.GetManifestResourceStream(_locationInfo!, fileName);
                if (createIfNotExists && stream != null)
                {
                    rs = ((ManifestBasedResourceGroveler)_resourceGroveler).CreateResourceSet(stream, MainAssembly);
                    Debug.Assert(localResourceSets != null);
                    AddResourceSet(localResourceSets, culture.Name, ref rs);
                    return rs;
                }
            }

            return InternalGetResourceSet(culture, createIfNotExists, tryParents);
        }

        // InternalGetResourceSet is a non-threadsafe method where all the logic
        // for getting a resource set lives.  Access to it is controlled by
        // threadsafe methods such as GetResourceSet, GetString, & GetObject.
        // This will take a minimal number of locks.
        protected virtual ResourceSet? InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            Debug.Assert(culture != null, "culture != null");
            Debug.Assert(_resourceSets != null);

            Dictionary<string, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet? rs = null;
            CultureInfo? foundCulture = null;
            lock (localResourceSets)
            {
                if (localResourceSets.TryGetValue(culture.Name, out rs))
                {
                    return rs;
                }
            }

            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, tryParents);

            foreach (CultureInfo currentCultureInfo in mgr)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(currentCultureInfo.Name, out rs))
                    {
                        // we need to update the cache if we fellback
                        if (culture != currentCultureInfo) foundCulture = currentCultureInfo;
                        break;
                    }
                }

                // InternalGetResourceSet will never be threadsafe.  However, it must
                // be protected against reentrancy from the SAME THREAD.  (ie, calling
                // GetSatelliteAssembly may send some window messages or trigger the
                // Assembly load event, which could fail then call back into the
                // ResourceManager).  It's happened.

                rs = _resourceGroveler.GrovelForResourceSet(currentCultureInfo, localResourceSets,
                                                           tryParents, createIfNotExists);

                // found a ResourceSet; we're done
                if (rs != null)
                {
                    foundCulture = currentCultureInfo;
                    break;
                }
            }

            if (rs != null && foundCulture != null)
            {
                // add entries to the cache for the cultures we have gone through

                // currentCultureInfo now refers to the culture that had resources.
                // update cultures starting from requested culture up to the culture
                // that had resources.
                foreach (CultureInfo updateCultureInfo in mgr)
                {
                    AddResourceSet(localResourceSets, updateCultureInfo.Name, ref rs);

                    // stop when we've added current or reached invariant (top of chain)
                    if (updateCultureInfo == foundCulture)
                    {
                        break;
                    }
                }
            }

            return rs;
        }

        // Simple helper to ease maintenance and improve readability.
        private static void AddResourceSet(Dictionary<string, ResourceSet> localResourceSets, string cultureName, ref ResourceSet rs)
        {
            // InternalGetResourceSet is both recursive and reentrant -
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            lock (localResourceSets)
            {
                // If another thread added this culture, return that.
                if (localResourceSets.TryGetValue(cultureName, out ResourceSet? lostRace))
                {
                    if (!object.ReferenceEquals(lostRace, rs))
                    {
                        // Note: In certain cases, we can be trying to add a ResourceSet for multiple
                        // cultures on one thread, while a second thread added another ResourceSet for one
                        // of those cultures.  If there is a race condition we must make sure our ResourceSet
                        // isn't in our dictionary before closing it.
                        if (!localResourceSets.ContainsValue(rs))
                            rs.Dispose();
                        rs = lostRace;
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }

        protected static Version? GetSatelliteContractVersion(Assembly a)
        {
            // Ensure that the assembly reference is not null
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a), SR.ArgumentNull_Assembly);
            }

            string? v = a.GetCustomAttribute<SatelliteContractVersionAttribute>()?.Version;
            if (v == null)
            {
                // Return null. The calling code will use the assembly version instead to avoid potential type
                // and library loads caused by CA lookup.
                return null;
            }

            if (!Version.TryParse(v, out Version? version))
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidSatelliteContract_Asm_Ver, a, v));
            }

            return version;
        }

        protected static CultureInfo GetNeutralResourcesLanguage(Assembly a)
        {
            // This method should be obsolete - replace it with the one below.
            // Unfortunately, we made it protected.
            return ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(a, out _);
        }

        // IGNORES VERSION
        internal static bool IsDefaultType(string asmTypeName,
                                           string typeName)
        {
            Debug.Assert(asmTypeName != null, "asmTypeName was unexpectedly null");

            // First, compare type names
            int comma = asmTypeName.IndexOf(',');
            if (((comma == -1) ? asmTypeName.Length : comma) != typeName.Length)
                return false;

            // case sensitive
            if (string.Compare(asmTypeName, 0, typeName, 0, typeName.Length, StringComparison.Ordinal) != 0)
                return false;
            if (comma == -1)
                return true;

            // Now, compare assembly display names (IGNORES VERSION AND PROCESSORARCHITECTURE)
            // also, for  mscorlib ignores everything, since that's what the binder is going to do
            while (char.IsWhiteSpace(asmTypeName[++comma])) ;

            // case insensitive
            AssemblyName an = new AssemblyName(asmTypeName.Substring(comma));

            // to match IsMscorlib() in VM
            return string.Equals(an.Name, "mscorlib", StringComparison.OrdinalIgnoreCase);
        }

        // Looks up a resource value for a particular name.  Looks in the
        // current thread's CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        //
        public virtual string? GetString(string name)
        {
            return GetString(name, null);
        }

        // Looks up a resource value for a particular name.  Looks in the
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        //
        public virtual string? GetString(string name, CultureInfo? culture)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name));

            culture ??= CultureInfo.CurrentUICulture;

            ResourceSet? last = GetFirstResourceSet(culture);

            if (last != null)
            {
                string? value = last.GetString(name, _ignoreCase);
                if (value != null)
                    return value;
            }

            // This is the CultureInfo hierarchy traversal code for resource
            // lookups, similar but necessarily orthogonal to the ResourceSet
            // lookup logic.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, true);
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                ResourceSet? rs = InternalGetResourceSet(currentCultureInfo, true, true);
                if (rs == null)
                    break;

                if (rs != last)
                {
                    string? value = rs.GetString(name, _ignoreCase);
                    if (value != null)
                    {
                        // update last used ResourceSet
                        if (_lastUsedResourceCache != null)
                        {
                            lock (_lastUsedResourceCache)
                            {
                                _lastUsedResourceCache.lastCultureName = currentCultureInfo.Name;
                                _lastUsedResourceCache.lastResourceSet = rs;
                            }
                        }
                        return value;
                    }

                    last = rs;
                }
            }

            return null;
        }

        // Looks up a resource value for a particular name.  Looks in the
        // current thread's CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        //
        public virtual object? GetObject(string name)
        {
            return GetObject(name, null, true);
        }

        // Looks up a resource value for a particular name.  Looks in the
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        public virtual object? GetObject(string name, CultureInfo? culture)
        {
            return GetObject(name, culture, true);
        }

        private object? GetObject(string name, CultureInfo? culture, bool wrapUnmanagedMemStream)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name));

            if (null == culture)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            ResourceSet? last = GetFirstResourceSet(culture);
            if (last != null)
            {
                object? value = last.GetObject(name, _ignoreCase);

                if (value != null)
                {
                    if (value is UnmanagedMemoryStream stream && wrapUnmanagedMemStream)
                        return new UnmanagedMemoryStreamWrapper(stream);
                    else
                        return value;
                }
            }

            // This is the CultureInfo hierarchy traversal code for resource
            // lookups, similar but necessarily orthogonal to the ResourceSet
            // lookup logic.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, true);

            foreach (CultureInfo currentCultureInfo in mgr)
            {
                ResourceSet? rs = InternalGetResourceSet(currentCultureInfo, true, true);
                if (rs == null)
                    break;

                if (rs != last)
                {
                    object? value = rs.GetObject(name, _ignoreCase);
                    if (value != null)
                    {
                        // update the last used ResourceSet
                        if (_lastUsedResourceCache != null)
                        {
                            lock (_lastUsedResourceCache)
                            {
                                _lastUsedResourceCache.lastCultureName = currentCultureInfo.Name;
                                _lastUsedResourceCache.lastResourceSet = rs;
                            }
                        }

                        if (value is UnmanagedMemoryStream stream && wrapUnmanagedMemStream)
                            return new UnmanagedMemoryStreamWrapper(stream);
                        else
                            return value;
                    }

                    last = rs;
                }
            }

            return null;
        }

        public UnmanagedMemoryStream? GetStream(string name)
        {
            return GetStream(name, null);
        }

        public UnmanagedMemoryStream? GetStream(string name, CultureInfo? culture)
        {
            object? obj = GetObject(name, culture, false);
            UnmanagedMemoryStream? ums = obj as UnmanagedMemoryStream;
            if (ums == null && obj != null)
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotStream_Name, name));
            return ums;
        }

        internal class ResourceManagerMediator
        {
            private readonly ResourceManager _rm;

            internal ResourceManagerMediator(ResourceManager rm)
            {
                if (rm == null)
                {
                    throw new ArgumentNullException(nameof(rm));
                }
                _rm = rm;
            }

            // NEEDED ONLY BY FILE-BASED
            internal string? ModuleDir => _rm._moduleDir;

            // NEEDED BOTH BY FILE-BASED  AND ASSEMBLY-BASED
            internal Type? LocationInfo => _rm._locationInfo;

            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            internal Type? UserResourceSet => _rm._userResourceSet;

            internal string? BaseNameField => _rm.BaseNameField;

            internal CultureInfo? NeutralResourcesCulture
            {
                get => _rm._neutralResourcesCulture;
                set => _rm._neutralResourcesCulture = value;
            }

            internal string GetResourceFileName(CultureInfo culture) =>
                _rm.GetResourceFileName(culture);

            // NEEDED ONLY BY ASSEMBLY-BASED
            internal bool LookedForSatelliteContractVersion
            {
                get => _rm._lookedForSatelliteContractVersion;
                set => _rm._lookedForSatelliteContractVersion = value;
            }

            internal Version? SatelliteContractVersion
            {
                get => _rm._satelliteContractVersion;
                set => _rm._satelliteContractVersion = value;
            }

            internal static Version? ObtainSatelliteContractVersion(Assembly a) =>
                ResourceManager.GetSatelliteContractVersion(a);

            internal UltimateResourceFallbackLocation FallbackLoc
            {
                get => _rm.FallbackLocation;
                set => _rm._fallbackLoc = value;
            }

            internal Assembly? MainAssembly => _rm.MainAssembly;

            // this is weird because we have BaseNameField accessor above, but we're sticking
            // with it for compat.
            internal string BaseName => _rm.BaseName;
        }
    }
}
