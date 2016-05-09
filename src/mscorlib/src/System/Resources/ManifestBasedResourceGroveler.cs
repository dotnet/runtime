// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Searches for resources in Assembly manifest, used
** for assembly-based resource lookup.
**
** 
===========================================================*/
namespace System.Resources {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;
    using Microsoft.Win32;

#if !FEATURE_CORECLR
    using System.Diagnostics.Tracing;
#endif

    //
    // Note: this type is integral to the construction of exception objects,
    // and sometimes this has to be done in low memory situtations (OOM) or
    // to create TypeInitializationExceptions due to failure of a static class
    // constructor. This type needs to be extremely careful and assume that 
    // any type it references may have previously failed to construct, so statics
    // belonging to that type may not be initialized. FrameworkEventSource.Log
    // is one such example.
    //
    internal class ManifestBasedResourceGroveler : IResourceGroveler
    {

        private ResourceManager.ResourceManagerMediator _mediator;

        public ManifestBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            // here and below: convert asserts to preconditions where appropriate when we get
            // contracts story in place.
            Contract.Requires(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable
        public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark)
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");
            Contract.Assert(localResourceSets != null, "localResourceSets shouldn't be null; check caller");

            ResourceSet rs = null;
            Stream stream = null;
            RuntimeAssembly satellite = null;

            // 1. Fixups for ultimate fallbacks
            CultureInfo lookForCulture = UltimateFallbackFixup(culture);

            // 2. Look for satellite assembly or main assembly, as appropriate
            if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
                // don't bother looking in satellites in this case
                satellite = _mediator.MainAssembly;
            }
#if RESOURCE_SATELLITE_CONFIG
            // If our config file says the satellite isn't here, don't ask for it.
            else if (!lookForCulture.HasInvariantCultureName && !_mediator.TryLookingForSatellite(lookForCulture))
            {
                satellite = null;
            }
#endif
            else
            {
                satellite = GetSatelliteAssembly(lookForCulture, ref stackMark);

                if (satellite == null)
                {
                    bool raiseException = (culture.HasInvariantCultureName && (_mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite));
                    // didn't find satellite, give error if necessary
                    if (raiseException)
                    {
                        HandleSatelliteMissing();
                    }
                }
            }

            // get resource file name we'll search for. Note, be careful if you're moving this statement
            // around because lookForCulture may be modified from originally requested culture above.
            String fileName = _mediator.GetResourceFileName(lookForCulture);

            // 3. If we identified an assembly to search; look in manifest resource stream for resource file
            if (satellite != null)
            {
                // Handle case in here where someone added a callback for assembly load events.
                // While no other threads have called into GetResourceSet, our own thread can!
                // At that point, we could already have an RS in our hash table, and we don't 
                // want to add it twice.
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(culture.Name, out rs))
                    {
#if !FEATURE_CORECLR
                        if (FrameworkEventSource.IsInitialized)
                        {
                            FrameworkEventSource.Log.ResourceManagerFoundResourceSetInCacheUnexpected(_mediator.BaseName, _mediator.MainAssembly, culture.Name);
                        }
#endif
                    }
                }

                stream = GetManifestResourceStream(satellite, fileName, ref stackMark);
            }

#if !FEATURE_CORECLR
            if (FrameworkEventSource.IsInitialized)
            {
                if (stream != null)
                {
                    FrameworkEventSource.Log.ResourceManagerStreamFound(_mediator.BaseName, _mediator.MainAssembly, culture.Name, satellite, fileName);
                }
                else
                {
                    FrameworkEventSource.Log.ResourceManagerStreamNotFound(_mediator.BaseName, _mediator.MainAssembly, culture.Name, satellite, fileName);
                }
            }
#endif

            // 4a. Found a stream; create a ResourceSet if possible
            if (createIfNotExists && stream != null && rs == null)
            {
#if !FEATURE_CORECLR
                if (FrameworkEventSource.IsInitialized)
                {
                    FrameworkEventSource.Log.ResourceManagerCreatingResourceSet(_mediator.BaseName, _mediator.MainAssembly, culture.Name, fileName);
                }
#endif
                rs = CreateResourceSet(stream, satellite);
            }
            else if (stream == null && tryParents)
            {
                // 4b. Didn't find stream; give error if necessary
                bool raiseException = culture.HasInvariantCultureName;
                if (raiseException)
                {
                    HandleResourceStreamMissing(fileName);
                }
            }

