// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** 
** 
**
**
** Purpose: For Assembly-related stuff.
**
**
=============================================================================*/

namespace System.Reflection 
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Security;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.IO;
    using StringBuilder = System.Text.StringBuilder;
    using System.Configuration.Assemblies;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using SecurityZone = System.Security.SecurityZone;
    using IEvidenceFactory = System.Security.IEvidenceFactory;
    using System.Runtime.Serialization;
    using Microsoft.Win32;
    using System.Threading;
    using __HResults = System.__HResults;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate Module ModuleResolveEventHandler(Object sender, ResolveEventArgs e);


    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Assembly))]
    [System.Runtime.InteropServices.ComVisible(true)]
#pragma warning disable 618
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Unrestricted = true)]
#pragma warning restore 618
    public abstract class Assembly : _Assembly, IEvidenceFactory, ICustomAttributeProvider, ISerializable
    {
#region constructors
        protected Assembly() {}
#endregion

#region public static methods

        public static String CreateQualifiedName(String assemblyName, String typeName)
        {
            return typeName + ", " + assemblyName;
        }

        public static Assembly GetAssembly(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            Module m = type.Module;
            if (m == null)
                return null;
            else
                return m.Assembly;
        }

        public static bool operator ==(Assembly left, Assembly right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeAssembly || right is RuntimeAssembly)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(Assembly left, Assembly right)
        {
            return !(left == right);
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // Locate an assembly by the name of the file containing the manifest.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadFrom(String assemblyFile)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

#if FEATURE_WINDOWSPHONE
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_WindowsPhone", "Assembly.LoadFrom"));
#else
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile,
                null, // securityEvidence
                null, // hashValue
                AssemblyHashAlgorithm.None,
                false,// forIntrospection
                false,// suppressSecurityChecks
                ref stackMark);
#endif // FEATURE_WINDOWSPHONE
        }

        // Locate an assembly for reflection by the name of the file containing the manifest.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly ReflectionOnlyLoadFrom(String assemblyFile)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile,
                null, //securityEvidence
                null, //hashValue
                AssemblyHashAlgorithm.None,
                true,  //forIntrospection
                false, //suppressSecurityChecks
                ref stackMark);
        }

        // Evidence is protected in Assembly.Load()
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadFrom(String assemblyFile,
                                        Evidence securityEvidence)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile,
                securityEvidence,
                null, // hashValue
                AssemblyHashAlgorithm.None,
                false,// forIntrospection);
                false,// suppressSecurityChecks
                ref stackMark);
        }

        // Evidence is protected in Assembly.Load()
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadFrom(String assemblyFile,
                                        Evidence securityEvidence,
                                        byte[] hashValue,
                                        AssemblyHashAlgorithm hashAlgorithm)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile, 
                securityEvidence, 
                hashValue, 
                hashAlgorithm, 
                false,
                false,
                ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadFrom(String assemblyFile,
                                        byte[] hashValue,
                                        AssemblyHashAlgorithm hashAlgorithm)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(
                assemblyFile, 
                null, 
                hashValue, 
                hashAlgorithm, 
                false,
                false,
                ref stackMark);
        }

#if FEATURE_CAS_POLICY
        // Load an assembly into the LoadFrom context bypassing some security checks
        [SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly UnsafeLoadFrom(string assemblyFile)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            return RuntimeAssembly.InternalLoadFrom(assemblyFile,
                                                    null, // securityEvidence
                                                    null, // hashValue
                                                    AssemblyHashAlgorithm.None,
                                                    false, // forIntrospection
                                                    true, // suppressSecurityChecks
                                                    ref stackMark);
        }
#endif // FEATURE_CAS_POLICY

        // Locate an assembly by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(String assemblyString)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false /*forIntrospection*/);
        }

        // Returns type from the assembly while keeping compatibility with Assembly.Load(assemblyString).GetType(typeName) for managed types.
        // Calls Type.GetType for WinRT types.
        // Note: Type.GetType fails for assembly names that start with weird characters like '['. By calling it for managed types we would 
        // break AppCompat.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static Type GetType_Compat(String assemblyString, String typeName)
        {
            // Normally we would get the stackMark only in public APIs. This is internal API, but it is AppCompat replacement of public API 
            // call Assembly.Load(assemblyString).GetType(typeName), therefore we take the stackMark here as well, to be fully compatible with 
            // the call sequence.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            RuntimeAssembly assembly;
            AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(
                assemblyString,
                false /*forIntrospection*/,
                out assembly);

            if (assembly == null) {
                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime) {
                    return Type.GetType(typeName + ", " + assemblyString, true /*throwOnError*/, false /*ignoreCase*/);
                }

                assembly = RuntimeAssembly.InternalLoadAssemblyName(
                    assemblyName, null, null, ref stackMark,
                    true /*thrownOnFileNotFound*/, false /*forIntrospection*/, false /*suppressSecurityChecks*/);
            }
            return assembly.GetType(typeName, true /*throwOnError*/, false /*ignoreCase*/);
        }

        // Locate an assembly for reflection by the long form of the assembly name. 
        // eg. "Toolbox.dll, version=1.1.10.1220, locale=en, publickey=1234567890123456789012345678901234567890"
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly ReflectionOnlyLoad(String assemblyString)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null,  ref stackMark, true /*forIntrospection*/);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static Assembly Load(String assemblyString, Evidence assemblySecurity)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackMark, false /*forIntrospection*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(AssemblyName assemblyRef)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

#if FEATURE_WINDOWSPHONE
            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadCodeBase"));
            }
#endif

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/, false /*suppressSecurityChecks*/);
        }

        // Locate an assembly by its name. The name can be strong or
        // weak. The assembly is loaded into the domain of the caller.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static Assembly Load(AssemblyName assemblyRef, IntPtr ptrLoadContextBinder)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

#if FEATURE_WINDOWSPHONE
            if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadCodeBase"));
            }
#endif

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/, false /*suppressSecurityChecks*/, ptrLoadContextBinder);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackMark, true /*thrownOnFileNotFound*/, false /*forIntrospection*/, false /*suppressSecurityChecks*/);
        }

#if FEATURE_FUSION
        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadWithPartialName(String partialName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.LoadWithPartialNameInternal(partialName, null, ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly LoadWithPartialName(String partialName, Evidence securityEvidence)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.LoadWithPartialNameInternal(partialName, securityEvidence, ref stackMark);
        }
#endif // FEATURE_FUSION

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(byte[] rawAssembly)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(
                rawAssembly,
                null, // symbol store
                null, // evidence
                ref stackMark,
                false,  // fIntrospection
                SecurityContextSource.CurrentAssembly);
        }

        // Loads the assembly for reflection with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly ReflectionOnlyLoad(byte[] rawAssembly)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);

            AppDomain.CheckReflectionOnlyLoadSupported();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(
                rawAssembly,
                null, // symbol store
                null, // evidence
                ref stackMark,
                true,  // fIntrospection
                SecurityContextSource.CurrentAssembly);
        }

        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller. The second parameter is the raw bytes
        // representing the symbol store that matches the assembly.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(
                rawAssembly,
                rawSymbolStore,
                null, // evidence
                ref stackMark,
                false,  // fIntrospection
                SecurityContextSource.CurrentAssembly);
        }

        // Load an assembly from a byte array, controlling where the grant set of this assembly is
        // propigated from.
        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.NoInlining)]  // Due to the stack crawl mark
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore,
                                    SecurityContextSource securityContextSource)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            if (securityContextSource < SecurityContextSource.CurrentAppDomain ||
                securityContextSource > SecurityContextSource.CurrentAssembly)
            {
                throw new ArgumentOutOfRangeException("securityContextSource");
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly,
                                              rawSymbolStore,
                                              null,             // evidence
                                              ref stackMark,
                                              false,            // fIntrospection
                                              securityContextSource);
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlEvidence)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of Load which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static Assembly Load(byte[] rawAssembly,
                                    byte[] rawSymbolStore,
                                    Evidence securityEvidence)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadByteArraySupported();

            if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                // A zone of MyComputer could not have been used to sandbox, so for compatibility we do not
                // throw an exception when we see it.
                Zone zone = securityEvidence.GetHostEvidence<Zone>();
                if (zone == null || zone.SecurityZone != SecurityZone.MyComputer)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
                }
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(
                rawAssembly,
                rawSymbolStore,
                securityEvidence,
                ref stackMark,
                false,  // fIntrospection
                SecurityContextSource.CurrentAssembly);
        }
