// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias System_Security_Principal;

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace System
{
    using PrincipalPolicy = System_Security_Principal::System.Security.Principal.PrincipalPolicy;
    using IPrincipal = System_Security_Principal::System.Security.Principal.IPrincipal;

    public partial class AppDomain : MarshalByRefObject
    {
        private static readonly AppDomain s_domain = new AppDomain();
        private AppDomain() { }
        public static AppDomain CurrentDomain => s_domain;
        public string BaseDirectory => AppContext.BaseDirectory;
        public string RelativeSearchPath => null;
        public event UnhandledExceptionEventHandler UnhandledException
        {
            add
            {
                AppContext.UnhandledException += value;
            }

            remove
            {
                AppContext.UnhandledException -= value;
            }
        }

        public string DynamicDirectory => null;
        public string FriendlyName
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                string name = null;
                if (assembly == null)
                    name = "DefaultDomain";
                else
                    name = assembly.GetName().Name;
                return name;
            }
        }
        public int Id => 1;
        public bool IsFullyTrusted => true;
        public bool IsHomogenous => true;
        public event EventHandler DomainUnload { add { } remove { } }

        public event EventHandler<FirstChanceExceptionEventArgs> FirstChanceException
        {
            add
            {
                AppContext.FirstChanceException += value;
            }

            remove
            {
                AppContext.FirstChanceException -= value;
            }
        }

        public event EventHandler ProcessExit
        {
            add
            {
                AppContext.ProcessExit += value;
            }

            remove
            {
                AppContext.ProcessExit -= value;
            }
        }

        public string ApplyPolicy(string assemblyName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));
            if (assemblyName.Length == 0 || assemblyName[0] == '\0')
                throw new ArgumentException(SR.ZeroLengthString);
            return assemblyName;
        }

        public static AppDomain CreateDomain(string friendlyName)
        {
            if (friendlyName == null) throw new ArgumentNullException(nameof(friendlyName));
            throw new PlatformNotSupportedException();
        }

        public int ExecuteAssembly(string assemblyFile) => ExecuteAssembly(assemblyFile, null);
        public int ExecuteAssembly(string assemblyFile, string[] args)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException(nameof(assemblyFile));
            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            return ExecuteAssembly(assembly, args);
        }
        public int ExecuteAssembly(string assemblyFile, string[] args, byte[] hashValue, Configuration.Assemblies.AssemblyHashAlgorithm hashAlgorithm)
        {
            // This api is only meaningful for very specific partial trust/CAS hence not supporting
            throw new PlatformNotSupportedException();
        }
        private int ExecuteAssembly(Assembly assembly, string[] args)
        {
            MethodInfo entry = assembly.EntryPoint;
            if (entry == null)
                throw new MissingMethodException(SR.EntryPointNotFound + assembly.FullName);
            object result = null;
            int intResult = 0;
            try
            {
                if (entry.GetParameters().Length > 0)
                    result = entry.Invoke(null, new object[] { args });
                else
                    result = entry.Invoke(null, null);
            }
            catch (TargetInvocationException targetInvocationException)
            {
                if (targetInvocationException.InnerException == null)
                    throw;
                //We are catching the TIE here and throws the inner exception only,
                //this is needed to have a consistent exception story with desktop clr
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }
            if (result != null)
                intResult = (int)result;
            return intResult;
        }

        public int ExecuteAssemblyByName(AssemblyName assemblyName, params string[] args)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            return ExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(string assemblyName) => ExecuteAssemblyByName(assemblyName, null);
        public int ExecuteAssemblyByName(string assemblyName, params string[] args)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            return ExecuteAssembly(assembly, args);
        }

        public object GetData(string name) => AppContext.GetData(name);
        public void SetData(string name, object data) => AppContext.SetData(name, data);

        public bool? IsCompatibilitySwitchSet(string value)
        {
            bool result;
            if (AppContext.TryGetSwitch(value, out result))
                return result;
            else
                return default(bool?);
        }

        public bool IsDefaultAppDomain() => true;
        public bool IsFinalizingForUnload() => false;

        public override string ToString()
        {
            return SR.AppDomain_Name + FriendlyName + Environment.NewLine + SR.AppDomain_NoContextPolicies;
        }
        public static void Unload(AppDomain domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));
            throw new CannotUnloadAppDomainException(SR.NotSupported);
        }

        public Assembly Load(byte[] rawAssembly) => Assembly.Load(rawAssembly);
        public Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore) => Assembly.Load(rawAssembly, rawSymbolStore);
        public Assembly Load(AssemblyName assemblyRef) => Assembly.Load(assemblyRef);
        public Assembly Load(string assemblyString) => Assembly.Load(assemblyString);

        public Assembly[] ReflectionOnlyGetAssemblies() { return Array.Empty<Assembly>(); }

        public static bool MonitoringIsEnabled
        {
            get { return false; }
            set
            {
                if (value == false)
                {
                    throw new ArgumentException(SR.Arg_MustBeTrue);
                }
                throw new PlatformNotSupportedException();
            }
        }
        public long MonitoringSurvivedMemorySize { get { throw new InvalidOperationException(SR.AppDomain_ResMonNotAvail); } }
        public static long MonitoringSurvivedProcessMemorySize { get { throw new InvalidOperationException(SR.AppDomain_ResMonNotAvail); } }
        public long MonitoringTotalAllocatedMemorySize { get { throw new InvalidOperationException(SR.AppDomain_ResMonNotAvail); } }
        public TimeSpan MonitoringTotalProcessorTime { get { throw new InvalidOperationException(SR.AppDomain_ResMonNotAvail); } }
        public static int GetCurrentThreadId() => Environment.CurrentManagedThreadId;
        public bool ShadowCopyFiles => false;
        public void AppendPrivatePath(string path) { }
        public void ClearPrivatePath() { }
        public void ClearShadowCopyPath() { }
        public void SetCachePath(string path) { }
        public void SetShadowCopyFiles() { }
        public void SetShadowCopyPath(string path) { }
        public Assembly[] GetAssemblies() => AssemblyLoadContext.GetLoadedAssemblies();
        public event AssemblyLoadEventHandler AssemblyLoad
        {
            add { AssemblyLoadContext.AssemblyLoad += value; }
            remove { AssemblyLoadContext.AssemblyLoad -= value; }
        }
        public event ResolveEventHandler TypeResolve
        {
            add { AssemblyLoadContext.TypeResolve += value; }
            remove { AssemblyLoadContext.TypeResolve -= value; }
        }
        public event ResolveEventHandler ResourceResolve
        {
            add { AssemblyLoadContext.ResourceResolve += value; }
            remove { AssemblyLoadContext.ResourceResolve -= value; }
        }
        public void SetPrincipalPolicy(PrincipalPolicy policy) { }
        public void SetThreadPrincipal(IPrincipal principal)
        {
            if (principal == null) throw new ArgumentNullException("principal");
            lock (_forLock) {
                // Check that principal has not been set previously.
                if (_defaultPrincipal != null)
                    throw new SystemException(SR.AppDomain_Policy_PrincipalTwice);

                _defaultPrincipal = principal;
            }
        }
        // TODO
        public event ResolveEventHandler AssemblyResolve { add { } remove { } }

        private IPrincipal _defaultPrincipal;
        private readonly object _forLock = new Object();
    }
}
