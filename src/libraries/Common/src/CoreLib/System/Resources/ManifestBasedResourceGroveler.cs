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

namespace System.Resources
{
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
    using System.Diagnostics;
    using Microsoft.Win32;

    //
    // Note: this type is integral to the construction of exception objects,
    // and sometimes this has to be done in low memory situtations (OOM) or
    // to create TypeInitializationExceptions due to failure of a static class
    // constructor. This type needs to be extremely careful and assume that
    // any type it references may have previously failed to construct, so statics
    // belonging to that type may not be initialized. FrameworkEventSource.Log
    // is one such example.
    //
    internal partial class ManifestBasedResourceGroveler : IResourceGroveler
    {
        private ResourceManager.ResourceManagerMediator _mediator;

        public ManifestBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            // here and below: convert asserts to preconditions where appropriate when we get
            // contracts story in place.
            Debug.Assert(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        public ResourceSet? GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists)
        {
            Debug.Assert(culture != null, "culture shouldn't be null; check caller");
            Debug.Assert(localResourceSets != null, "localResourceSets shouldn't be null; check caller");

            ResourceSet? rs = null;
            Stream? stream = null;
            Assembly? satellite = null;

            // 1. Fixups for ultimate fallbacks
            CultureInfo lookForCulture = UltimateFallbackFixup(culture);

            // 2. Look for satellite assembly or main assembly, as appropriate
            if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
                // don't bother looking in satellites in this case
                satellite = _mediator.MainAssembly;
            }
            else
            {
                satellite = GetSatelliteAssembly(lookForCulture);

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
            string fileName = _mediator.GetResourceFileName(lookForCulture);

            // 3. If we identified an assembly to search; look in manifest resource stream for resource file
            if (satellite != null)
            {
                // Handle case in here where someone added a callback for assembly load events.
                // While no other threads have called into GetResourceSet, our own thread can!
                // At that point, we could already have an RS in our hash table, and we don't
                // want to add it twice.
                lock (localResourceSets)
                {
                    localResourceSets.TryGetValue(culture.Name, out rs);
                }

                stream = GetManifestResourceStream(satellite, fileName);
            }

            // 4a. Found a stream; create a ResourceSet if possible
            if (createIfNotExists && stream != null && rs == null)
            {
                Debug.Assert(satellite != null, "satellite should not be null when stream is set");
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

            return rs;
        }

        private CultureInfo UltimateFallbackFixup(CultureInfo lookForCulture)
        {
            CultureInfo returnCulture = lookForCulture;

            // If our neutral resources were written in this culture AND we know the main assembly
            // does NOT contain neutral resources, don't probe for this satellite.
            Debug.Assert(_mediator.NeutralResourcesCulture != null);
            if (lookForCulture.Name == _mediator.NeutralResourcesCulture.Name &&
                _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
            {
                returnCulture = CultureInfo.InvariantCulture;
            }
            else if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite)
            {
                returnCulture = _mediator.NeutralResourcesCulture;
            }

            return returnCulture;
        }

        internal static CultureInfo GetNeutralResourcesLanguage(Assembly a, out UltimateResourceFallbackLocation fallbackLocation)
        {
            Debug.Assert(a != null, "assembly != null");

            var attr = a.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
            if (attr == null)
            {
                fallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
                return CultureInfo.InvariantCulture;
            }

            fallbackLocation = attr.Location;
            if (fallbackLocation < UltimateResourceFallbackLocation.MainAssembly || fallbackLocation > UltimateResourceFallbackLocation.Satellite)
            {
                throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, fallbackLocation));
            }