#endif // FEATURE_CAS_POLICY

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Assembly LoadFile(String path)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadFileSupported();

            new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, path).Demand();
            return RuntimeAssembly.nLoadFile(path, null);
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecuritySafeCritical]  // auto-generated
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.ControlEvidence)]
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of LoadFile which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static Assembly LoadFile(String path,
                                        Evidence securityEvidence)
        {

            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            AppDomain.CheckLoadFileSupported();

            if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));

            new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, path).Demand();
            return RuntimeAssembly.nLoadFile(path, securityEvidence);
        }
#endif // FEATURE_CAS_POLICY

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(Stream assemblyStream, Stream pdbStream)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFromStream(assemblyStream, pdbStream, ref stackMark);
        }
        
        [System.Security.SecurityCritical] // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly Load(Stream assemblyStream)
        {
            Contract.Ensures(Contract.Result<Assembly>() != null);
            Contract.Ensures(!Contract.Result<Assembly>().ReflectionOnly);

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFromStream(assemblyStream, null, ref stackMark);
        }
#endif //FEATURE_CORECLR

        /*
         * Get the assembly that the current code is running from.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable 
        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }
       
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static Assembly GetCallingAssembly()
        {
            // LookForMyCallersCaller is not guarantee to return the correct stack frame
            // because of inlining, tail calls, etc. As a result GetCallingAssembly is not 
            // ganranteed to return the correct result. We should document it as such.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }
       
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static Assembly GetEntryAssembly() {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();
            return domainManager.EntryAssembly;
        }
    
#endregion // public static methods

#region public methods
        public virtual event ModuleResolveEventHandler ModuleResolve
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            add
            {
                throw new NotImplementedException();
            }
            [System.Security.SecurityCritical]  // auto-generated_required
            remove
            {
                throw new NotImplementedException();
            }
        }

        public virtual String CodeBase
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String EscapedCodeBase
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return AssemblyName.EscapeCodeBase(CodeBase);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public virtual AssemblyName GetName()
        {
            return GetName(false);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public virtual AssemblyName GetName(bool copiedName)
        {
            throw new NotImplementedException();
        }

        public virtual String FullName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual MethodInfo EntryPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

#if !FEATURE_CORECLR
        Type _Assembly.GetType()
        {
            return base.GetType();
        }
#endif

        public virtual Type GetType(String name)
        {
            return GetType(name, false, false);
        }

        public virtual Type GetType(String name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public virtual Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<Type> ExportedTypes
        {
            get
            {
                return GetExportedTypes();
            }
        }

        public virtual Type[] GetExportedTypes()
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                Type[] types = GetTypes();

                TypeInfo[] typeinfos = new TypeInfo[types.Length];

                for (int i = 0; i < types.Length; i++)
                {

                    TypeInfo typeinfo = types[i].GetTypeInfo();
                    if (typeinfo == null)
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoTypeInfo", types[i].FullName));

                    typeinfos[i] = typeinfo;
                }

                return typeinfos;
            }
        }

        public virtual Type[] GetTypes()
        {
            Module[] m = GetModules(false);

            int iNumModules = m.Length;
            int iFinalLength = 0;
            Type[][] ModuleTypes = new Type[iNumModules][];

            for (int i = 0; i < iNumModules; i++)
            {
                ModuleTypes[i] = m[i].GetTypes();
                iFinalLength += ModuleTypes[i].Length;
            }

            int iCurrent = 0;
            Type[] ret = new Type[iFinalLength];
            for (int i = 0; i < iNumModules; i++)
            {
                int iLength = ModuleTypes[i].Length;
                Array.Copy(ModuleTypes[i], 0, ret, iCurrent, iLength);
                iCurrent += iLength;
            }

            return ret;
        }

        // Load a resource based on the NameSpace of the type.
        public virtual Stream GetManifestResourceStream(Type type, String name)
        {
            throw new NotImplementedException();
        }

        public virtual Stream GetManifestResourceStream(String name)
        {
            throw new NotImplementedException();
        }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // Useful for binding to a very specific version of a satellite assembly
        public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            throw new NotImplementedException();
        }

#if FEATURE_CAS_POLICY
        public virtual Evidence Evidence
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual PermissionSet PermissionSet
        {
            // SecurityCritical because permissions can contain sensitive information such as paths
            [SecurityCritical]
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsFullyTrusted
        {
            [SecuritySafeCritical]
            get
            {
                return PermissionSet.IsUnrestricted();
            }
        }

        public virtual SecurityRuleSet SecurityRuleSet
        {
            get
            {
                throw new NotImplementedException();
            }
        }

#endif // FEATURE_CAS_POLICY

        // ISerializable implementation
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        [ComVisible(false)]
        public virtual Module ManifestModule
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.ManifestModule;

                throw new NotImplementedException();
            }
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }
        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            Contract.Ensures(Contract.Result<Object[]>() != null);
            throw new NotImplementedException();
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            Contract.Ensures(Contract.Result<Object[]>() != null);
            throw new NotImplementedException();
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        // To not break compatibility with the V1 _Assembly interface we need to make this
        // new member ComVisible(false).
        [ComVisible(false)]
        public virtual bool ReflectionOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

#if FEATURE_MULTIMODULE_ASSEMBLIES

        public Module LoadModule(String moduleName,
                                 byte[] rawModule)
        {
            return LoadModule(moduleName, rawModule, null);
        }

        public virtual Module LoadModule(String moduleName,
                                 byte[] rawModule,
                                 byte[] rawSymbolStore)
        {
            throw new NotImplementedException();
        }
#endif //FEATURE_MULTIMODULE_ASSEMBLIES

        //
        // Locates a type from this assembly and creates an instance of it using
        // the system activator. 
        //
        public Object CreateInstance(String typeName)
        {
            return CreateInstance(typeName,
                                  false, // ignore case
                                  BindingFlags.Public | BindingFlags.Instance,
                                  null, // binder
                                  null, // args
                                  null, // culture
                                  null); // activation attributes
        }

        public Object CreateInstance(String typeName,
                                     bool ignoreCase)
        {
            return CreateInstance(typeName,
                                  ignoreCase,
                                  BindingFlags.Public | BindingFlags.Instance,
                                  null, // binder
                                  null, // args
                                  null, // culture
                                  null); // activation attributes
        }

        public virtual Object CreateInstance(String typeName, 
                                     bool ignoreCase,
                                     BindingFlags bindingAttr, 
                                     Binder binder,
                                     Object[] args,
                                     CultureInfo culture,
                                     Object[] activationAttributes)
        {
            Type t = GetType(typeName, false, ignoreCase);
            if (t == null) return null;
            return Activator.CreateInstance(t,
                                            bindingAttr,
                                            binder,
                                            args,
                                            culture,
                                            activationAttributes);
        }

        public virtual IEnumerable<Module> Modules
        {
            get
            {
                return GetLoadedModules(true);
            }
        }
                                     
        public Module[] GetLoadedModules()
        {
            return GetLoadedModules(false);
        }

        public virtual Module[] GetLoadedModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }
                                     
        public Module[] GetModules()
        {
            return GetModules(false);
        }

        public virtual Module[] GetModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public virtual Module GetModule(String name)
        {
            throw new NotImplementedException();
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public virtual FileStream GetFile(String name)
        {
            throw new NotImplementedException();
        }

        public virtual FileStream[] GetFiles()
        {
            return GetFiles(false);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        public virtual FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        // Returns the names of all the resources
        public virtual String[] GetManifestResourceNames()
        {
            throw new NotImplementedException();
        }

        public virtual AssemblyName[] GetReferencedAssemblies()
        {
            throw new NotImplementedException();
        }

        public virtual ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            throw new NotImplementedException();
        }

        public override String ToString()
        {
            String displayName = FullName; 
            if (displayName == null)
                return base.ToString();
            else
                return displayName;
        }

        public virtual String Location
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            get
            {
                throw new NotImplementedException();
            }
        }

        // To not break compatibility with the V1 _Assembly interface we need to make this
        // new member ComVisible(false).
        [ComVisible(false)]
        public virtual String ImageRuntimeVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /*
          Returns true if the assembly was loaded from the global assembly cache.
        */        
        public virtual bool GlobalAssemblyCache
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        [ComVisible(false)]
        public virtual Int64 HostContext
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.HostContext;

                throw new NotImplementedException();
            }
        }

        public virtual bool IsDynamic
        {
            get
            {
                return false;
            }
        }
