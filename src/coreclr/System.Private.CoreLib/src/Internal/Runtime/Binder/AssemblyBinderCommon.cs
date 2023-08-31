// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Internal.Runtime.Binder
{
    internal enum CorPEKind
    {
        peNot = 0x00000000,   // not a PE file
        peILonly = 0x00000001,   // flag IL_ONLY is set in COR header
        pe32BitRequired = 0x00000002,  // flag 32BITREQUIRED is set and 32BITPREFERRED is clear in COR header
        pe32Plus = 0x00000004,   // PE32+ file (64 bit)
        pe32Unmanaged = 0x00000008,    // PE32 without COR header
        pe32BitPreferred = 0x00000010  // flags 32BITREQUIRED and 32BITPREFERRED are set in COR header
    }

    internal static class AssemblyBinderCommon
    {
        public static bool IsCompatibleAssemblyVersion(AssemblyName requestedName, AssemblyName foundName)
        {
            AssemblyVersion pRequestedVersion = requestedName.Version;
            AssemblyVersion pFoundVersion = foundName.Version;

            if (!pRequestedVersion.HasMajor)
            {
                // An unspecified requested version component matches any value for the same component in the found version,
                // regardless of lesser-order version components
                return true;
            }
            if (!pFoundVersion.HasMajor || pRequestedVersion.Major > pFoundVersion.Major)
            {
                // - A specific requested version component does not match an unspecified value for the same component in
                //   the found version, regardless of lesser-order version components
                // - Or, the requested version is greater than the found version
                return false;
            }
            if (pRequestedVersion.Major < pFoundVersion.Major)
            {
                // The requested version is less than the found version
                return true;
            }

            if (!pRequestedVersion.HasMinor)
            {
                return true;
            }
            if (!pFoundVersion.HasMinor || pRequestedVersion.Minor > pFoundVersion.Minor)
            {
                return false;
            }
            if (pRequestedVersion.Minor < pFoundVersion.Minor)
            {
                return true;
            }

            if (!pRequestedVersion.HasBuild)
            {
                return true;
            }
            if (!pFoundVersion.HasBuild || pRequestedVersion.Build > pFoundVersion.Build)
            {
                return false;
            }
            if (pRequestedVersion.Build < pFoundVersion.Build)
            {
                return true;
            }

            if (!pRequestedVersion.HasRevision)
            {
                return true;
            }
            if (!pFoundVersion.HasRevision || pRequestedVersion.Revision > pFoundVersion.Revision)
            {
                return false;
            }
            return true;
        }

        public static void CreateImageAssembly(IntPtr pPEImage, ref BindResult bindResult) => bindResult.SetResult(new Assembly(pPEImage, isInTPA: false));

        // defined in System.Reflection.PortableExecutable.Machine, but it's in System.Reflection.Metadata
        // also defined in System.Reflection.ImageFileMachine
        private const int IMAGE_FILE_MACHINE_I386 = 0x014c;  // Intel 386.
        private const int IMAGE_FILE_MACHINE_ARMNT = 0x01c4;  // ARM Thumb-2 Little-Endian
        private const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;  // AMD64 (K8)
        private const int IMAGE_FILE_MACHINE_ARM64 = 0xAA64;  // ARM64 Little-Endian

        public static unsafe PEKind TranslatePEToArchitectureType(int* pdwPAFlags)
        {
            CorPEKind CLRPeKind = (CorPEKind)pdwPAFlags[0];
            int dwImageType = pdwPAFlags[1];

            if (CLRPeKind == CorPEKind.peNot)
            {
                // Not a PE. Shouldn't ever get here.
                throw new BadImageFormatException();
            }

            if ((CLRPeKind & CorPEKind.peILonly) != 0 && (CLRPeKind & CorPEKind.pe32Plus) == 0 &&
                (CLRPeKind & CorPEKind.pe32BitRequired) == 0 && dwImageType == IMAGE_FILE_MACHINE_I386)
            {
                // Processor-agnostic (MSIL)
                return PEKind.MSIL;
            }
            else if ((CLRPeKind & CorPEKind.pe32Plus) != 0)
            {
                // 64-bit
                if ((CLRPeKind & CorPEKind.pe32BitRequired) != 0)
                {
                    // Invalid
                    throw new BadImageFormatException();
                }

                // Regardless of whether ILONLY is set or not, the architecture
                // is the machine type.
                if (dwImageType == IMAGE_FILE_MACHINE_ARM64)
                    return PEKind.ARM64;
                else if (dwImageType == IMAGE_FILE_MACHINE_AMD64)
                    return PEKind.AMD64;
                else
                {
                    // We don't support other architectures
                    throw new BadImageFormatException();
                }
            }
            else
            {
                // 32-bit, non-agnostic
                if (dwImageType == IMAGE_FILE_MACHINE_I386)
                    return PEKind.I386;
                else if (dwImageType == IMAGE_FILE_MACHINE_ARMNT)
                    return PEKind.ARM;
                else
                {
                    // Not supported
                    throw new BadImageFormatException();
                }
            }
        }

        public static Assembly? BindAssembly(AssemblyBinder binder, AssemblyName assemblyName, bool excludeAppPaths)
        {
            int kContextVersion = 0;
            BindResult bindResult;
            ApplicationContext applicationContext = binder.AppContext;

        // Tracing happens outside the binder lock to avoid calling into managed code within the lock
        //BinderTracing::ResolutionAttemptedOperation tracer{ pAssemblyName, pBinder, 0 /*managedALC*/, hr};

        Retry:
            lock (applicationContext.ContextCriticalSection)
            {
                bindResult = BindByName(applicationContext, assemblyName, false, false, excludeAppPaths);

                // Remember the post-bind version
                kContextVersion = applicationContext.Version;
            }

            // tracer.TraceBindResult(bindResult);

            if (bindResult.Assembly != null)
            {
                if (RegisterAndGetHostChosen(applicationContext, kContextVersion, bindResult, out BindResult hostBindResult))
                {
                    Debug.Assert(hostBindResult.Assembly != null);
                    return hostBindResult.Assembly;
                }
                else
                {
                    // Another bind interfered. We need to retry the entire bind.
                    // This by design loops as long as needed because by construction we eventually
                    // will succeed or fail the bind.
                    bindResult = default;
                    goto Retry;
                }
            }

            return null;
        }

        // Skipped - the managed binder can't bootstrap CoreLib
        // static Assembly? BindToSystem(string systemDirectory);
        // static Assembly? BindToSystemSatellite(string systemDirectory, string simpleName, string cultureName);

        private static BindResult BindByName(ApplicationContext applicationContext, AssemblyName assemblyName, bool skipFailureChecking, bool skipVersionCompatibilityCheck, bool excludeAppPaths)
        {
            // Look for already cached binding failure (ignore PA, every PA will lock the context)

            if (applicationContext.FailureCache.TryGetValue(new FailureCacheKey(assemblyName), out Exception? failure))
            {
                if (failure != null) // FAILED(hr)
                {
                    if (failure is FileNotFoundException && skipFailureChecking)
                    {
                        // Ignore pre-existing transient bind error (re-bind will succeed)
                        applicationContext.FailureCache.Remove(new FailureCacheKey(assemblyName));
                    }

                    return default;
                }
                else // hr == HResults.S_FALSE
                {
                    // workaround: Special case for byte arrays. Rerun the bind to create binding log.
                    assemblyName.IsDefinition = true;
                }
            }

            BindResult bindResult = default;

            try
            {
                if (!IsValidArchitecture(assemblyName.Architecture))
                {
                    // Assembly reference contains wrong architecture
                    throw new Exception("FUSION_E_INVALID_NAME");
                }

                BindLocked(applicationContext, assemblyName, skipVersionCompatibilityCheck, excludeAppPaths, ref bindResult);

                if (bindResult.Assembly == null)
                {
                    // Behavior rules are clueless now
                    throw new FileNotFoundException();
                }

                return bindResult;
            }
            catch (Exception ex)
            {
                if (skipFailureChecking)
                {
                    if (ex is not FileNotFoundException)
                    {
                        // Cache non-transient bind error for byte-array
                        ex = null!;
                    }
                    else
                    {
                        // Ignore transient bind error (re-bind will succeed)
                        return bindResult;
                    }
                }

                applicationContext.AddToFailureCache(assemblyName, ex);

                throw;
            }
        }

        private static void BindLocked(ApplicationContext applicationContext, AssemblyName assemblyName, bool skipVersionCompatibilityCheck, bool excludeAppPaths, ref BindResult bindResult)
        {
            bool isTpaListProvided = applicationContext.TrustedPlatformAssemblyMap != null;
            Assembly? assembly = null;

            try
            {
                assembly = FindInExecutionContext(applicationContext, assemblyName);
            }
            catch (Exception ex)
            {
                bindResult.SetAttemptResult(assembly, ex, isInContext: true);
                throw;
            }

            if (assembly == null) // if (FAILED(hr) || pAssembly == NULL)
            {
                bindResult.SetAttemptResult(null, null, isInContext: true);
            }

            if (assembly != null)
            {
                if (!skipVersionCompatibilityCheck)
                {
                    // Can't give higher version than already bound
                    bool isCompatible = IsCompatibleAssemblyVersion(assemblyName, assembly.AssemblyName);
                    Exception? exception = isCompatible ? null : new Exception("FUSION_E_APP_DOMAIN_LOCKED");
                    bindResult.SetAttemptResult(assembly, exception, isInContext: true);

                    // TPA binder returns FUSION_E_REF_DEF_MISMATCH for incompatible version
                    if (exception != null && isTpaListProvided) // hr == FUSION_E_APP_DOMAIN_LOCKED
                        exception = new Exception("FUSION_E_REF_DEF_MISMATCH");

                    if (exception != null)
                        throw exception;
                }
                else
                {
                    bindResult.SetAttemptResult(assembly, null, isInContext: true);
                }

                bindResult.SetResult(assembly, isInContext: true);
            }
            else if (isTpaListProvided)
            {
                // BindByTpaList handles setting attempt results on the bind result
                try
                {
                    BindByTpaList(applicationContext, assemblyName, excludeAppPaths, ref bindResult);

                    if (bindResult.Assembly != null) // SUCCEEDED(hr) && pBindResult->HaveResult()
                    {
                        bool isCompatible = IsCompatibleAssemblyVersion(assemblyName, bindResult.Assembly.AssemblyName);
                        Exception? exception = isCompatible ? null : new Exception("FUSION_E_APP_DOMAIN_LOCKED");
                        bindResult.SetAttemptResult(bindResult.Assembly, exception, isInContext: false);

                        // TPA binder returns FUSION_E_REF_DEF_MISMATCH for incompatible version
                        if (exception != null && isTpaListProvided) // hr == FUSION_E_APP_DOMAIN_LOCKED
                            exception = new Exception("FUSION_E_REF_DEF_MISMATCH");

                        if (exception != null)
                            throw exception;
                    }
                }
                catch
                {
                    bindResult.SetNoResult();
                    throw;
                }
            }
        }

        private static Assembly? FindInExecutionContext(ApplicationContext applicationContext, AssemblyName assemblyName)
        {
            applicationContext.ExecutionContext.TryGetValue(assemblyName, out Assembly? assembly);

            // Set any found context entry. It is up to the caller to check the returned HRESULT
            // for errors due to validation

            if (assembly != null && assemblyName.IsDefinition
                && (assembly.AssemblyName.Architecture != assemblyName.Architecture))
            {
                throw new Exception("FUSION_E_APP_DOMAIN_LOCKED");
            }

            return assembly;
        }

        //
        // Tests whether a candidate assembly's name matches the requested.
        // This does not do a version check.  The binder applies version policy
        // further up the stack once it gets a successful bind.
        //
        private static bool TestCandidateRefMatchesDef(AssemblyName requestedAssemblyName, AssemblyName boundAssemblyName, bool tpaListAssembly)
        {
            AssemblyNameIncludeFlags includeFlags = AssemblyNameIncludeFlags.INCLUDE_DEFAULT;

            if (!tpaListAssembly)
            {
                if (requestedAssemblyName.IsNeutralCulture)
                {
                    includeFlags |= AssemblyNameIncludeFlags.EXCLUDE_CULTURE;
                }
            }

            if (requestedAssemblyName.Architecture != PEKind.None)
            {
                includeFlags |= AssemblyNameIncludeFlags.INCLUDE_ARCHITECTURE;
            }

            return boundAssemblyName.Equals(requestedAssemblyName, includeFlags);
        }

        // TODO: BindSatelliteResource

        private static Assembly BindAssemblyByProbingPaths(List<string> bindingPaths, AssemblyName requestedAssemblyName)
        {
            // Loop through the binding paths looking for a matching assembly
            foreach (string bindingPath in bindingPaths)
            {
                string fileNameWithoutExtension = Path.Combine(bindingPath, requestedAssemblyName.SimpleName);

                // Look for a matching dll first
                string fileName = fileNameWithoutExtension + ".dll";

                try
                {
                    Assembly assembly = GetAssembly(fileName, false);

                    // BinderTracing::PathProbed(fileName, pathSource, hr);

                    // We found a candidate.
                    //
                    // Below this point, we either establish that the ref-def matches, or
                    // we fail the bind.

                    // Compare requested AssemblyName with that from the candidate assembly
                    if (!TestCandidateRefMatchesDef(requestedAssemblyName, assembly.AssemblyName, false))
                        throw new Exception("FUSION_E_REF_DEF_MISMATCH");

                    return assembly; // S_OK
                }
                catch (Exception ex)
                {
                    if (ex is FileNotFoundException)
                    {
                        // Since we're probing, file not founds are ok and we should just try another
                        // probing path
                        continue;
                    }

                }
            }

            throw new FileNotFoundException();
        }

        /*
         * BindByTpaList is the entry-point for the custom binding algorithm in CoreCLR.
         *
         * The search for assemblies will proceed in the following order:
         *
         * If this application is a single-file bundle, the meta-data contained in the bundle
         * will be probed to find the requested assembly. If the assembly is not found,
         * The list of platform assemblies (TPAs) are considered next.
         *
         * Platform assemblies are specified as a list of files.  This list is the only set of
         * assemblies that we will load as platform.  They can be specified as IL or NIs.
         *
         * Resources for platform assemblies are located by probing starting at the Platform Resource Roots,
         * a set of folders configured by the host.
         *
         * If a requested assembly identity cannot be found in the TPA list or the resource roots,
         * it is considered an application assembly.  We probe for application assemblies in the
         * AppPaths, a list of paths containing IL files and satellite resource folders.
         *
         */

        public static void BindByTpaList(ApplicationContext applicationContext, AssemblyName requestedAssemblyName, bool excludeAppPaths, ref BindResult bindResult)
        {
            bool fPartialMatchOnTpa = false;

            if (requestedAssemblyName.IsNeutralCulture)
            {
                // IF_FAIL_GO(BindSatelliteResource(pApplicationContext, pRequestedAssemblyName, pBindResult));
            }
            else
            {
                Assembly? tpaAssembly = null;

                // Is assembly in the bundle?
                // Single-file bundle contents take precedence over TPA.
                // The list of bundled assemblies is contained in the bundle manifest, and NOT in the TPA.
                // Therefore the bundle is first probed using the assembly's simple name.
                // If found, the assembly is loaded from the bundle.

                // if (Bundle::AppIsBundle())

                // Is assembly on TPA list?
                Debug.Assert(applicationContext.TrustedPlatformAssemblyMap != null);
                if (applicationContext.TrustedPlatformAssemblyMap.TryGetValue(requestedAssemblyName.SimpleName, out TPAEntry tpaEntry))
                {
                    string? tpaFileName = tpaEntry.NIFileName ?? tpaEntry.ILFileName;
                    Debug.Assert(tpaFileName != null);

                    try
                    {
                        tpaAssembly = GetAssembly(tpaFileName, isInTPA: true);
                        // BinderTracing::PathProbed(fileName, BinderTracing::PathSource::ApplicationAssemblies, hr);

                        bindResult.SetAttemptResult(tpaAssembly, null);

                        if (TestCandidateRefMatchesDef(requestedAssemblyName, tpaAssembly.AssemblyName, true))
                        {
                            // We have found the requested assembly match on TPA with validation of the full-qualified name. Bind to it.
                            bindResult.SetResult(tpaAssembly);
                            bindResult.SetAttemptResult(tpaAssembly, null);
                            return;
                        }
                        else
                        {
                            // We found the assembly on TPA but it didn't match the RequestedAssembly assembly-name. In this case, lets proceed to see if we find the requested
                            // assembly in the App paths.
                            bindResult.SetAttemptResult(tpaAssembly, new Exception("FUSION_E_REF_DEF_MISMATCH"));
                            fPartialMatchOnTpa = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        bindResult.SetAttemptResult(null, ex);

                        // On file not found, simply fall back to app path probing
                        if (ex is not FileNotFoundException)
                        {
                            // Any other error is fatal
                            throw;
                        }
                    }

                    // We either didn't find a candidate, or the ref-def failed.  Either way; fall back to app path probing.
                }

                if (!excludeAppPaths)
                {
                    // Probe AppPaths

                    try
                    {
                        Assembly assembly = BindAssemblyByProbingPaths(applicationContext.AppPaths, requestedAssemblyName);
                        bindResult.SetAttemptResult(assembly, null);

                        // At this point, we have found an assembly with the expected name in the App paths. If this was also found on TPA,
                        // make sure that the app assembly has the same fullname (excluding version) as the TPA version. If it does, then
                        // we should bind to the TPA assembly. If it does not, then bind to the app assembly since it has a different fullname than the
                        // TPA assembly.
                        if (fPartialMatchOnTpa)
                        {
                            Debug.Assert(tpaAssembly != null);

                            if (TestCandidateRefMatchesDef(assembly.AssemblyName, tpaAssembly.AssemblyName, true))
                            {
                                // Fullname (SimpleName+Culture+PKT) matched for TPA and app assembly - so bind to TPA instance.
                                bindResult.SetResult(tpaAssembly);
                                bindResult.SetAttemptResult(tpaAssembly, null);
                                return;
                            }
                            else
                            {
                                // Fullname (SimpleName+Culture+PKT) did not match for TPA and app assembly - so bind to app instance.
                                bindResult.SetResult(assembly);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        bindResult.SetAttemptResult(null, ex);

                        if (ex is not FileNotFoundException)
                        {
                            throw;
                        }
                    }
                }
            }

            // Couldn't find a matching assembly in any of the probing paths
            // Return S_FALSE here. BindByName will interpret a successful HRESULT
            // and lack of BindResult as a failure to find a matching assembly.
            return;
        }

        // static Assembly GetAssembly(string assemblyPath, bool isInTPA, BundleFileLocation);

        public static void Register(ApplicationContext applicationContext, ref BindResult bindResult)
        {
            Debug.Assert(bindResult.IsContextBound);
            Debug.Assert(bindResult.Assembly != null);

            applicationContext.IncrementVersion();

            // Register the bindResult in the ExecutionContext only if we dont have it already.
            // This method is invoked under a lock (by its caller), so we are thread safe.
            Assembly? assembly = FindInExecutionContext(applicationContext, bindResult.Assembly.AssemblyName);

            if (assembly == null)
            {
                applicationContext.ExecutionContext.Add(bindResult.Assembly.AssemblyName, bindResult.Assembly);
            }
            else
            {
                // Update the BindResult with the assembly we found
                bindResult.SetResult(assembly, isInContext: true);
            }
        }

        public static bool RegisterAndGetHostChosen(ApplicationContext applicationContext, int kContextVersion, BindResult bindResult, out BindResult hostBindResult)
        {
            Debug.Assert(bindResult.Assembly != null);
            hostBindResult = default;

            if (!bindResult.IsContextBound)
            {
                hostBindResult = bindResult;

                // Lock the application context
                lock (applicationContext.ContextCriticalSection)
                {
                    // Only perform costly validation if other binds succeeded before us
                    if (kContextVersion != applicationContext.Version)
                    {
                        if (OtherBindInterferred(applicationContext, bindResult)) // S_FALSE == return true
                        {
                            // Another bind interfered
                            return false; // S_FALSE == return false
                        }
                    }

                    // No bind interfered, we can now register
                    Register(applicationContext, ref hostBindResult);
                }
            }
            else
            {
                // No work required. Return the input
                hostBindResult = bindResult;
            }

            return true;
        }

        private static bool OtherBindInterferred(ApplicationContext applicationContext, BindResult bindResult)
        {
            Debug.Assert(bindResult.Assembly != null);

            // Look for already cached binding failure (ignore PA, every PA will lock the context)
            if (!applicationContext.FailureCache.ContainsKey(new FailureCacheKey(bindResult.Assembly.AssemblyName)))
            {
                // hr == S_OK
                try
                {
                    Assembly? assembly = FindInExecutionContext(applicationContext, bindResult.Assembly.AssemblyName);
                    if (assembly != null) // SUCCEEDED(hr)
                    {
                        // We can accept this bind in the domain
                        return false; // S_OK == return false
                    }
                }
                catch
                {
                    // No throwing
                }

            }

            // Some other bind interfered
            return true; // S_FALSE == return true
        }

        static Assembly GetAssembly(string assemblyPath, bool isInTPA)
        {
            throw null;
        }

        public static bool IsValidArchitecture(PEKind architecture)
        {
            if (architecture is PEKind.MSIL or PEKind.None)
                return true;

            PEKind processArchitecture =
#if TARGET_X86
                PEKind.I386;
#elif TARGET_AMD64
                PEKind.AMD64;
#elif TARGET_ARM
                PEKind.ARM;
#elif TARGET_ARM64
                PEKind.ARM64;
#else
                PEKind.MSIL;
#endif

            return architecture == processArchitecture;
        }
    }
}