#if !FEATURE_CORECLR
            if (!createIfNotExists && stream != null && rs == null) 
            {
                if (FrameworkEventSource.IsInitialized)
                {
                    FrameworkEventSource.Log.ResourceManagerNotCreatingResourceSet(_mediator.BaseName, _mediator.MainAssembly, culture.Name);
                }
            }
#endif

            return rs;
        }

#if !FEATURE_CORECLR
        // Returns whether or not the main assembly contains a particular resource
        // file in it's assembly manifest.  Used to verify that the neutral 
        // Culture's .resources file is present in the main assembly
        public bool HasNeutralResources(CultureInfo culture, String defaultResName)
        {
            String resName = defaultResName;
            if (_mediator.LocationInfo != null && _mediator.LocationInfo.Namespace != null)
                resName = _mediator.LocationInfo.Namespace + Type.Delimiter + defaultResName;
            String[] resourceFiles = _mediator.MainAssembly.GetManifestResourceNames();
            foreach(String s in resourceFiles)
                if (s.Equals(resName))
                    return true;
            return false;
        }
#endif

        private CultureInfo UltimateFallbackFixup(CultureInfo lookForCulture)
        {

            CultureInfo returnCulture = lookForCulture;

            // If our neutral resources were written in this culture AND we know the main assembly
            // does NOT contain neutral resources, don't probe for this satellite.
            if (lookForCulture.Name == _mediator.NeutralResourcesCulture.Name &&
                _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
#if !FEATURE_CORECLR
                if (FrameworkEventSource.IsInitialized)
                {
                    FrameworkEventSource.Log.ResourceManagerNeutralResourcesSufficient(_mediator.BaseName, _mediator.MainAssembly, lookForCulture.Name);
                }
#endif

                returnCulture = CultureInfo.InvariantCulture;
            }
            else if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite)
            {
                returnCulture = _mediator.NeutralResourcesCulture;
            }

            return returnCulture;

        }

        [System.Security.SecurityCritical]
        internal static CultureInfo GetNeutralResourcesLanguage(Assembly a, ref UltimateResourceFallbackLocation fallbackLocation)
        {
            Contract.Assert(a != null, "assembly != null");
            string cultureName = null;
            short fallback = 0;
            if (GetNeutralResourcesLanguageAttribute(((RuntimeAssembly)a).GetNativeHandle(), 
                                                        JitHelpers.GetStringHandleOnStack(ref cultureName), 
                                                        out fallback)) {
                if ((UltimateResourceFallbackLocation)fallback < UltimateResourceFallbackLocation.MainAssembly || (UltimateResourceFallbackLocation)fallback > UltimateResourceFallbackLocation.Satellite) {
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", fallback));
                }
                fallbackLocation = (UltimateResourceFallbackLocation)fallback;
            }
            else {
#if !FEATURE_CORECLR
                if (FrameworkEventSource.IsInitialized) {
                    FrameworkEventSource.Log.ResourceManagerNeutralResourceAttributeMissing(a);
                }
#endif
                fallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
                return CultureInfo.InvariantCulture;
            }

            try
            {
                CultureInfo c = CultureInfo.GetCultureInfo(cultureName);
                return c;
            }
            catch (ArgumentException e)
            { // we should catch ArgumentException only.
                // Note we could go into infinite loops if mscorlib's 
                // NeutralResourcesLanguageAttribute is mangled.  If this assert
                // fires, please fix the build process for the BCL directory.
                if (a == typeof(Object).Assembly)
                {
                    Contract.Assert(false, System.CoreLib.Name+"'s NeutralResourcesLanguageAttribute is a malformed culture name! name: \"" + cultureName + "\"  Exception: " + e);
                    return CultureInfo.InvariantCulture;
                }

                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_Asm_Culture", a.ToString(), cultureName), e);
            }
        }

        // Constructs a new ResourceSet for a given file name.  The logic in
        // here avoids a ReflectionPermission check for our RuntimeResourceSet
        // for perf and working set reasons.
        // Use the assembly to resolve assembly manifest resource references.
        // Note that is can be null, but probably shouldn't be.
        // This method could use some refactoring. One thing at a time.
        [System.Security.SecurityCritical]
        internal ResourceSet CreateResourceSet(Stream store, Assembly assembly)
        {
            Contract.Assert(store != null, "I need a Stream!");
            // Check to see if this is a Stream the ResourceManager understands,
            // and check for the correct resource reader type.
            if (store.CanSeek && store.Length > 4)
            {
                long startPos = store.Position;
                
                // not disposing because we want to leave stream open
                BinaryReader br = new BinaryReader(store);
                
                // Look for our magic number as a little endian Int32.
                int bytes = br.ReadInt32();
                if (bytes == ResourceManager.MagicNumber)
                {
                    int resMgrHeaderVersion = br.ReadInt32();
                    String readerTypeName = null, resSetTypeName = null;
                    if (resMgrHeaderVersion == ResourceManager.HeaderVersionNumber)
                    {
                        br.ReadInt32();  // We don't want the number of bytes to skip.
                        readerTypeName = System.CoreLib.FixupCoreLibName(br.ReadString());
                        resSetTypeName = System.CoreLib.FixupCoreLibName(br.ReadString());
                    }
                    else if (resMgrHeaderVersion > ResourceManager.HeaderVersionNumber)
                    {
                        // Assume that the future ResourceManager headers will
                        // have two strings for us - the reader type name and
                        // resource set type name.  Read those, then use the num
                        // bytes to skip field to correct our position.
                        int numBytesToSkip = br.ReadInt32();
                        long endPosition = br.BaseStream.Position + numBytesToSkip;

                        readerTypeName = System.CoreLib.FixupCoreLibName(br.ReadString());
                        resSetTypeName = System.CoreLib.FixupCoreLibName(br.ReadString());

                        br.BaseStream.Seek(endPosition, SeekOrigin.Begin);
                    }
                    else
                    {
                        // resMgrHeaderVersion is older than this ResMgr version.
                        // We should add in backwards compatibility support here.

                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_ObsoleteResourcesFile", _mediator.MainAssembly.GetSimpleName()));
                    }

                    store.Position = startPos;
                    // Perf optimization - Don't use Reflection for our defaults.
                    // Note there are two different sets of strings here - the
                    // assembly qualified strings emitted by ResourceWriter, and
                    // the abbreviated ones emitted by InternalResGen.
                    if (CanUseDefaultResourceClasses(readerTypeName, resSetTypeName))
                    {
                        RuntimeResourceSet rs;
#if LOOSELY_LINKED_RESOURCE_REFERENCE
                        rs = new RuntimeResourceSet(store, assembly);
#else
                        rs = new RuntimeResourceSet(store);
#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
                        return rs;
                    }
                    else
                    {
                        // we do not want to use partial binding here.
                        Type readerType = Type.GetType(readerTypeName, true);
                        Object[] args = new Object[1];
                        args[0] = store;
                        IResourceReader reader = (IResourceReader)Activator.CreateInstance(readerType, args);

                        Object[] resourceSetArgs =
#if LOOSELY_LINKED_RESOURCE_REFERENCE
                            new Object[2];
#else
 new Object[1];
#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
                        resourceSetArgs[0] = reader;
#if LOOSELY_LINKED_RESOURCE_REFERENCE
                        resourceSetArgs[1] = assembly;
#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
                        Type resSetType;
                        if (_mediator.UserResourceSet == null)
                        {
                            Contract.Assert(resSetTypeName != null, "We should have a ResourceSet type name from the custom resource file here.");
                            resSetType = Type.GetType(resSetTypeName, true, false);
                        }
                        else
                            resSetType = _mediator.UserResourceSet;
                        ResourceSet rs = (ResourceSet)Activator.CreateInstance(resSetType,
                                                                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                                                                                null,
                                                                                resourceSetArgs,
                                                                                null,
                                                                                null);
                        return rs;
                    }
                }
                else
                {
                    store.Position = startPos;
                }

            }

            if (_mediator.UserResourceSet == null)
            {
                // Explicitly avoid CreateInstance if possible, because it
                // requires ReflectionPermission to call private & protected
                // constructors.  
#if LOOSELY_LINKED_RESOURCE_REFERENCE                
                return new RuntimeResourceSet(store, assembly);
#else
                return new RuntimeResourceSet(store);
#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
            }
            else
            {
                Object[] args = new Object[2];
                args[0] = store;
                args[1] = assembly;
                try
                {
                    ResourceSet rs = null;
                    // Add in a check for a constructor taking in an assembly first.
                    try
                    {
                        rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
                        return rs;
                    }
                    catch (MissingMethodException) { }

                    args = new Object[1];
                    args[0] = store;
                    rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
#if LOOSELY_LINKED_RESOURCE_REFERENCE
                    rs.Assembly = assembly;
#endif // LOOSELY_LINKED_RESOURCE_REFERENCE
                    return rs;
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResMgrBadResSet_Type", _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }

        [System.Security.SecurityCritical]
        private Stream GetManifestResourceStream(RuntimeAssembly satellite, String fileName, ref StackCrawlMark stackMark)
        {
            Contract.Requires(satellite != null, "satellite shouldn't be null; check caller");
            Contract.Requires(fileName != null, "fileName shouldn't be null; check caller");

            // If we're looking in the main assembly AND if the main assembly was the person who
            // created the ResourceManager, skip a security check for private manifest resources.
            bool canSkipSecurityCheck = (_mediator.MainAssembly == satellite)
                                        && (_mediator.CallingAssembly == _mediator.MainAssembly);

            Stream stream = satellite.GetManifestResourceStream(_mediator.LocationInfo, fileName, canSkipSecurityCheck, ref stackMark);
            if (stream == null)
            {
                stream = CaseInsensitiveManifestResourceStreamLookup(satellite, fileName);
            }

            return stream;
        }

        // Looks up a .resources file in the assembly manifest using 
        // case-insensitive lookup rules.  Yes, this is slow.  The metadata
        // dev lead refuses to make all assembly manifest resource lookups case-insensitive,
        // even optionally case-insensitive.        
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable
        private Stream CaseInsensitiveManifestResourceStreamLookup(RuntimeAssembly satellite, String name)
        {
            Contract.Requires(satellite != null, "satellite shouldn't be null; check caller");
            Contract.Requires(name != null, "name shouldn't be null; check caller");

            StringBuilder sb = new StringBuilder();
            if (_mediator.LocationInfo != null)
            {
                String nameSpace = _mediator.LocationInfo.Namespace;
                if (nameSpace != null)
                {
                    sb.Append(nameSpace);
                    if (name != null)
                        sb.Append(Type.Delimiter);
                }
            }
            sb.Append(name);

            String givenName = sb.ToString();
            CompareInfo comparer = CultureInfo.InvariantCulture.CompareInfo;
            String canonicalName = null;
            foreach (String existingName in satellite.GetManifestResourceNames())
            {
                if (comparer.Compare(existingName, givenName, CompareOptions.IgnoreCase) == 0)
                {
                    if (canonicalName == null)
                    {
                        canonicalName = existingName;
                    }
                    else
                    {
                        throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_MultipleBlobs", givenName, satellite.ToString()));
                    }
                }
            }

#if !FEATURE_CORECLR
            if (FrameworkEventSource.IsInitialized)
            {
                if (canonicalName != null)
                {
                    FrameworkEventSource.Log.ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(_mediator.BaseName, _mediator.MainAssembly, satellite.GetSimpleName(), givenName);
                }
                else
                {
                    FrameworkEventSource.Log.ResourceManagerCaseInsensitiveResourceStreamLookupFailed(_mediator.BaseName, _mediator.MainAssembly, satellite.GetSimpleName(), givenName);
                }
            }
#endif

            if (canonicalName == null)
            {
                return null;
            }
            // If we're looking in the main assembly AND if the main
            // assembly was the person who created the ResourceManager,
            // skip a security check for private manifest resources.
            bool canSkipSecurityCheck = _mediator.MainAssembly == satellite && _mediator.CallingAssembly == _mediator.MainAssembly;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Stream s = satellite.GetManifestResourceStream(canonicalName, ref stackMark, canSkipSecurityCheck);
            // GetManifestResourceStream will return null if we don't have 
            // permission to read this stream from the assembly.  For example,
            // if the stream is private and we're trying to access it from another
            // assembly (ie, ResMgr in mscorlib accessing anything else), we 
            // require Reflection TypeInformation permission to be able to read 
            // this. 
#if !FEATURE_CORECLR
            if (s!=null) {
                if (FrameworkEventSource.IsInitialized)
                {
                    FrameworkEventSource.Log.ResourceManagerManifestResourceAccessDenied(_mediator.BaseName, _mediator.MainAssembly, satellite.GetSimpleName(), canonicalName);
                }
            }
#endif
            return s;
        }

        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var have to be marked non-inlineable
        private RuntimeAssembly GetSatelliteAssembly(CultureInfo lookForCulture, ref StackCrawlMark stackMark)
        {
            if (!_mediator.LookedForSatelliteContractVersion)
            {
                _mediator.SatelliteContractVersion = _mediator.ObtainSatelliteContractVersion(_mediator.MainAssembly);
                _mediator.LookedForSatelliteContractVersion = true;
            }

            RuntimeAssembly satellite = null;
            String satAssemblyName = GetSatelliteAssemblyName();

            // Look up the satellite assembly, but don't let problems
            // like a partially signed satellite assembly stop us from
            // doing fallback and displaying something to the user.
            // Yet also somehow log this error for a developer.
            try
            {
                satellite = _mediator.MainAssembly.InternalGetSatelliteAssembly(satAssemblyName, lookForCulture, _mediator.SatelliteContractVersion, false, ref stackMark);
            }

            // Jun 08: for cases other than ACCESS_DENIED, we'll assert instead of throw to give release builds more opportunity to fallback.

            catch (FileLoadException fle)
            {
                // Ignore cases where the loader gets an access
                // denied back from the OS.  This showed up for
                // href-run exe's at one point.  
                int hr = fle._HResult;
                if (hr != Win32Native.MakeHRFromErrorCode(Win32Native.ERROR_ACCESS_DENIED))
                {
                    Contract.Assert(false, "[This assert catches satellite assembly build/deployment problems - report this message to your build lab & loc engineer]" + Environment.NewLine + "GetSatelliteAssembly failed for culture " + lookForCulture.Name + " and version " + (_mediator.SatelliteContractVersion == null ? _mediator.MainAssembly.GetVersion().ToString() : _mediator.SatelliteContractVersion.ToString()) + " of assembly " + _mediator.MainAssembly.GetSimpleName() + " with error code 0x" + hr.ToString("X", CultureInfo.InvariantCulture) + Environment.NewLine + "Exception: " + fle);
                }
            }

            // Don't throw for zero-length satellite assemblies, for compat with v1
            catch (BadImageFormatException bife)
            {
                Contract.Assert(false, "[This assert catches satellite assembly build/deployment problems - report this message to your build lab & loc engineer]" + Environment.NewLine + "GetSatelliteAssembly failed for culture " + lookForCulture.Name + " and version " + (_mediator.SatelliteContractVersion == null ? _mediator.MainAssembly.GetVersion().ToString() : _mediator.SatelliteContractVersion.ToString()) + " of assembly " + _mediator.MainAssembly.GetSimpleName() + Environment.NewLine + "Exception: " + bife);
            }

#if !FEATURE_CORECLR
            if (FrameworkEventSource.IsInitialized)
            {
                if (satellite != null)
                {
                    FrameworkEventSource.Log.ResourceManagerGetSatelliteAssemblySucceeded(_mediator.BaseName, _mediator.MainAssembly, lookForCulture.Name, satAssemblyName);
                }
                else
                {
                    FrameworkEventSource.Log.ResourceManagerGetSatelliteAssemblyFailed(_mediator.BaseName, _mediator.MainAssembly, lookForCulture.Name, satAssemblyName);
                }
            }
#endif

            return satellite;
        }

        // Perf optimization - Don't use Reflection for most cases with
        // our .resources files.  This makes our code run faster and we can
        // creating a ResourceReader via Reflection.  This would incur
        // a security check (since the link-time check on the constructor that
        // takes a String is turned into a full demand with a stack walk)
        // and causes partially trusted localized apps to fail.
        private bool CanUseDefaultResourceClasses(String readerTypeName, String resSetTypeName)
        {
            Contract.Assert(readerTypeName != null, "readerTypeName shouldn't be null; check caller");
            Contract.Assert(resSetTypeName != null, "resSetTypeName shouldn't be null; check caller");

            if (_mediator.UserResourceSet != null)
                return false;

            // Ignore the actual version of the ResourceReader and 
            // RuntimeResourceSet classes.  Let those classes deal with
            // versioning themselves.
            AssemblyName mscorlib = new AssemblyName(ResourceManager.MscorlibName);

            if (readerTypeName != null)
            {
                if (!ResourceManager.CompareNames(readerTypeName, ResourceManager.ResReaderTypeName, mscorlib))
                    return false;
            }

            if (resSetTypeName != null)
            {
                if (!ResourceManager.CompareNames(resSetTypeName, ResourceManager.ResSetTypeName, mscorlib))
                    return false;
            }

            return true;
        }

        [System.Security.SecurityCritical]
        private String GetSatelliteAssemblyName()
        {
            String satAssemblyName = _mediator.MainAssembly.GetSimpleName();
                satAssemblyName += ".resources";
            return satAssemblyName;
        }

        [System.Security.SecurityCritical]
        private void HandleSatelliteMissing()
        {
            String satAssemName = _mediator.MainAssembly.GetSimpleName() + ".resources.dll";
            if (_mediator.SatelliteContractVersion != null)
            {
                satAssemName += ", Version=" + _mediator.SatelliteContractVersion.ToString();
            }

            AssemblyName an = new AssemblyName();
            an.SetPublicKey(_mediator.MainAssembly.GetPublicKey());
            byte[] token = an.GetPublicKeyToken();

            int iLen = token.Length;
            StringBuilder publicKeyTok = new StringBuilder(iLen * 2);
            for (int i = 0; i < iLen; i++)
            {
                publicKeyTok.Append(token[i].ToString("x", CultureInfo.InvariantCulture));
            }
            satAssemName += ", PublicKeyToken=" + publicKeyTok;

            String missingCultureName = _mediator.NeutralResourcesCulture.Name;
            if (missingCultureName.Length == 0)
            {
                missingCultureName = "<invariant>";
            }
            throw new MissingSatelliteAssemblyException(Environment.GetResourceString("MissingSatelliteAssembly_Culture_Name", _mediator.NeutralResourcesCulture, satAssemName), missingCultureName);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void HandleResourceStreamMissing(String fileName)
        {
            // Keep people from bothering me about resources problems
            if (_mediator.MainAssembly == typeof(Object).Assembly && _mediator.BaseName.Equals(System.CoreLib.Name))
            {
                // This would break CultureInfo & all our exceptions.
                Contract.Assert(false, "Couldn't get " + System.CoreLib.Name+ResourceManager.ResFileExtension + " from "+System.CoreLib.Name+"'s assembly" + Environment.NewLine + Environment.NewLine + "Are you building the runtime on your machine?  Chances are the BCL directory didn't build correctly.  Type 'build -c' in the BCL directory.  If you get build errors, look at buildd.log.  If you then can't figure out what's wrong (and you aren't changing the assembly-related metadata code), ask a BCL dev.\n\nIf you did NOT build the runtime, you shouldn't be seeing this and you've found a bug.");
                
                // We cannot continue further - simply FailFast.
                string mesgFailFast = System.CoreLib.Name + ResourceManager.ResFileExtension + " couldn't be found!  Large parts of the BCL won't work!";
                System.Environment.FailFast(mesgFailFast);
            }
            // We really don't think this should happen - we always
            // expect the neutral locale's resources to be present.
            String resName = String.Empty;
            if (_mediator.LocationInfo != null && _mediator.LocationInfo.Namespace != null)
                resName = _mediator.LocationInfo.Namespace + Type.Delimiter;
            resName += fileName;
            throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoNeutralAsm", resName, _mediator.MainAssembly.GetSimpleName()));
        }

            [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
            [System.Security.SecurityCritical]  // Our security team doesn't yet allow safe-critical P/Invoke methods.
            [System.Security.SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetNeutralResourcesLanguageAttribute(RuntimeAssembly assemblyHandle, StringHandleOnStack cultureName, out short fallbackLocation);
    }
}