            try
            {
                return CultureInfo.GetCultureInfo(attr.CultureName);
            }
            catch (ArgumentException e)
            { // we should catch ArgumentException only.
                // Note we could go into infinite loops if mscorlib's
                // NeutralResourcesLanguageAttribute is mangled.  If this assert
                // fires, please fix the build process for the BCL directory.
                if (a == typeof(object).Assembly)
                {
                    Debug.Fail(System.CoreLib.Name + "'s NeutralResourcesLanguageAttribute is a malformed culture name! name: \"" + attr.CultureName + "\"  Exception: " + e);
                    return CultureInfo.InvariantCulture;
                }

                throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_Asm_Culture, a, attr.CultureName), e);
            }
        }

        // Constructs a new ResourceSet for a given file name.
        // Use the assembly to resolve assembly manifest resource references.
        // Note that is can be null, but probably shouldn't be.
        // This method could use some refactoring. One thing at a time.
        internal ResourceSet CreateResourceSet(Stream store, Assembly assembly)
        {
            Debug.Assert(store != null, "I need a Stream!");
            // Check to see if this is a Stream the ResourceManager understands,
            // and check for the correct resource reader type.
            if (store.CanSeek && store.Length > 4)
            {
                long startPos = store.Position;

                // not disposing because we want to leave stream open
                BinaryReader br = new BinaryReader(store);

                // Look for our magic number as a little endian int.
                int bytes = br.ReadInt32();
                if (bytes == ResourceManager.MagicNumber)
                {
                    int resMgrHeaderVersion = br.ReadInt32();
                    string? readerTypeName = null, resSetTypeName = null;
                    if (resMgrHeaderVersion == ResourceManager.HeaderVersionNumber)
                    {
                        br.ReadInt32();  // We don't want the number of bytes to skip.
                        readerTypeName = br.ReadString();
                        resSetTypeName = br.ReadString();
                    }
                    else if (resMgrHeaderVersion > ResourceManager.HeaderVersionNumber)
                    {
                        // Assume that the future ResourceManager headers will
                        // have two strings for us - the reader type name and
                        // resource set type name.  Read those, then use the num
                        // bytes to skip field to correct our position.
                        int numBytesToSkip = br.ReadInt32();
                        long endPosition = br.BaseStream.Position + numBytesToSkip;

                        readerTypeName = br.ReadString();
                        resSetTypeName = br.ReadString();

                        br.BaseStream.Seek(endPosition, SeekOrigin.Begin);
                    }
                    else
                    {
                        // resMgrHeaderVersion is older than this ResMgr version.
                        // We should add in backwards compatibility support here.
                        Debug.Assert(_mediator.MainAssembly != null);
                        throw new NotSupportedException(SR.Format(SR.NotSupported_ObsoleteResourcesFile, _mediator.MainAssembly.GetName().Name));
                    }

                    store.Position = startPos;
                    // Perf optimization - Don't use Reflection for our defaults.
                    // Note there are two different sets of strings here - the
                    // assembly qualified strings emitted by ResourceWriter, and
                    // the abbreviated ones emitted by InternalResGen.
                    if (CanUseDefaultResourceClasses(readerTypeName, resSetTypeName))
                    {
                        return new RuntimeResourceSet(store, permitDeserialization: true);
                    }
                    else
                    {
                        IResourceReader reader;

                        // Permit deserialization as long as the default ResourceReader is used
                        if (ResourceManager.IsDefaultType(readerTypeName, ResourceManager.ResReaderTypeName))
                        {
                            reader = new ResourceReader(
                                store,
                                new Dictionary<string, ResourceLocator>(FastResourceComparer.Default),
                                permitDeserialization: true);
                        }
                        else
                        {
                            Type readerType = Type.GetType(readerTypeName, throwOnError: true)!;
                            object[] args = new object[1];
                            args[0] = store;
                            reader = (IResourceReader)Activator.CreateInstance(readerType, args)!;
                        }

                        object[] resourceSetArgs = new object[1];
                        resourceSetArgs[0] = reader;

                        Type resSetType;
                        if (_mediator.UserResourceSet == null)
                        {
                            Debug.Assert(resSetTypeName != null, "We should have a ResourceSet type name from the custom resource file here.");
                            resSetType = Type.GetType(resSetTypeName, true, false)!;
                        }
                        else
                        {
                            resSetType = _mediator.UserResourceSet;
                        }

                        ResourceSet rs = (ResourceSet)Activator.CreateInstance(resSetType,
                                                                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                                                                                null,
                                                                                resourceSetArgs,
                                                                                null,
                                                                                null)!;
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
                return new RuntimeResourceSet(store, permitDeserialization:true);
            }
            else
            {
                object[] args = new object[2];
                args[0] = store;
                args[1] = assembly;
                try
                {
                    ResourceSet? rs = null;
                    // Add in a check for a constructor taking in an assembly first.
                    try
                    {
                        rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args)!;
                        return rs;
                    }
                    catch (MissingMethodException) { }

                    args = new object[1];
                    args[0] = store;
                    rs = (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args)!;

                    return rs;
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResMgrBadResSet_Type, _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }

        private Stream? GetManifestResourceStream(Assembly satellite, string fileName)
        {
            Debug.Assert(satellite != null, "satellite shouldn't be null; check caller");
            Debug.Assert(fileName != null, "fileName shouldn't be null; check caller");

            Stream? stream = satellite.GetManifestResourceStream(_mediator.LocationInfo!, fileName);
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
        private Stream? CaseInsensitiveManifestResourceStreamLookup(Assembly satellite, string name)
        {
            Debug.Assert(satellite != null, "satellite shouldn't be null; check caller");
            Debug.Assert(name != null, "name shouldn't be null; check caller");

            string? nameSpace = _mediator.LocationInfo?.Namespace;

            char c = Type.Delimiter;
            string resourceName = nameSpace != null && name != null ?
                string.Concat(nameSpace, new ReadOnlySpan<char>(ref c, 1), name) :
                string.Concat(nameSpace, name);

            string? canonicalName = null;
            foreach (string existingName in satellite.GetManifestResourceNames())
            {
                if (string.Equals(existingName, resourceName, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (canonicalName == null)
                    {
                        canonicalName = existingName;
                    }
                    else
                    {
                        throw new MissingManifestResourceException(SR.Format(SR.MissingManifestResource_MultipleBlobs, resourceName, satellite.ToString()));
                    }
                }
            }

            if (canonicalName == null)
            {
                return null;
            }

            return satellite.GetManifestResourceStream(canonicalName);
        }

        private Assembly? GetSatelliteAssembly(CultureInfo lookForCulture)
        {
            Debug.Assert(_mediator.MainAssembly != null);
            if (!_mediator.LookedForSatelliteContractVersion)
            {
                _mediator.SatelliteContractVersion = _mediator.ObtainSatelliteContractVersion(_mediator.MainAssembly);
                _mediator.LookedForSatelliteContractVersion = true;
            }

            Assembly? satellite = null;

            // Look up the satellite assembly, but don't let problems
            // like a partially signed satellite assembly stop us from
            // doing fallback and displaying something to the user.
            try
            {
                satellite = InternalGetSatelliteAssembly(_mediator.MainAssembly, lookForCulture, _mediator.SatelliteContractVersion);
            }
            catch (FileLoadException)
            {
            }
            catch (BadImageFormatException)
            {
                // Don't throw for zero-length satellite assemblies, for compat with v1
            }

            return satellite;
        }

        // Perf optimization - Don't use Reflection for most cases with
        // our .resources files.  This makes our code run faster and we can avoid
        // creating a ResourceReader via Reflection.  This would incur
        // a security check (since the link-time check on the constructor that
        // takes a String is turned into a full demand with a stack walk)
        // and causes partially trusted localized apps to fail.
        private bool CanUseDefaultResourceClasses(string readerTypeName, string resSetTypeName)
        {
            Debug.Assert(readerTypeName != null, "readerTypeName shouldn't be null; check caller");
            Debug.Assert(resSetTypeName != null, "resSetTypeName shouldn't be null; check caller");

            if (_mediator.UserResourceSet != null)
                return false;

            // Ignore the actual version of the ResourceReader and
            // RuntimeResourceSet classes.  Let those classes deal with
            // versioning themselves.

            if (readerTypeName != null)
            {
                if (!ResourceManager.IsDefaultType(readerTypeName, ResourceManager.ResReaderTypeName))
                    return false;
            }

            if (resSetTypeName != null)
            {
                if (!ResourceManager.IsDefaultType(resSetTypeName, ResourceManager.ResSetTypeName))
                    return false;
            }

            return true;
        }

        private void HandleSatelliteMissing()
        {
            Debug.Assert(_mediator.MainAssembly != null);
            string satAssemName = _mediator.MainAssembly.GetName().Name + ".resources.dll";
            if (_mediator.SatelliteContractVersion != null)
            {
                satAssemName += ", Version=" + _mediator.SatelliteContractVersion.ToString();
            }

            byte[] token = _mediator.MainAssembly.GetName().GetPublicKeyToken()!;

            int iLen = token.Length;
            StringBuilder publicKeyTok = new StringBuilder(iLen * 2);
            for (int i = 0; i < iLen; i++)
            {
                publicKeyTok.Append(token[i].ToString("x", CultureInfo.InvariantCulture));
            }
            satAssemName += ", PublicKeyToken=" + publicKeyTok;

            Debug.Assert(_mediator.NeutralResourcesCulture != null);
            string missingCultureName = _mediator.NeutralResourcesCulture.Name;
            if (missingCultureName.Length == 0)
            {
                missingCultureName = "<invariant>";
            }
            throw new MissingSatelliteAssemblyException(SR.Format(SR.MissingSatelliteAssembly_Culture_Name, _mediator.NeutralResourcesCulture, satAssemName), missingCultureName);
        }

        private static string GetManifestResourceNamesList(Assembly assembly)
        {
            try
            {
                string postfix = "\"";
                string [] resourceSetNames = assembly.GetManifestResourceNames();

                // If we have more than 10 resource sets, we just print the first 10 for the sake of the exception message readability.
                if (resourceSetNames.Length > 10)
                {
                    resourceSetNames = resourceSetNames[..10];
                    postfix = "\", ...";
                }
                return "\"" + String.Join("\", \"", resourceSetNames) + postfix;
            }
            catch
            {
                return "\"\"";
            }
        }

        private void HandleResourceStreamMissing(string fileName)
        {
            Debug.Assert(_mediator.BaseName != null);
            // Keep people from bothering me about resources problems
            if (_mediator.MainAssembly == typeof(object).Assembly && _mediator.BaseName.Equals(System.CoreLib.Name))
            {
                // This would break CultureInfo & all our exceptions.
                Debug.Fail("Couldn't get " + System.CoreLib.Name + ResourceManager.ResFileExtension + " from " + System.CoreLib.Name + "'s assembly" + Environment.NewLine + Environment.NewLine + "Are you building the runtime on your machine?  Chances are the BCL directory didn't build correctly.  Type 'build -c' in the BCL directory.  If you get build errors, look at buildd.log.  If you then can't figure out what's wrong (and you aren't changing the assembly-related metadata code), ask a BCL dev.\n\nIf you did NOT build the runtime, you shouldn't be seeing this and you've found a bug.");

                // We cannot continue further - simply FailFast.
                string mesgFailFast = System.CoreLib.Name + ResourceManager.ResFileExtension + " couldn't be found!  Large parts of the BCL won't work!";
                System.Environment.FailFast(mesgFailFast);
            }
            // We really don't think this should happen - we always
            // expect the neutral locale's resources to be present.
            string resName = string.Empty;
            if (_mediator.LocationInfo != null && _mediator.LocationInfo.Namespace != null)
                resName = _mediator.LocationInfo.Namespace + Type.Delimiter;
            resName += fileName;
            Debug.Assert(_mediator.MainAssembly != null);
            throw new MissingManifestResourceException(
                            SR.Format(SR.MissingManifestResource_NoNeutralAsm,
                            resName, _mediator.MainAssembly.GetName().Name, GetManifestResourceNamesList(_mediator.MainAssembly)));
        }
    }
}