#endregion // public methods

    }

    // Keep this in sync with LOADCTX_TYPE defined in fusionpriv.idl
    internal enum LoadContext
    {
       DEFAULT,
       LOADFROM,
       UNKNOWN,
       HOSTED,
    }

    [Serializable]
    internal class RuntimeAssembly : Assembly
#if !FEATURE_CORECLR
        , ICustomQueryInterface
#endif
    {
#if !FEATURE_CORECLR
#region ICustomQueryInterface
        [System.Security.SecurityCritical]
        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface([In]ref Guid iid, out IntPtr ppv)
        {
            if (iid == typeof(NativeMethods.IDispatch).GUID)
            {
                ppv = Marshal.GetComInterfaceForObject(this, typeof(_Assembly));
                return CustomQueryInterfaceResult.Handled;
            }

            ppv = IntPtr.Zero;
            return CustomQueryInterfaceResult.NotHandled;
        }
#endregion
#endif // !FEATURE_CORECLR

#if FEATURE_APPX
        // The highest byte is the flags and the lowest 3 bytes are 
        // the cached ctor token of [DynamicallyInvocableAttribute].
        private enum ASSEMBLY_FLAGS : uint
        {
            ASSEMBLY_FLAGS_UNKNOWN =            0x00000000,
            ASSEMBLY_FLAGS_INITIALIZED =        0x01000000,
            ASSEMBLY_FLAGS_FRAMEWORK =          0x02000000,
            ASSEMBLY_FLAGS_SAFE_REFLECTION =    0x04000000,
            ASSEMBLY_FLAGS_TOKEN_MASK =         0x00FFFFFF,
        }
#endif // FEATURE_APPX

        private const uint COR_E_LOADING_REFERENCE_ASSEMBLY = 0x80131058U;

        internal RuntimeAssembly() { throw new NotSupportedException(); }

#region private data members
        [method: System.Security.SecurityCritical]
        private event ModuleResolveEventHandler _ModuleResolve;
        private string m_fullname;
        private object m_syncRoot;   // Used to keep collectible types alive and as the syncroot for reflection.emit
        private IntPtr m_assembly;    // slack for ptr datum on unmanaged side

#if FEATURE_APPX
        private ASSEMBLY_FLAGS m_flags;
#endif
#endregion

#if FEATURE_APPX
        internal int InvocableAttributeCtorToken
        {
            get
            {
                int token = (int)(Flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_TOKEN_MASK);

                return token | (int)MetadataTokenType.MethodDef;
            }
        }

        private ASSEMBLY_FLAGS Flags
        {
            [SecuritySafeCritical]
            get
            {
                if ((m_flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED) == 0)
                {
                    ASSEMBLY_FLAGS flags = ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN;

#if FEATURE_CORECLR
                    flags |= ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
#else
                    if (RuntimeAssembly.IsFrameworkAssembly(GetName()))
                    {
                        flags |= ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;

                        foreach (string name in s_unsafeFrameworkAssemblyNames)
                        {
                            if (String.Compare(GetSimpleName(), name, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                flags &= ~ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
                                break;
                            }
                        }

                        // Each blessed API will be annotated with a "__DynamicallyInvokableAttribute".
                        // This "__DynamicallyInvokableAttribute" is a type defined in its own assembly.
                        // So the ctor is always a MethodDef and the type a TypeDef.
                        // We cache this ctor MethodDef token for faster custom attribute lookup.
                        // If this attribute type doesn't exist in the assembly, it means the assembly
                        // doesn't contain any blessed APIs.
                        Type invocableAttribute = GetType("__DynamicallyInvokableAttribute", false);
                        if (invocableAttribute != null)
                        {
                            Contract.Assert(((MetadataToken)invocableAttribute.MetadataToken).IsTypeDef);

                            ConstructorInfo ctor = invocableAttribute.GetConstructor(Type.EmptyTypes);
                            Contract.Assert(ctor != null);

                            int token = ctor.MetadataToken;
                            Contract.Assert(((MetadataToken)token).IsMethodDef);

                            flags |= (ASSEMBLY_FLAGS)token & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_TOKEN_MASK;
                        }
                    }
                    else if (IsDesignerBindingContext())
                    {
                        flags = ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
                    }
#endif

                    m_flags = flags | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED;
                }

                return m_flags;
            }
        }
#endif // FEATURE_CORECLR

        internal object SyncRoot
        {
            get
            {
                if (m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref m_syncRoot, new object(), null);
                }
                return m_syncRoot;
            }
        }
        
        public override event ModuleResolveEventHandler ModuleResolve
        {
            [System.Security.SecurityCritical]  // auto-generated_required
            add
            {
                _ModuleResolve += value;
            }
            [System.Security.SecurityCritical]  // auto-generated_required
            remove
            {
                _ModuleResolve -= value;
            }
        }

        private const String s_localFilePrefix = "file:";

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetCodeBase(RuntimeAssembly assembly, 
                                               bool copiedName, 
                                               StringHandleOnStack retString);

        [System.Security.SecurityCritical]  // auto-generated
        internal String GetCodeBase(bool copiedName)
        {
            String codeBase = null;
            GetCodeBase(GetNativeHandle(), copiedName, JitHelpers.GetStringHandleOnStack(ref codeBase));
            return codeBase;
        }

        public override String CodeBase
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#else
            [System.Security.SecuritySafeCritical]
#endif
            get {
                String codeBase = GetCodeBase(false);
                VerifyCodeBaseDiscovery(codeBase);
                return codeBase;
            }
        }

        internal RuntimeAssembly GetNativeHandle()
        {
            return this;
        }

        // If the assembly is copied before it is loaded, the codebase will be set to the
        // actual file loaded if copiedName is true. If it is false, then the original code base
        // is returned.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public override AssemblyName GetName(bool copiedName)
        {
            AssemblyName an = new AssemblyName();

            String codeBase = GetCodeBase(copiedName);
            VerifyCodeBaseDiscovery(codeBase);

            an.Init(GetSimpleName(), 
                    GetPublicKey(),
                    null, // public key token
                    GetVersion(),
                    GetLocale(),
                    GetHashAlgorithm(),
                    AssemblyVersionCompatibility.SameMachine,
                    codeBase,
                    GetFlags() | AssemblyNameFlags.PublicKey,
                    null); // strong name key pair

            PortableExecutableKinds pek;
            ImageFileMachine ifm;
        
            Module manifestModule = ManifestModule;
            if (manifestModule != null)
            {
                if (manifestModule.MDStreamVersion > 0x10000)
                {
                    ManifestModule.GetPEKind(out pek, out ifm);
                    an.SetProcArchIndex(pek,ifm);
                }
            }
            return an;
        }

#if FEATURE_APTCA
        // This method is called from the VM when creating conditional APTCA exceptions, in order to include
        // the text which must be added to the partial trust visible assembly list
        [SecurityCritical]
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private string GetNameForConditionalAptca()
        {
            AssemblyName assemblyName = GetName();
            return assemblyName.GetNameWithPublicKey();

        }
