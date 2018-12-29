// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Default way to access String and Object resources
** from an assembly.
**
** 
===========================================================*/

namespace System.Resources
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Collections;
    using System.Text;
    using System.Reflection;
    using System.Security;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using Microsoft.Win32;
    using System.Collections.Generic;
    using System.Runtime.Versioning;
    using System.Diagnostics;
#if FEATURE_APPX
    using Internal.Resources;
#endif
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

    public class ResourceManager
    {
        internal class CultureNameResourceSetPair
        {
            public string lastCultureName;
            public ResourceSet lastResourceSet;
        }

        protected string BaseNameField;
        // Sets is a many-to-one table of CultureInfos mapped to ResourceSets.
        // Don't synchronize ResourceSets - too fine-grained a lock to be effective
        [Obsolete("call InternalGetResourceSet instead")]
        internal Hashtable ResourceSets;


        private Dictionary<string, ResourceSet> _resourceSets;
        private string moduleDir;      // For assembly-ignorant directory location
        protected Assembly MainAssembly;   // Need the assembly manifest sometimes.
        private Type _locationInfo;    // For Assembly or type-based directory layout
        private Type _userResourceSet;  // Which ResourceSet instance to create
        private CultureInfo _neutralResourcesCulture;  // For perf optimizations.

        private CultureNameResourceSetPair _lastUsedResourceCache;

        private bool _ignoreCase;   // Whether case matters in GetString & GetObject

        private bool UseManifest;  // Use Assembly manifest, or grovel disk.

        // Whether to fall back to the main assembly or a particular 
        // satellite for the neutral resources.
        private UltimateResourceFallbackLocation _fallbackLoc;
        // Version number of satellite assemblies to look for.  May be null.
        private Version _satelliteContractVersion;
        private bool _lookedForSatelliteContractVersion;

        private RuntimeAssembly _callingAssembly;  // Assembly who created the ResMgr.

        private IResourceGroveler resourceGroveler;

        public static readonly int MagicNumber = unchecked((int)0xBEEFCACE);  // If only hex had a K...

        // Version number so ResMgr can get the ideal set of classes for you.
        // ResMgr header is:
        // 1) MagicNumber (little endian Int32)
        // 2) HeaderVersionNumber (little endian Int32)
        // 3) Num Bytes to skip past ResMgr header (little endian Int32)
        // 4) IResourceReader type name for this file (bytelength-prefixed UTF-8 String)
        // 5) ResourceSet type name for this file (bytelength-prefixed UTF8 String)
        public static readonly int HeaderVersionNumber = 1;

        //
        //It would be better if we could use _neutralCulture instead of calling
        //CultureInfo.InvariantCulture everywhere, but we run into problems with the .cctor.  CultureInfo 
        //initializes assembly, which initializes ResourceManager, which tries to get a CultureInfo which isn't
        //there yet because CultureInfo's class initializer hasn't finished.  If we move SystemResMgr off of
        //Assembly (or at least make it an internal property) we should be able to circumvent this problem.
        //
        //      private static CultureInfo _neutralCulture = null;

        // This is our min required ResourceSet type.
        private static readonly Type s_minResourceSet = typeof(ResourceSet);
        // These Strings are used to avoid using Reflection in CreateResourceSet.
        internal const string ResReaderTypeName = "System.Resources.ResourceReader";
        internal const string ResSetTypeName = "System.Resources.RuntimeResourceSet";
        internal const string ResFileExtension = ".resources";
        internal const int ResFileExtensionLength = 10;

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        private void Init()
        {
            _callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
        }

        protected ResourceManager()
        {
            Init();

            _lastUsedResourceCache = new CultureNameResourceSetPair();
            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            resourceGroveler = new ManifestBasedResourceGroveler(mediator);
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
        private ResourceManager(string baseName, string resourceDir, Type usingResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));
            if (null == resourceDir)
                throw new ArgumentNullException(nameof(resourceDir));

            BaseNameField = baseName;

            moduleDir = resourceDir;
            _userResourceSet = usingResourceSet;
#pragma warning disable 618
            ResourceSets = new Hashtable(); // for backward compatibility
#pragma warning restore 618
            _resourceSets = new Dictionary<string, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            UseManifest = false;

            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            resourceGroveler = new FileBasedResourceGroveler(mediator);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public ResourceManager(string baseName, Assembly assembly)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));

            if (null == assembly)
                throw new ArgumentNullException(nameof(assembly));

            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            MainAssembly = assembly;
            BaseNameField = baseName;

            SetAppXConfiguration();

            CommonAssemblyInit();

            _callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            // Special case for mscorlib - protect mscorlib's private resources.
            // This isn't for security reasons, but to ensure we can make
            // breaking changes to mscorlib's internal resources without 
            // assuming users may have taken a dependency on them.
            if (assembly == typeof(object).Assembly && _callingAssembly != assembly)
            {
                _callingAssembly = null;
            }
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public ResourceManager(string baseName, Assembly assembly, Type usingResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException(nameof(baseName));
            if (null == assembly)
                throw new ArgumentNullException(nameof(assembly));

            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            MainAssembly = assembly;
            BaseNameField = baseName;

            if (usingResourceSet != null && (usingResourceSet != s_minResourceSet) && !(usingResourceSet.IsSubclassOf(s_minResourceSet)))
                throw new ArgumentException(SR.Arg_ResMgrNotResSet, nameof(usingResourceSet));
            _userResourceSet = usingResourceSet;

            CommonAssemblyInit();
            _callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            // Special case for mscorlib - protect mscorlib's private resources.
            // This isn't for security reasons, but to ensure we can make
            // breaking changes to mscorlib's internal resources without 
            // assuming users may have taken a dependency on them.
            if (assembly == typeof(object).Assembly && _callingAssembly != assembly)
                _callingAssembly = null;
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
                throw new ArgumentNullException(nameof(resourceSource));

            if (!(resourceSource is RuntimeType))
                throw new ArgumentException(SR.Argument_MustBeRuntimeType);

            _locationInfo = resourceSource;
            MainAssembly = _locationInfo.Assembly;
            BaseNameField = resourceSource.Name;

            SetAppXConfiguration();

            CommonAssemblyInit();

            _callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            // Special case for mscorlib - protect mscorlib's private resources.
            if (MainAssembly == typeof(object).Assembly && _callingAssembly != MainAssembly)
            {
                _callingAssembly = null;
            }
        }

        // Trying to unify code as much as possible, even though having to do a
        // security check in each constructor prevents it.
        private void CommonAssemblyInit()
        {
            // Now we can use the managed resources even when using PRI's to support the APIs GetObject, GetStream...etc.
            UseManifest = true;

            _resourceSets = new Dictionary<string, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();

            _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;

            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            resourceGroveler = new ManifestBasedResourceGroveler(mediator);

            _neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(MainAssembly, ref _fallbackLoc);
        }

        // Gets the base name for the ResourceManager.
        public virtual string BaseName
        {
            get { return BaseNameField; }
        }

        // Whether we should ignore the capitalization of resources when calling
        // GetString or GetObject.
        public virtual bool IgnoreCase
        {
            get { return _ignoreCase; }
            set { _ignoreCase = value; }
        }

        // Returns the Type of the ResourceSet the ResourceManager uses
        // to construct ResourceSets.
        public virtual Type ResourceSetType
        {
            get { return (_userResourceSet == null) ? typeof(RuntimeResourceSet) : _userResourceSet; }
        }

        protected UltimateResourceFallbackLocation FallbackLocation
        {
            get { return _fallbackLoc; }
            set { _fallbackLoc = value; }
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

        public static ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir, Type usingResourceSet)
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
        internal ResourceSet GetFirstResourceSet(CultureInfo culture)
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
            Dictionary<string, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs = null;
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
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public virtual ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            if (null == culture)
                throw new ArgumentNullException(nameof(culture));

            Dictionary<string, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs;
            if (localResourceSets != null)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(culture.Name, out rs))
                        return rs;
                }
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            if (UseManifest && culture.HasInvariantCultureName)
            {
                string fileName = GetResourceFileName(culture);
                RuntimeAssembly mainAssembly = (RuntimeAssembly)MainAssembly;
                Stream stream = mainAssembly.GetManifestResourceStream(_locationInfo, fileName, _callingAssembly == MainAssembly, ref stackMark);
                if (createIfNotExists && stream != null)
                {
                    rs = ((ManifestBasedResourceGroveler)resourceGroveler).CreateResourceSet(stream, MainAssembly);
                    AddResourceSet(localResourceSets, culture.Name, ref rs);
                    return rs;
                }
            }

            // Note: ideally we could plumb through the stack crawl mark here, but we must
            // call the virtual 3-argument InternalGetResourceSet method for compatibility.
            // Security-wise, we're not overly interested in protecting access to resources,
            // since full-trust callers can get them already and most resources are public.
            // Also, the JIT inliner could always inline a caller into another assembly's
            // method.
            // So if we happen to return some resources in cases where we should really be
            // doing a demand for member access permissions, we're not overly concerned.
            return InternalGetResourceSet(culture, createIfNotExists, tryParents);
        }

        // InternalGetResourceSet is a non-threadsafe method where all the logic
        // for getting a resource set lives.  Access to it is controlled by
        // threadsafe methods such as GetResourceSet, GetString, & GetObject.  
        // This will take a minimal number of locks.
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        protected virtual ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            Debug.Assert(culture != null, "culture != null");

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetResourceSet(culture, createIfNotExists, tryParents, ref stackMark);
        }

        // InternalGetResourceSet is a non-threadsafe method where all the logic
        // for getting a resource set lives.  Access to it is controlled by
        // threadsafe methods such as GetResourceSet, GetString, & GetObject.  
        // This will take a minimal number of locks.
        private ResourceSet InternalGetResourceSet(CultureInfo requestedCulture, bool createIfNotExists, bool tryParents, ref StackCrawlMark stackMark)
        {
            Dictionary<string, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs = null;
            CultureInfo foundCulture = null;
            lock (localResourceSets)
            {
                if (localResourceSets.TryGetValue(requestedCulture.Name, out rs))
                {
                    return rs;
                }
            }

            ResourceFallbackManager mgr = new ResourceFallbackManager(requestedCulture, _neutralResourcesCulture, tryParents);

            foreach (CultureInfo currentCultureInfo in mgr)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(currentCultureInfo.Name, out rs))
                    {
                        // we need to update the cache if we fellback
                        if (requestedCulture != currentCultureInfo) foundCulture = currentCultureInfo;
                        break;
                    }
                }

                // InternalGetResourceSet will never be threadsafe.  However, it must
                // be protected against reentrancy from the SAME THREAD.  (ie, calling
                // GetSatelliteAssembly may send some window messages or trigger the
                // Assembly load event, which could fail then call back into the 
                // ResourceManager).  It's happened.

                rs = resourceGroveler.GrovelForResourceSet(currentCultureInfo, localResourceSets,
                                                           tryParents, createIfNotExists, ref stackMark);

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
                ResourceSet lostRace;
                if (localResourceSets.TryGetValue(cultureName, out lostRace))
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

        protected static Version GetSatelliteContractVersion(Assembly a)
        {
            // Ensure that the assembly reference is not null
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a), SR.ArgumentNull_Assembly);
            }

            // Return null. The calling code will use the assembly version instead to avoid potential type
            // and library loads caused by CA lookup. NetCF uses the assembly version always.
            return null;
        }

        protected static CultureInfo GetNeutralResourcesLanguage(Assembly a)
        {
            // This method should be obsolete - replace it with the one below.
            // Unfortunately, we made it protected.
            UltimateResourceFallbackLocation ignoringUsefulData = UltimateResourceFallbackLocation.MainAssembly;
            CultureInfo culture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(a, ref ignoringUsefulData);
            return culture;
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

#if FEATURE_APPX
        private string GetStringFromPRI(string stringName, string startingCulture, string neutralResourcesCulture)
        {
            Debug.Assert(_bUsingModernResourceManagement);
            Debug.Assert(_WinRTResourceManager != null);
            Debug.Assert(_PRIonAppXInitialized);
            Debug.Assert(ApplicationModel.IsUap);

            if (stringName.Length == 0)
                return null;

            string resourceString = null;

            // Do not handle exceptions. See the comment in SetAppXConfiguration about throwing
            // exception types that the ResourceManager class is not documented to throw.
            resourceString = _WinRTResourceManager.GetString(
                                       stringName,
                                       string.IsNullOrEmpty(startingCulture) ? null : startingCulture,
                                       string.IsNullOrEmpty(neutralResourcesCulture) ? null : neutralResourcesCulture);

            return resourceString;
        }

        // Since we can't directly reference System.Runtime.WindowsRuntime from mscorlib, we have to get the type via reflection.
        // It would be better if we could just implement WindowsRuntimeResourceManager in mscorlib, but we can't, because
        // we can do very little with WinRT in mscorlib.
        internal static WindowsRuntimeResourceManagerBase GetWinRTResourceManager()
        {
            Type WinRTResourceManagerType = Type.GetType("System.Resources.WindowsRuntimeResourceManager, System.Runtime.WindowsRuntime", throwOnError: true);
            return (WindowsRuntimeResourceManagerBase)Activator.CreateInstance(WinRTResourceManagerType, true);
        }
#endif

        private bool _bUsingModernResourceManagement; // Written only by SetAppXConfiguration

#if FEATURE_APPX
        private WindowsRuntimeResourceManagerBase _WinRTResourceManager; // Written only by SetAppXConfiguration

        private bool _PRIonAppXInitialized; // Written only by SetAppXConfiguration

        private PRIExceptionInfo _PRIExceptionInfo; // Written only by SetAppXConfiguration

        // When running under AppX, the following rules apply for resource lookup:
        //
        // 1) For Framework assemblies, we always use satellite assembly based lookup.
        // 2) For non-FX assemblies:
        //    
        //    a) If the assembly lives under PLATFORM_RESOURCE_ROOTS (as specified by the host during AppDomain creation),
        //       then we will use satellite assembly based lookup in assemblies like *.resources.dll.
        //   
        //    b) For any other non-FX assembly, we will use the modern resource manager with the premise that app package
        //       contains the PRI resources.
        private bool ShouldUseSatelliteAssemblyResourceLookupUnderAppX(RuntimeAssembly resourcesAssembly)
        {
            bool fUseSatelliteAssemblyResourceLookupUnderAppX = typeof(object).Assembly == resourcesAssembly;

            if (!fUseSatelliteAssemblyResourceLookupUnderAppX)
            {
                // Check to see if the assembly is under PLATFORM_RESOURCE_ROOTS. If it is, then we should use satellite assembly lookup for it.
                string platformResourceRoots = (string)(AppContext.GetData("PLATFORM_RESOURCE_ROOTS"));
                if ((platformResourceRoots != null) && (platformResourceRoots != string.Empty))
                {
                    string resourceAssemblyPath = resourcesAssembly.Location;

                    // Loop through the PLATFORM_RESOURCE_ROOTS and see if the assembly is contained in it.
                    foreach (string pathPlatformResourceRoot in platformResourceRoots.Split(Path.PathSeparator))
                    {
                        if (resourceAssemblyPath.StartsWith(pathPlatformResourceRoot, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Found the resource assembly to be present in one of the PLATFORM_RESOURCE_ROOT, so stop the enumeration loop.
                            fUseSatelliteAssemblyResourceLookupUnderAppX = true;
                            break;
                        }
                    }
                }
            }

            return fUseSatelliteAssemblyResourceLookupUnderAppX;
        }
#endif // FEATURE_APPX

        // Only call SetAppXConfiguration from ResourceManager constructors, and nowhere else.
        // Throws MissingManifestResourceException and WinRT HResults

        private void SetAppXConfiguration()
        {
            Debug.Assert(_bUsingModernResourceManagement == false); // Only this function writes to this member
#if FEATURE_APPX
            Debug.Assert(_WinRTResourceManager == null); // Only this function writes to this member
            Debug.Assert(_PRIonAppXInitialized == false); // Only this function writes to this member
            Debug.Assert(_PRIExceptionInfo == null); // Only this function writes to this member

            bool bUsingSatelliteAssembliesUnderAppX = false;

            RuntimeAssembly resourcesAssembly = (RuntimeAssembly)MainAssembly;

            if (resourcesAssembly == null)
                resourcesAssembly = _callingAssembly;

            if (resourcesAssembly != null)
            {
                if (resourcesAssembly != typeof(object).Assembly) // We are not loading resources for mscorlib
                {
                    if (ApplicationModel.IsUap)
                    {
                        // If we have the type information from the ResourceManager(Type) constructor, we use it. Otherwise, we use BaseNameField.
                        string reswFilename = _locationInfo == null ? BaseNameField : _locationInfo.FullName;

                        // The only way this can happen is if a class inherited from ResourceManager and
                        // did not set the BaseNameField before calling the protected ResourceManager() constructor.
                        // For other constructors, we would already have thrown an ArgumentNullException by now.
                        // Throwing an ArgumentNullException now is not the right thing to do because technically
                        // ResourceManager() takes no arguments, and because it is not documented as throwing
                        // any exceptions. Instead, let's go through the rest of the initialization with this set to
                        // an empty string. We may in fact fail earlier for another reason, but otherwise we will
                        // throw a MissingManifestResourceException when GetString is called indicating that a
                        // resW filename called "" could not be found.
                        if (reswFilename == null)
                            reswFilename = string.Empty;

                        if (!bUsingSatelliteAssembliesUnderAppX)
                        {
                            _bUsingModernResourceManagement = !ShouldUseSatelliteAssemblyResourceLookupUnderAppX(resourcesAssembly);

                            if (_bUsingModernResourceManagement)
                            {
                                // Only now are we certain that we need the PRI file.

                                // At this point it is important NOT to set _bUsingModernResourceManagement to false
                                // if the PRI file does not exist because we are now certain we need to load PRI
                                // resources. We want to fail by throwing a MissingManifestResourceException
                                // if WindowsRuntimeResourceManager.Initialize fails to locate the PRI file. We do not
                                // want to fall back to using satellite assemblies anymore. Note that we would not throw
                                // the MissingManifestResourceException from this function, but from GetString. See the
                                // comment below on the reason for this.

                                _WinRTResourceManager = GetWinRTResourceManager();

                                try
                                {
                                    _PRIonAppXInitialized = _WinRTResourceManager.Initialize(resourcesAssembly.Location, reswFilename, out _PRIExceptionInfo);
                                    // Note that _PRIExceptionInfo might be null - this is OK.
                                    // In that case we will just throw the generic
                                    // MissingManifestResource_NoPRIresources exception.
                                    // See the implementation of GetString for more details.
                                }
                                // We would like to be able to throw a MissingManifestResourceException here if PRI resources
                                // could not be loaded for a recognized reason. However, the ResourceManager constructors
                                // that call SetAppXConfiguration are not documented as throwing MissingManifestResourceException,
                                // and since they are part of the portable profile, we cannot start throwing a new exception type
                                // as that would break existing portable libraries. Hence we must save the exception information
                                // now and throw the exception on the first call to GetString.
                                catch (FileNotFoundException)
                                {
                                    // We will throw MissingManifestResource_NoPRIresources from GetString
                                    // when we see that _PRIonAppXInitialized is false.
                                }
                                catch (Exception e)
                                {
                                    // ERROR_MRM_MAP_NOT_FOUND can be thrown by the call to ResourceManager.get_AllResourceMaps
                                    // in WindowsRuntimeResourceManager.Initialize.
                                    // In this case _PRIExceptionInfo is now null and we will just throw the generic
                                    // MissingManifestResource_NoPRIresources exception.
                                    // See the implementation of GetString for more details.
                                    if (e.HResult != HResults.ERROR_MRM_MAP_NOT_FOUND)
                                        throw; // Unexpected exception code. Bubble it up to the caller.
                                }

                                if (!_PRIonAppXInitialized)
                                {
                                    _bUsingModernResourceManagement = false;
                                }
                                // Allow all other exception types to bubble up to the caller.

                                // Yes, this causes us to potentially throw exception types that are not documented.

                                // Ultimately the tradeoff is the following:
                                // -We could ignore unknown exceptions or rethrow them as inner exceptions
                                // of exceptions that the ResourceManager class is already documented as throwing.
                                // This would allow existing portable libraries to gracefully recover if they don't care
                                // too much about the ResourceManager object they are using. However it could
                                // mask potentially fatal errors that we are not aware of, such as a disk drive failing.


                                // The alternative, which we chose, is to throw unknown exceptions. This may tear
                                // down the process if the portable library and app don't expect this exception type.
                                // On the other hand, this won't mask potentially fatal errors we don't know about.
                            }
                        }
                    }
                }
            }
            // resourcesAssembly == null should not happen but it can. See the comment on Assembly.GetCallingAssembly.
            // However for the sake of 100% backwards compatibility on Win7 and below, we must leave
            // _bUsingModernResourceManagement as false.
#endif // FEATURE_APPX            
        }

        // Looks up a resource value for a particular name.  Looks in the 
        // current thread's CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        // 
        public virtual string GetString(string name)
        {
            return GetString(name, (CultureInfo)null);
        }

        // Looks up a resource value for a particular name.  Looks in the 
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        // 
        public virtual string GetString(string name, CultureInfo culture)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name));