#endif // FEATURE_APTCA

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetFullName(RuntimeAssembly assembly, StringHandleOnStack retString);

        public override String FullName
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // If called by Object.ToString(), return val may be NULL.
                if (m_fullname == null)
                {
                    string s = null;
                    GetFullName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                    Interlocked.CompareExchange<string>(ref m_fullname, s, null);
                }

                return m_fullname;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetEntryPoint(RuntimeAssembly assembly, ObjectHandleOnStack retMethod);
           
        public override MethodInfo EntryPoint
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                IRuntimeMethodInfo methodHandle = null;
                GetEntryPoint(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref methodHandle));

                if (methodHandle == null)
                    return null;

                    return (MethodInfo)RuntimeType.GetMethodBase(methodHandle);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetType(RuntimeAssembly assembly, 
                                                        String name, 
                                                        bool throwOnError, 
                                                        bool ignoreCase,
                                                        ObjectHandleOnStack type,
                                                        ObjectHandleOnStack keepAlive);
        
        [System.Security.SecuritySafeCritical]
        public override Type GetType(String name, bool throwOnError, bool ignoreCase) 
        {
            // throw on null strings regardless of the value of "throwOnError"
            if (name == null)
                throw new ArgumentNullException("name");

            RuntimeType type = null;
            Object keepAlive = null;
            GetType(GetNativeHandle(), name, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack(ref type), JitHelpers.GetObjectHandleOnStack(ref keepAlive));
            GC.KeepAlive(keepAlive);
            
            return type;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal extern static void GetForwardedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetExportedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes); 
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type[] GetExportedTypes()
        {
            Type[] types = null;
            GetExportedTypes(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));
            return types;
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                List<RuntimeType> rtTypes = new List<RuntimeType>();

                RuntimeModule[] modules = GetModulesInternal(true, false);

                for (int i = 0; i < modules.Length; i++)
                {
                    rtTypes.AddRange(modules[i].GetDefinedTypes());
                }

                return rtTypes.ToArray();
            }
        }

        // Load a resource based on the NameSpace of the type.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Stream GetManifestResourceStream(Type type, String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(type, name, false, ref stackMark);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override Stream GetManifestResourceStream(String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(name, ref stackMark, false);
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetEvidence(RuntimeAssembly assembly, ObjectHandleOnStack retEvidence);

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static SecurityRuleSet GetSecurityRuleSet(RuntimeAssembly assembly);

        public override Evidence Evidence
        {
            [SecuritySafeCritical]
            [SecurityPermissionAttribute( SecurityAction.Demand, ControlEvidence = true )]
            get
            {
                Evidence evidence = EvidenceNoDemand;
                return evidence.Clone();
            }           
        }

        internal Evidence EvidenceNoDemand
        {
            [SecurityCritical]
            get
            {
                Evidence evidence = null;
                GetEvidence(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref evidence));
                return evidence;
            }
        }

        public override PermissionSet PermissionSet
        {
            [SecurityCritical]
            get
            {
                PermissionSet grantSet = null;
                PermissionSet deniedSet = null;

                GetGrantSet(out grantSet, out deniedSet);

                if (grantSet != null)
                {
                    return grantSet.Copy();
                }
                else
                {
                    return new PermissionSet(PermissionState.Unrestricted);
                }
            }
        }

        public override SecurityRuleSet SecurityRuleSet
        {
            [SecuritySafeCritical]
            get
            {
                return GetSecurityRuleSet(GetNativeHandle());
            }
        }
#endif // FEATURE_CAS_POLICY

        // ISerializable implementation
        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info==null)
                throw new ArgumentNullException("info");

            Contract.EndContractBlock();

            UnitySerializationHolder.GetUnitySerializationInfo(info,
                                                               UnitySerializationHolder.AssemblyUnity, 
                                                               this.FullName, 
                                                               this);
        }

        public override Module ManifestModule
        {
            get
            {
                // We don't need to return the "external" ModuleBuilder because
                // it is meant to be read-only
                return RuntimeAssembly.GetManifestModule(GetNativeHandle());
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }
                    
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"caType");

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static RuntimeAssembly InternalLoadFrom(String assemblyFile, 
                                                         Evidence securityEvidence,
                                                         byte[] hashValue, 
                                                         AssemblyHashAlgorithm hashAlgorithm,
                                                         bool forIntrospection,
                                                         bool suppressSecurityChecks,
                                                         ref StackCrawlMark stackMark)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException("assemblyFile");

            Contract.EndContractBlock();

#if FEATURE_CAS_POLICY
            if (securityEvidence != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY
            AssemblyName an = new AssemblyName();
            an.CodeBase = assemblyFile;
            an.SetHashControl(hashValue, hashAlgorithm);
            // The stack mark is used for MDA filtering
            return InternalLoadAssemblyName(an, securityEvidence, null, ref stackMark, true /*thrownOnFileNotFound*/, forIntrospection, suppressSecurityChecks);
        }

        // Wrapper function to wrap the typical use of InternalLoad.
        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly InternalLoad(String assemblyString,
                                                     Evidence assemblySecurity,
                                                     ref StackCrawlMark stackMark,
                                                     bool forIntrospection)
        {
            return InternalLoad(assemblyString, assemblySecurity,  ref stackMark, IntPtr.Zero, forIntrospection);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static RuntimeAssembly InternalLoad(String assemblyString,
                                                     Evidence assemblySecurity,
                                                     ref StackCrawlMark stackMark,
                                                     IntPtr pPrivHostBinder,
                                                     bool forIntrospection)
        {
            RuntimeAssembly assembly;
            AssemblyName an = CreateAssemblyName(assemblyString, forIntrospection, out assembly);

            if (assembly != null) {
                // The assembly was returned from ResolveAssemblyEvent
                return assembly;
            }

            return InternalLoadAssemblyName(an, assemblySecurity, null, ref stackMark, 
                                            pPrivHostBinder,
                                            true  /*thrownOnFileNotFound*/, forIntrospection, false /* suppressSecurityChecks */);
        }
        
        // Creates AssemblyName. Fills assembly if AssemblyResolve event has been raised.
        [System.Security.SecurityCritical]  // auto-generated
        internal static AssemblyName CreateAssemblyName(
            String assemblyString, 
            bool forIntrospection, 
            out RuntimeAssembly assemblyFromResolveEvent)
        {
            if (assemblyString == null)
                throw new ArgumentNullException("assemblyString");
            Contract.EndContractBlock();

            if ((assemblyString.Length == 0) ||
                (assemblyString[0] == '\0'))
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));

            if (forIntrospection)
                AppDomain.CheckReflectionOnlyLoadSupported();

            AssemblyName an = new AssemblyName();

            an.Name = assemblyString;
            an.nInit(out assemblyFromResolveEvent, forIntrospection, true);
            
            return an;
        }
        
        // Wrapper function to wrap the typical use of InternalLoadAssemblyName.
        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly InternalLoadAssemblyName(
            AssemblyName assemblyRef,
            Evidence assemblySecurity,
            RuntimeAssembly reqAssembly,
            ref StackCrawlMark stackMark,
            bool throwOnFileNotFound,
            bool forIntrospection,
            bool suppressSecurityChecks,
            IntPtr ptrLoadContextBinder = default(IntPtr))
        {
            return InternalLoadAssemblyName(assemblyRef, assemblySecurity, reqAssembly, ref stackMark, IntPtr.Zero, true /*throwOnError*/, forIntrospection, suppressSecurityChecks, ptrLoadContextBinder);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly InternalLoadAssemblyName(
            AssemblyName assemblyRef, 
            Evidence assemblySecurity,
            RuntimeAssembly reqAssembly,
            ref StackCrawlMark stackMark,
            IntPtr pPrivHostBinder,
            bool throwOnFileNotFound, 
            bool forIntrospection,
            bool suppressSecurityChecks,
            IntPtr ptrLoadContextBinder = default(IntPtr))
        {
       
            if (assemblyRef == null)
                throw new ArgumentNullException("assemblyRef");
            Contract.EndContractBlock();

            if (assemblyRef.CodeBase != null)
            {
                AppDomain.CheckLoadFromSupported();
            }

            assemblyRef = (AssemblyName)assemblyRef.Clone();
#if FEATURE_VERSIONING
            if (!forIntrospection &&
                (assemblyRef.ProcessorArchitecture != ProcessorArchitecture.None)) {
                // PA does not have a semantics for by-name binds for execution
                assemblyRef.ProcessorArchitecture = ProcessorArchitecture.None;
            }
#endif

            if (assemblySecurity != null)
            {
#if FEATURE_CAS_POLICY
                if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
                }
#endif // FEATURE_CAS_POLICY

                if (!suppressSecurityChecks)
                {
#pragma warning disable 618
                    new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
#pragma warning restore 618
                }
            }


            String codeBase = VerifyCodeBase(assemblyRef.CodeBase);
            if (codeBase != null && !suppressSecurityChecks) {
                
                if (String.Compare( codeBase, 0, s_localFilePrefix, 0, 5, StringComparison.OrdinalIgnoreCase) != 0) {
#if FEATURE_FUSION   // Of all the binders, Fusion is the only one that understands Web locations                 
                    IPermission perm = CreateWebPermission( assemblyRef.EscapedCodeBase );
                    perm.Demand();
#else
                     throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileName"), "assemblyRef.CodeBase");
#endif
                }
                else {
                    System.Security.Util.URLString urlString = new System.Security.Util.URLString( codeBase, true );
                    new FileIOPermission( FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read , urlString.GetFileName() ).Demand();
                }   
            }

            return nLoad(assemblyRef, codeBase, assemblySecurity, reqAssembly, ref stackMark,
                pPrivHostBinder,
                throwOnFileNotFound, forIntrospection, suppressSecurityChecks, ptrLoadContextBinder);
        }

        // These are the framework assemblies that does reflection invocation
        // on behalf of user code. We allow framework code to invoke non-W8P
        // framework APIs but don't want user code to gain that privilege 
        // through these assemblies. So we blaklist them.
        static string[] s_unsafeFrameworkAssemblyNames = new string[] {
            "System.Reflection.Context",
            "Microsoft.VisualBasic"
        };

#if FEATURE_APPX
        [System.Security.SecuritySafeCritical]
        internal bool IsFrameworkAssembly()
        {
            ASSEMBLY_FLAGS flags = Flags;
            return (flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK) != 0;
        }

        // Returns true if we want to allow this assembly to invoke non-W8P
        // framework APIs through reflection.
        internal bool IsSafeForReflection()
        {
            ASSEMBLY_FLAGS flags = Flags;
            return (flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION) != 0;
        }

        [System.Security.SecuritySafeCritical]
        private bool IsDesignerBindingContext()
        {
            return RuntimeAssembly.nIsDesignerBindingContext(this);
        }

        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static bool nIsDesignerBindingContext(RuntimeAssembly assembly);
#endif

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern RuntimeAssembly _nLoad(AssemblyName fileName,
                                                     String codeBase,
                                                     Evidence assemblySecurity,
                                                     RuntimeAssembly locationHint,
                                                     ref StackCrawlMark stackMark,                                              
                                                     IntPtr pPrivHostBinder,
                                                     bool throwOnFileNotFound,        
                                                     bool forIntrospection,
                                                     bool suppressSecurityChecks,
                                                     IntPtr ptrLoadContextBinder);

#if !FEATURE_CORECLR
        // The NGEN task uses this method, so please do not modify its signature
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsFrameworkAssembly(AssemblyName assemblyName);

        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsNewPortableAssembly(AssemblyName assemblyName);
#endif

        [System.Security.SecurityCritical]  // auto-generated
        private static RuntimeAssembly nLoad(AssemblyName fileName,
                                             String codeBase,
                                             Evidence assemblySecurity,
                                             RuntimeAssembly locationHint,
                                             ref StackCrawlMark stackMark,
                                             IntPtr pPrivHostBinder,
                                             bool throwOnFileNotFound,
                                             bool forIntrospection,
                                             bool suppressSecurityChecks, IntPtr ptrLoadContextBinder = default(IntPtr))
        {
            return _nLoad(fileName, codeBase, assemblySecurity, locationHint, ref stackMark,
                pPrivHostBinder,
                throwOnFileNotFound, forIntrospection, suppressSecurityChecks, ptrLoadContextBinder);
        }

#if FEATURE_FUSION
        // used by vm
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private static unsafe RuntimeAssembly LoadWithPartialNameHack(String partialName, bool cropPublicKey)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
        
            AssemblyName an = new AssemblyName(partialName);
        
            if (!IsSimplyNamed(an))
            {
                if (cropPublicKey)
                {
                    an.SetPublicKey(null);
                    an.SetPublicKeyToken(null);
                }
                
                if(IsFrameworkAssembly(an) || !AppDomain.IsAppXModel())
                {
                    AssemblyName GACAssembly = EnumerateCache(an);
                    if(GACAssembly != null)
                        return InternalLoadAssemblyName(GACAssembly, null, null,ref stackMark, true /*thrownOnFileNotFound*/, false, false);
                    else
                        return null;
                }
            }

            if (AppDomain.IsAppXModel())
            {
                // also try versionless bind from the package
                an.Version = null;
                return nLoad(an, null, null, null, ref stackMark, 
                       IntPtr.Zero,
                       false, false, false);
            }
            return null;
            
        }        

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly LoadWithPartialNameInternal(String partialName, Evidence securityEvidence, ref StackCrawlMark stackMark)
        {
            AssemblyName an = new AssemblyName(partialName);
            return LoadWithPartialNameInternal(an, securityEvidence, ref stackMark);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly LoadWithPartialNameInternal(AssemblyName an, Evidence securityEvidence, ref StackCrawlMark stackMark)
        {
            if (securityEvidence != null)
            {
#if FEATURE_CAS_POLICY
                if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
                }
#endif // FEATURE_CAS_POLICY
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            }
            
            AppDomain.CheckLoadWithPartialNameSupported(stackMark);

            RuntimeAssembly result = null;
            try {
                result = nLoad(an, null, securityEvidence, null, ref stackMark, 
                               IntPtr.Zero,
                               true, false, false);
            }
            catch(Exception e) {
                if (e.IsTransient)
                    throw e;

                if (IsUserError(e))
                    throw;


                if(IsFrameworkAssembly(an) || !AppDomain.IsAppXModel())
                {
                    if (IsSimplyNamed(an))
                        return null;

                    AssemblyName GACAssembly = EnumerateCache(an);
                    if(GACAssembly != null)
                        result = InternalLoadAssemblyName(GACAssembly, securityEvidence, null, ref stackMark, true /*thrownOnFileNotFound*/, false, false);
                }
                else
                {
                    an.Version = null;
                    result = nLoad(an, null, securityEvidence, null, ref stackMark, 
                                   IntPtr.Zero,
                                   false, false, false);
                }   
           }


            return result;
        }
#endif // !FEATURE_CORECLR

        [SecuritySafeCritical]
        private static bool IsUserError(Exception e)
        {
            return (uint)e.HResult == COR_E_LOADING_REFERENCE_ASSEMBLY;
        }

        private static bool IsSimplyNamed(AssemblyName partialName)
        {
            byte[] pk = partialName.GetPublicKeyToken();
            if ((pk != null) &&
                (pk.Length == 0))
                return true;

            pk = partialName.GetPublicKey();
            if ((pk != null) &&
                (pk.Length == 0))
                return true;

            return false;
        }        

        [System.Security.SecurityCritical]  // auto-generated
        private static AssemblyName EnumerateCache(AssemblyName partialName)
        {
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();

            partialName.Version = null;

            ArrayList a = new ArrayList();
            Fusion.ReadCache(a, partialName.FullName, ASM_CACHE.GAC);
            
            IEnumerator myEnum = a.GetEnumerator();
            AssemblyName ainfoBest = null;
            CultureInfo refCI = partialName.CultureInfo;

            while (myEnum.MoveNext()) {
                AssemblyName ainfo = new AssemblyName((String)myEnum.Current);

                if (CulturesEqual(refCI, ainfo.CultureInfo)) {
                    if (ainfoBest == null)
                        ainfoBest = ainfo;
                    else {
                        // Choose highest version
                        if (ainfo.Version > ainfoBest.Version)
                            ainfoBest = ainfo;
                    }
                }
            }

            return ainfoBest;
        }

        private static bool CulturesEqual(CultureInfo refCI, CultureInfo defCI)
        {
            bool defNoCulture = defCI.Equals(CultureInfo.InvariantCulture);

            // cultured asms aren't allowed to be bound to if
            // the ref doesn't ask for them specifically
            if ((refCI == null) || refCI.Equals(CultureInfo.InvariantCulture))
                return defNoCulture;

            if (defNoCulture || 
                ( !defCI.Equals(refCI) ))
                return false;

            return true;
        }
#endif // FEATURE_FUSION

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsReflectionOnly(RuntimeAssembly assembly);

        // To not break compatibility with the V1 _Assembly interface we need to make this
        // new member ComVisible(false).
        [ComVisible(false)]
        public override bool ReflectionOnly
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return IsReflectionOnly(GetNativeHandle());
            }
        }

#if FEATURE_CORECLR
        // Loads the assembly with a COFF based IMAGE containing
        // an emitted assembly. The assembly is loaded into the domain
        // of the caller. Currently is implemented only for  UnmanagedMemoryStream
        // (no derived classes since we are not calling Read())
        [System.Security.SecurityCritical] // auto-generated
        internal static RuntimeAssembly InternalLoadFromStream(Stream assemblyStream, Stream pdbStream, ref StackCrawlMark stackMark)
        {
            if (assemblyStream  == null)
                throw new ArgumentNullException("assemblyStream");

            if (assemblyStream.GetType()!=typeof(UnmanagedMemoryStream))
                throw new NotSupportedException();

            if (pdbStream!= null && pdbStream.GetType()!=typeof(UnmanagedMemoryStream))
                throw new NotSupportedException();

            AppDomain.CheckLoadFromSupported();

            UnmanagedMemoryStream umAssemblyStream = (UnmanagedMemoryStream)assemblyStream;
            UnmanagedMemoryStream umPdbStream = (UnmanagedMemoryStream)pdbStream;
            
            unsafe
            {
                byte* umAssemblyStreamBuffer=umAssemblyStream.PositionPointer;
                byte* umPdbStreamBuffer=(umPdbStream!=null)?umPdbStream.PositionPointer:null; 
                long assemblyDataLength = umAssemblyStream.Length-umAssemblyStream.Position;
                long pdbDataLength = (umPdbStream!=null)?(umPdbStream.Length-umPdbStream.Position):0;
                
                // use Seek() to benefit from boundary checking, the actual read is done using *StreamBuffer
                umAssemblyStream.Seek(assemblyDataLength,SeekOrigin.Current);
                
                if(umPdbStream != null)
                {
                    umPdbStream.Seek(pdbDataLength,SeekOrigin.Current);                  
                }
                
                BCLDebug.Assert(assemblyDataLength > 0L, "assemblyDataLength > 0L");
    
                RuntimeAssembly assembly = null;

                nLoadFromUnmanagedArray(false, 
                                                                 umAssemblyStreamBuffer, 
                                                                 (ulong)assemblyDataLength, 
                                                                 umPdbStreamBuffer,
                                                                 (ulong)pdbDataLength, 
                                                                 JitHelpers.GetStackCrawlMarkHandle(ref stackMark),
                                                                 JitHelpers.GetObjectHandleOnStack(ref assembly));

                return assembly;
            }
        }