#if FEATURE_APPX
            if (_bUsingModernResourceManagement)
            {
                // If the caller explicitly passed in a culture that was obtained by calling CultureInfo.CurrentUICulture,
                // null it out, so that we re-compute it.  If we use modern resource lookup, we may end up getting a "better"
                // match, since CultureInfo objects can't represent all the different languages the AppX resource model supports.
                if (object.ReferenceEquals(culture, CultureInfo.CurrentUICulture))
                {
                    culture = null;
                }

                if (_PRIonAppXInitialized == false)
                {
                    // Always throw if we did not fully succeed in initializing the WinRT Resource Manager.

                    if (_PRIExceptionInfo != null && _PRIExceptionInfo.PackageSimpleName != null && _PRIExceptionInfo.ResWFile != null)
                        throw new MissingManifestResourceException(SR.Format(SR.MissingManifestResource_ResWFileNotLoaded, _PRIExceptionInfo.ResWFile, _PRIExceptionInfo.PackageSimpleName));

                    throw new MissingManifestResourceException(SR.MissingManifestResource_NoPRIresources);
                }

                // Throws WinRT hresults.
                return GetStringFromPRI(name,
                                        culture == null ? null : culture.Name,
                                        _neutralResourcesCulture.Name);
            }
            else
#endif // FEATURE_APPX
            {
                if (culture == null)
                {
                    culture = CultureInfo.CurrentUICulture;
                }

                ResourceSet last = GetFirstResourceSet(culture);

                if (last != null)
                {
                    string value = last.GetString(name, _ignoreCase);
                    if (value != null)
                        return value;
                }


                // This is the CultureInfo hierarchy traversal code for resource 
                // lookups, similar but necessarily orthogonal to the ResourceSet 
                // lookup logic.
                ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, true);
                foreach (CultureInfo currentCultureInfo in mgr)
                {
                    ResourceSet rs = InternalGetResourceSet(currentCultureInfo, true, true);
                    if (rs == null)
                        break;

                    if (rs != last)
                    {
                        string value = rs.GetString(name, _ignoreCase);
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
            }

            return null;
        }


        // Looks up a resource value for a particular name.  Looks in the 
        // current thread's CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        // 
        public virtual object GetObject(string name)
        {
            return GetObject(name, (CultureInfo)null, true);
        }

        // Looks up a resource value for a particular name.  Looks in the 
        // specified CultureInfo, and if not found, all parent CultureInfos.
        // Returns null if the resource wasn't found.
        public virtual object GetObject(string name, CultureInfo culture)
        {
            return GetObject(name, culture, true);
        }

        private object GetObject(string name, CultureInfo culture, bool wrapUnmanagedMemStream)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name));

            if (null == culture)
            {
                culture = CultureInfo.CurrentUICulture;
            }

            ResourceSet last = GetFirstResourceSet(culture);
            if (last != null)
            {
                object value = last.GetObject(name, _ignoreCase);

                if (value != null)
                {
                    UnmanagedMemoryStream stream = value as UnmanagedMemoryStream;
                    if (stream != null && wrapUnmanagedMemStream)
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
                // Note: Technically this method should be passed in a stack crawl mark that we then pass
                // to InternalGetResourceSet for ensuring we demand permissions to read your private resources
                // if you're reading resources from an assembly other than yourself.  But, we must call our
                // three argument overload (without the stack crawl mark) for compatibility.  After 
                // consideration, we aren't worried about the security impact.
                ResourceSet rs = InternalGetResourceSet(currentCultureInfo, true, true);
                if (rs == null)
                    break;

                if (rs != last)
                {
                    object value = rs.GetObject(name, _ignoreCase);
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

                        UnmanagedMemoryStream stream = value as UnmanagedMemoryStream;
                        if (stream != null && wrapUnmanagedMemStream)
                            return new UnmanagedMemoryStreamWrapper(stream);
                        else
                            return value;
                    }

                    last = rs;
                }
            }

            return null;
        }

        public UnmanagedMemoryStream GetStream(string name)
        {
            return GetStream(name, (CultureInfo)null);
        }

        public UnmanagedMemoryStream GetStream(string name, CultureInfo culture)
        {
            object obj = GetObject(name, culture, false);
            UnmanagedMemoryStream ums = obj as UnmanagedMemoryStream;
            if (ums == null && obj != null)
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotStream_Name, name));
            return ums;
        }

        internal class ResourceManagerMediator
        {
            private ResourceManager _rm;

            internal ResourceManagerMediator(ResourceManager rm)
            {
                if (rm == null)
                {
                    throw new ArgumentNullException(nameof(rm));
                }
                _rm = rm;
            }

            // NEEDED ONLY BY FILE-BASED
            internal string ModuleDir
            {
                get { return _rm.moduleDir; }
            }

            // NEEDED BOTH BY FILE-BASED  AND ASSEMBLY-BASED
            internal Type LocationInfo
            {
                get { return _rm._locationInfo; }
            }

            internal Type UserResourceSet
            {
                get { return _rm._userResourceSet; }
            }

            internal string BaseNameField
            {
                get { return _rm.BaseNameField; }
            }

            internal CultureInfo NeutralResourcesCulture
            {
                get { return _rm._neutralResourcesCulture; }
                set { _rm._neutralResourcesCulture = value; }
            }

            internal string GetResourceFileName(CultureInfo culture)
            {
                return _rm.GetResourceFileName(culture);
            }

            // NEEDED ONLY BY ASSEMBLY-BASED
            internal bool LookedForSatelliteContractVersion
            {
                get { return _rm._lookedForSatelliteContractVersion; }
                set { _rm._lookedForSatelliteContractVersion = value; }
            }

            internal Version SatelliteContractVersion
            {
                get { return _rm._satelliteContractVersion; }
                set { _rm._satelliteContractVersion = value; }
            }

            internal Version ObtainSatelliteContractVersion(Assembly a)
            {
                return ResourceManager.GetSatelliteContractVersion(a);
            }

            internal UltimateResourceFallbackLocation FallbackLoc
            {
                get { return _rm.FallbackLocation; }
                set { _rm._fallbackLoc = value; }
            }

            internal RuntimeAssembly CallingAssembly
            {
                get { return _rm._callingAssembly; }
            }

            internal RuntimeAssembly MainAssembly
            {
                get { return (RuntimeAssembly)_rm.MainAssembly; }
            }

            // this is weird because we have BaseNameField accessor above, but we're sticking
            // with it for compat.
            internal string BaseName
            {
                get { return _rm.BaseName; }
            }
        }
    }
}