#endif //FEATURE_CORECLR

#if FEATURE_MULTIMODULE_ASSEMBLIES
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void LoadModule(RuntimeAssembly assembly,
                                                      String moduleName,
                                                      byte[] rawModule, int cbModule,
                                                      byte[] rawSymbolStore, int cbSymbolStore,
                                                      ObjectHandleOnStack retModule);

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlEvidence = true)]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Module LoadModule(String moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            RuntimeModule retModule = null;
            LoadModule(
                GetNativeHandle(),
                moduleName,
                rawModule,
                (rawModule != null) ? rawModule.Length : 0,
                rawSymbolStore,
                (rawSymbolStore != null) ? rawSymbolStore.Length : 0,
                JitHelpers.GetObjectHandleOnStack(ref retModule));

            return retModule;
        }
#endif //FEATURE_MULTIMODULE_ASSEMBLIES

        // Returns the module in this assembly with name 'name'

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetModule(RuntimeAssembly assembly, String name, ObjectHandleOnStack retModule);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Module GetModule(String name)
        {
            Module retModule = null;
            GetModule(GetNativeHandle(), name, JitHelpers.GetObjectHandleOnStack(ref retModule));
            return retModule;
        }

        // Returns the file in the File table of the manifest that matches the
        // given name.  (Name should not include path.)
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public override FileStream GetFile(String name)
        {
            RuntimeModule m = (RuntimeModule)GetModule(name);
            if (m == null)
                return null;

            return new FileStream(m.GetFullyQualifiedName(),
                                  FileMode.Open,
                                  FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public override FileStream[] GetFiles(bool getResourceModules)
        {
            Module[] m = GetModules(getResourceModules);
            int iLength = m.Length;
            FileStream[] fs = new FileStream[iLength];

            for(int i = 0; i < iLength; i++)
                fs[i] = new FileStream(((RuntimeModule)m[i]).GetFullyQualifiedName(),
                                       FileMode.Open,
                                       FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);

            return fs;
        }


        // Returns the names of all the resources
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern String[] GetManifestResourceNames(RuntimeAssembly assembly);

        // Returns the names of all the resources
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String[] GetManifestResourceNames()
        {
            return GetManifestResourceNames(GetNativeHandle());
        }
    
            
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);

        [System.Security.SecurityCritical]  // auto-generated
        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }
        
        // Returns the names of all the resources
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override AssemblyName[] GetReferencedAssemblies()
        {
            return GetReferencedAssemblies(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern int GetManifestResourceInfo(RuntimeAssembly assembly,
                                                          String resourceName,
                                                          ObjectHandleOnStack assemblyRef,
                                                          StringHandleOnStack retFileName,
                                                          StackCrawlMarkHandle stackMark);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            RuntimeAssembly retAssembly = null;
            String fileName = null;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            int location = GetManifestResourceInfo(GetNativeHandle(), resourceName, 
                                                   JitHelpers.GetObjectHandleOnStack(ref retAssembly),
                                                   JitHelpers.GetStringHandleOnStack(ref fileName),
                                                   JitHelpers.GetStackCrawlMarkHandle(ref stackMark));

            if (location == -1)
                return null;

            return new ManifestResourceInfo(retAssembly, fileName,
                                                (ResourceLocation) location);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetLocation(RuntimeAssembly assembly, StringHandleOnStack retString);

        public override String Location
        {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#else
            [System.Security.SecuritySafeCritical]
#endif
            get {
                String location = null;

                GetLocation(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref location));

                if (location != null)
                    new FileIOPermission( FileIOPermissionAccess.PathDiscovery, location ).Demand();

                return location;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetImageRuntimeVersion(RuntimeAssembly assembly, StringHandleOnStack retString);

        // To not break compatibility with the V1 _Assembly interface we need to make this
        // new member ComVisible(false).
        [ComVisible(false)]
        public override String ImageRuntimeVersion
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get{
                String s = null;
                GetImageRuntimeVersion(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                return s;
            }
        }


        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static bool IsGlobalAssemblyCache(RuntimeAssembly assembly);

        public override bool GlobalAssemblyCache
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return IsGlobalAssemblyCache(GetNativeHandle());
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static Int64 GetHostContext(RuntimeAssembly assembly);

        public override Int64 HostContext
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return GetHostContext(GetNativeHandle());
            }
        }

        private static String VerifyCodeBase(String codebase)
        {
            if(codebase == null)
                return null;

            int len = codebase.Length;
            if (len == 0)
                return null;


            int j = codebase.IndexOf(':');
            // Check to see if the url has a prefix
            if( (j != -1) &&
                (j+2 < len) &&
                ((codebase[j+1] == '/') || (codebase[j+1] == '\\')) &&
                ((codebase[j+2] == '/') || (codebase[j+2] == '\\')) )
                return codebase;
#if !PLATFORM_UNIX
            else if ((len > 2) && (codebase[0] == '\\') && (codebase[1] == '\\'))
                return "file://" + codebase;
            else
                return "file:///" + Path.GetFullPathInternal( codebase );
#else
            else
                return "file://" + Path.GetFullPathInternal( codebase );
#endif // !PLATFORM_UNIX
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal Stream GetManifestResourceStream(
            Type type,
            String name,
            bool skipSecurityCheck,
            ref StackCrawlMark stackMark)
        {
            StringBuilder sb = new StringBuilder();
            if(type == null) {
                if (name == null)
                    throw new ArgumentNullException("type");
            }
            else {
                String nameSpace = type.Namespace;
                if(nameSpace != null) {
                    sb.Append(nameSpace);
                    if(name != null) 
                        sb.Append(Type.Delimiter);
                }
            }

            if(name != null)
                sb.Append(name);
    
            return GetManifestResourceStream(sb.ToString(), ref stackMark, skipSecurityCheck);
        }

#if FEATURE_CAS_POLICY
        internal bool IsStrongNameVerified
        {
            [System.Security.SecurityCritical]  // auto-generated
            get { return GetIsStrongNameVerified(GetNativeHandle()); }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern bool GetIsStrongNameVerified(RuntimeAssembly assembly);
#endif // FEATURE_CAS_POLICY

        // GetResource will return a pointer to the resources in memory.
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static unsafe extern byte* GetResource(RuntimeAssembly assembly,
                                                       String resourceName,
                                                       out ulong length,
                                                       StackCrawlMarkHandle stackMark,
                                                       bool skipSecurityCheck);

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe Stream GetManifestResourceStream(String name, ref StackCrawlMark stackMark, bool skipSecurityCheck)
        {
            ulong length = 0;
            byte* pbInMemoryResource = GetResource(GetNativeHandle(), name, out length, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), skipSecurityCheck);

            if (pbInMemoryResource != null) {
                //Console.WriteLine("Creating an unmanaged memory stream of length "+length);
                if (length > Int64.MaxValue)
                    throw new NotImplementedException(Environment.GetResourceString("NotImplemented_ResourcesLongerThan2^63"));

                return new UnmanagedMemoryStream(pbInMemoryResource, (long)length, (long)length, FileAccess.Read, true);
            }

            //Console.WriteLine("GetManifestResourceStream: Blob "+name+" not found...");
            return null;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetVersion(RuntimeAssembly assembly, 
                                              out int majVer, 
                                              out int minVer, 
                                              out int buildNum,
                                              out int revNum);

        [System.Security.SecurityCritical]  // auto-generated
        internal Version GetVersion()
        {
            int majorVer, minorVer, build, revision;
            GetVersion(GetNativeHandle(), out majorVer, out minorVer, out build, out revision);
            return new Version (majorVer, minorVer, build, revision);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetLocale(RuntimeAssembly assembly, StringHandleOnStack retString);

        [System.Security.SecurityCritical]  // auto-generated
        internal CultureInfo GetLocale()
        {
            String locale = null;

            GetLocale(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref locale));

            if (locale == null)
                return CultureInfo.InvariantCulture;

            return new CultureInfo(locale);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool FCallIsDynamic(RuntimeAssembly assembly);

        public override bool IsDynamic
        {
            [SecuritySafeCritical]
            get {
                return FCallIsDynamic(GetNativeHandle());
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void VerifyCodeBaseDiscovery(String codeBase)
        {
#if FEATURE_CAS_POLICY
            if (CodeAccessSecurityEngine.QuickCheckForAllDemands()) {
                return;
            }
#endif // FEATURE_CAS_POLICY

            if ((codeBase != null) &&
                (String.Compare( codeBase, 0, s_localFilePrefix, 0, 5, StringComparison.OrdinalIgnoreCase) == 0)) {
                System.Security.Util.URLString urlString = new System.Security.Util.URLString( codeBase, true );
                new FileIOPermission( FileIOPermissionAccess.PathDiscovery, urlString.GetFileName() ).Demand();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetSimpleName(RuntimeAssembly assembly, StringHandleOnStack retSimpleName);

        [SecuritySafeCritical]
        internal String GetSimpleName()
        {
            string name = null;
            GetSimpleName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static AssemblyHashAlgorithm GetHashAlgorithm(RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        private AssemblyHashAlgorithm GetHashAlgorithm()
        {
            return GetHashAlgorithm(GetNativeHandle());
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static AssemblyNameFlags GetFlags(RuntimeAssembly assembly);

        [System.Security.SecurityCritical]  // auto-generated
        private AssemblyNameFlags GetFlags()
        {
            return GetFlags(GetNativeHandle());
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetRawBytes(RuntimeAssembly assembly, ObjectHandleOnStack retRawBytes);

        // Get the raw bytes of the assembly
        [SecuritySafeCritical]
        internal byte[] GetRawBytes()
        {
            byte[] rawBytes = null;

            GetRawBytes(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref rawBytes));
            return rawBytes;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetPublicKey(RuntimeAssembly assembly, ObjectHandleOnStack retPublicKey);

        [System.Security.SecurityCritical]  // auto-generated
        internal byte[] GetPublicKey()
        {
            byte[] publicKey = null;
            GetPublicKey(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref publicKey));
            return publicKey;
        }

        [SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetGrantSet(RuntimeAssembly assembly, ObjectHandleOnStack granted, ObjectHandleOnStack denied);

        [SecurityCritical]
        internal void GetGrantSet(out PermissionSet newGrant, out PermissionSet newDenied)
        {
            PermissionSet granted = null, denied = null;
            GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref granted), JitHelpers.GetObjectHandleOnStack(ref denied));
            newGrant = granted; newDenied = denied;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsAllSecurityCritical(RuntimeAssembly assembly);

        // Is everything introduced by this assembly critical
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsAllSecurityCritical()
        {
            return IsAllSecurityCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsAllSecuritySafeCritical(RuntimeAssembly assembly);

        // Is everything introduced by this assembly safe critical
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsAllSecuritySafeCritical()
        {
            return IsAllSecuritySafeCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsAllPublicAreaSecuritySafeCritical(RuntimeAssembly assembly);

        // Is everything introduced by this assembly safe critical
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsAllPublicAreaSecuritySafeCritical()
        {
            return IsAllPublicAreaSecuritySafeCritical(GetNativeHandle());
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool IsAllSecurityTransparent(RuntimeAssembly assembly);

        // Is everything introduced by this assembly transparent
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsAllSecurityTransparent()
        {
            return IsAllSecurityTransparent(GetNativeHandle());
        }

#if FEATURE_FUSION
        // demandFlag:
        // 0 demand PathDiscovery permission only
        // 1 demand Read permission only
        // 2 demand both Read and PathDiscovery
        // 3 demand Web permission only
        [System.Security.SecurityCritical]  // auto-generated
        private static void DemandPermission(String codeBase, bool havePath,
                                             int demandFlag)
        {
            FileIOPermissionAccess access = FileIOPermissionAccess.PathDiscovery;
            switch(demandFlag) {

            case 0: // default
                break;
            case 1:
                access = FileIOPermissionAccess.Read;
                break;
            case 2:
                access = FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read;
                break;

            case 3:
                IPermission perm = CreateWebPermission(AssemblyName.EscapeCodeBase(codeBase));
                perm.Demand();
                return;
            }

            if (!havePath) {
                System.Security.Util.URLString urlString = new System.Security.Util.URLString( codeBase, true );
                codeBase = urlString.GetFileName();
            }

            codeBase = Path.GetFullPathInternal(codeBase);  // canonicalize

            new FileIOPermission(access, codeBase).Demand();
        }
#endif

#if FEATURE_FUSION
        private static IPermission CreateWebPermission( String codeBase )
        {
            Contract.Assert( codeBase != null, "Must pass in a valid CodeBase" );
            Assembly sys = Assembly.Load("System, Version=" + ThisAssembly.Version + ", Culture=neutral, PublicKeyToken=" + AssemblyRef.EcmaPublicKeyToken);

            Type type = sys.GetType("System.Net.NetworkAccess", true);

            IPermission retval = null;
            if (!type.IsEnum || !type.IsVisible)
                goto Exit;

            Object[] webArgs = new Object[2];
            webArgs[0] = (Enum) Enum.Parse(type, "Connect", true);
            if (webArgs[0] == null)
                goto Exit;

            webArgs[1] = codeBase;

            type = sys.GetType("System.Net.WebPermission", true);

            if (!type.IsVisible)
                goto Exit;

            retval = (IPermission) Activator.CreateInstance(type, webArgs);

        Exit:
            if (retval == null) {
                Contract.Assert( false, "Unable to create WebPermission" );
                throw new InvalidOperationException();
            }

            return retval;            
        }
#endif
        // This method is called by the VM.
        [System.Security.SecurityCritical]
        private RuntimeModule OnModuleResolveEvent(String moduleName)
        {
            ModuleResolveEventHandler moduleResolve = _ModuleResolve;
            if (moduleResolve == null)
                return null;

            Delegate[] ds = moduleResolve.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++) {
                RuntimeModule ret = (RuntimeModule)((ModuleResolveEventHandler) ds[i])(this, new ResolveEventArgs(moduleName,this));
                if (ret != null)
                    return ret;
            }

            return null;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable  
        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, null, ref stackMark);
        }

        // Useful for binding to a very specific version of a satellite assembly
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable  
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, version, ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable  
        internal Assembly InternalGetSatelliteAssembly(CultureInfo culture,
                                                       Version version,
                                                       ref StackCrawlMark stackMark)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
            Contract.EndContractBlock();


            String name = GetSimpleName() + ".resources";
            return InternalGetSatelliteAssembly(name, culture, version, true, ref stackMark);
        }

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UseRelativeBindForSatellites();
#endif

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable  
        internal RuntimeAssembly InternalGetSatelliteAssembly(String name,
                                                              CultureInfo culture,
                                                              Version version,
                                                              bool throwOnFileNotFound,
                                                              ref StackCrawlMark stackMark)
        {

            AssemblyName an = new AssemblyName();

            an.SetPublicKey(GetPublicKey());
            an.Flags = GetFlags() | AssemblyNameFlags.PublicKey;

            if (version == null)
                an.Version = GetVersion();
            else
                an.Version = version;

            an.CultureInfo = culture;
            an.Name = name;

            RuntimeAssembly retAssembly = null;

#if !FEATURE_CORECLR
            bool bIsAppXDevMode = AppDomain.IsAppXDesignMode();

            bool useRelativeBind = false; 
            if (CodeAccessSecurityEngine.QuickCheckForAllDemands())
            {
                if (IsFrameworkAssembly())
                    useRelativeBind = true;
                else
                    useRelativeBind = UseRelativeBindForSatellites();
            }


            if (bIsAppXDevMode || useRelativeBind)
            {
                if (GlobalAssemblyCache)
                {
                    // lookup in GAC
                    ArrayList a = new ArrayList();
                    bool bTryLoadAnyway = false;
                    try
                    {
                        Fusion.ReadCache(a, an.FullName, ASM_CACHE.GAC);
                    }
                    catch(Exception e)
                    {
                        if (e.IsTransient)
                            throw;

                        // We also catch any other exception types we haven't come across yet,
                        // not just UnauthorizedAccessException.

                        // We do not want this by itself to cause us to fail to load resources.

                        // On Classic, try the old unoptimized way, for full compatibility with 4.0.
                        // i.e. fall back to using nLoad.
                        if (!AppDomain.IsAppXModel())
                            bTryLoadAnyway = true;

                        // On AppX:
                        // Do not try nLoad since that would effectively allow Framework
                        // resource satellite assemblies to be placed in AppX packages.
                        // Instead, leave retAssembly == null. If we were called by the
                        // ResourceManager, this will usually result in falling back to
                        // the next culture in the resource fallback chain, possibly the
                        // neutral culture.

                        // Note: if throwOnFileNotFound is true, arbitrary
                        // exceptions will be absorbed here and
                        // FileNotFoundException will be thrown in their place.
                        // (See below: "throw new FileNotFoundException").
                    }
                    if (a.Count > 0 || bTryLoadAnyway)
                    {
                        // present in the GAC, load it from there
                        retAssembly = nLoad(an, null, null, this, ref stackMark, 
                                            IntPtr.Zero,
                                            throwOnFileNotFound, false, false);
                    }
                }
                else
                {
                    String codeBase = CodeBase;

                    if ((codeBase != null) &&
                        (String.Compare(codeBase, 0, s_localFilePrefix, 0, 5, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        retAssembly = InternalProbeForSatelliteAssemblyNextToParentAssembly(an,
                                                                                            name,
                                                                                            codeBase,
                                                                                            culture,          
                                                                                            throwOnFileNotFound,
                                                                                            bIsAppXDevMode /* useLoadFile */, // if bIsAppXDevMode is false, then useRelativeBind is true.
                                                                                            ref stackMark);
                        if (retAssembly != null && !IsSimplyNamed(an))
                        {
                            AssemblyName defName = retAssembly.GetName();
                            if (!AssemblyName.ReferenceMatchesDefinitionInternal(an,defName,false))
                                retAssembly = null;
                        }
                    }
                    else if (!bIsAppXDevMode)
                    {
                        retAssembly = nLoad(an, null, null, this, ref stackMark, 
                                            IntPtr.Zero,
                                            throwOnFileNotFound, false, false);
                    }
                }
            }
            else
#endif // !FEATURE_CORECLR
            {
                retAssembly = nLoad(an, null, null, this,  ref stackMark, 
                                    IntPtr.Zero,
                                    throwOnFileNotFound, false, false);
            }

            if (retAssembly == this || (retAssembly == null && throwOnFileNotFound))
            {
                throw new FileNotFoundException(String.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), an.Name));
            }

            return retAssembly;
        }

        // Helper method used by InternalGetSatelliteAssembly only. Not abstracted for use elsewhere.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable  
        private RuntimeAssembly InternalProbeForSatelliteAssemblyNextToParentAssembly(AssemblyName an,
                                                                                      String name,
                                                                                      String codeBase,
                                                                                      CultureInfo culture,
                                                                                      bool throwOnFileNotFound,
                                                                                      bool useLoadFile,
                                                                                      ref StackCrawlMark stackMark)
        {
            // if useLoadFile == false, we do LoadFrom binds

            RuntimeAssembly retAssembly = null;
            String location = null;

            if (useLoadFile)
                location = Location;
            
            FileNotFoundException dllNotFoundException = null;

            StringBuilder assemblyFile = new StringBuilder(useLoadFile ? location : codeBase,
                                                           0,
                                                           useLoadFile ? location.LastIndexOf('\\') + 1 : codeBase.LastIndexOf('/') + 1,
                                                           Path.MaxPath);
            assemblyFile.Append(an.CultureInfo.Name);
            assemblyFile.Append(useLoadFile ? '\\' : '/');
            assemblyFile.Append(name);
            assemblyFile.Append(".DLL");

            string fileNameOrCodeBase = assemblyFile.ToString();

            AssemblyName loadFromAsmName = null;

            if (useLoadFile == false)
            {
                loadFromAsmName = new AssemblyName();
                // set just the codebase - we want this to be a pure LoadFrom
                loadFromAsmName.CodeBase = fileNameOrCodeBase;
            }

            try
            {
                try
                {
                    retAssembly = useLoadFile ? nLoadFile(fileNameOrCodeBase, null) :
                                                nLoad(loadFromAsmName, fileNameOrCodeBase, null, this, ref stackMark,
                                                IntPtr.Zero,
                                                throwOnFileNotFound, false, false);
                }
                catch (FileNotFoundException)
                {
                    // Create our own exception since the one caught doesn't have a filename associated with it, making it less useful for debugging.
                    dllNotFoundException = new FileNotFoundException(String.Format(culture,
                                                                                   Environment.GetResourceString("IO.FileNotFound_FileName"),
                                                                                   fileNameOrCodeBase),
                                                                     fileNameOrCodeBase); // Save this exception so we can throw it if we also don't find the .EXE
                    retAssembly = null;
                }
            
                if (retAssembly == null)
                {
                    // LoadFile will always throw, but LoadFrom will only throw if throwOnFileNotFound is true.
                    // If an exception was thrown, we must have a dllNotFoundException ready for throwing later.
                    BCLDebug.Assert((useLoadFile == false && throwOnFileNotFound == false) || dllNotFoundException != null,
                                   "(useLoadFile == false && throwOnFileNotFound == false) || dllNotFoundException != null");
                
                    assemblyFile.Remove(assemblyFile.Length - 4, 4);
                    assemblyFile.Append(".EXE");
                    fileNameOrCodeBase = assemblyFile.ToString();
                    
                    if (useLoadFile == false)
                        loadFromAsmName.CodeBase = fileNameOrCodeBase;

                    try
                    {
                        retAssembly = useLoadFile ? nLoadFile(fileNameOrCodeBase, null) :
                                                    nLoad(loadFromAsmName, fileNameOrCodeBase,  null, this, ref stackMark,
                                                          IntPtr.Zero,
                                                          false /* do not throw on file not found */, false, false);
                            
                    }
                    catch (FileNotFoundException)
                    {
                        retAssembly = null;
                    }

                    // It would be messy to have a FileNotFoundException that reports both .DLL and .EXE not found.
                    // Using a .DLL extension for satellite assemblies is the more common scenario,
                    // so just throw that exception.
                    
                    // In classic (i.e. non-AppX) mode, if binder logging is turned on, there will be separate  logs for
                    // the .DLL and .EXE load attempts if the user is interested in digging deeper.
                    
                    if (retAssembly == null && throwOnFileNotFound)
                        throw dllNotFoundException;                   
                }
            }
            catch (DirectoryNotFoundException)
            {
                if (throwOnFileNotFound)
                    throw;
                retAssembly = null;
            }
            // No other exceptions should be caught here.

            return retAssembly;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern RuntimeAssembly nLoadFile(String path, Evidence evidence);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static internal extern RuntimeAssembly nLoadImage(byte[] rawAssembly,
                                                          byte[] rawSymbolStore,
                                                          Evidence evidence,
                                                          ref StackCrawlMark stackMark,
                                                          bool fIntrospection,
                                                          SecurityContextSource securityContextSource);
#if FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static internal extern unsafe void nLoadFromUnmanagedArray(bool fIntrospection, 
                                                                            byte* assemblyContent, 
                                                                            ulong assemblySize,
                                                                            byte* pdbContent, 
                                                                            ulong pdbSize,
                                                                            StackCrawlMarkHandle stackMark,
                                                                            ObjectHandleOnStack retAssembly);
#endif

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void GetModules(RuntimeAssembly assembly, 
                                              bool loadIfNotFound, 
                                              bool getResourceModules, 
                                              ObjectHandleOnStack retModuleHandles);
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        private RuntimeModule[] GetModulesInternal(bool loadIfNotFound,
                                     bool getResourceModules)
        {
            RuntimeModule[] modules = null;
            GetModules(GetNativeHandle(), loadIfNotFound, getResourceModules, JitHelpers.GetObjectHandleOnStack(ref modules));
            return modules;
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return GetModulesInternal(true, getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModulesInternal(false, getResourceModules);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);

#if FEATURE_APTCA
        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool AptcaCheck(RuntimeAssembly targetAssembly, RuntimeAssembly sourceAssembly);
#endif // FEATURE_APTCA

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int GetToken(RuntimeAssembly assembly);
    }
}
