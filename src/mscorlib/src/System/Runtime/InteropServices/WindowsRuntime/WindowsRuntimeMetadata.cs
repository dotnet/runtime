// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Runtime.InteropServices.WindowsRuntime
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    
    public static class WindowsRuntimeMetadata
    {
        // Wrapper for Win8 API RoResolveNamespace with default Windows SDK path as installed .winmd files in %WINDIR%\system32\WinMetadata.
        [System.Security.SecurityCritical]
        public static IEnumerable<string> ResolveNamespace(string namespaceName, IEnumerable<string> packageGraphFilePaths)
        {
            return ResolveNamespace(namespaceName, null, packageGraphFilePaths);
        }

        // Wrapper for Win8 API RoResolveNamespace.
        [System.Security.SecurityCritical]
        public static IEnumerable<string> ResolveNamespace(string namespaceName, string windowsSdkFilePath, IEnumerable<string> packageGraphFilePaths)
        {
            if (namespaceName == null)
                throw new ArgumentNullException("namespaceName");
            Contract.EndContractBlock();

            string[] packageGraphFilePathsArray = null;
            if (packageGraphFilePaths != null)
            {
                List<string> packageGraphFilePathsList = new List<string>(packageGraphFilePaths);
                packageGraphFilePathsArray = new string[packageGraphFilePathsList.Count];
                
                int index = 0;
                foreach (string packageGraphFilePath in packageGraphFilePathsList)
                {
                    packageGraphFilePathsArray[index] = packageGraphFilePath;
                    index++;
                }
            }
            
            string[] retFileNames = null;
            nResolveNamespace(
                namespaceName, 
                windowsSdkFilePath, 
                packageGraphFilePathsArray,
                ((packageGraphFilePathsArray == null) ? 0 : packageGraphFilePathsArray.Length), 
                JitHelpers.GetObjectHandleOnStack(ref retFileNames));
            
            return retFileNames;
        }
        
        [System.Security.SecurityCritical]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private extern static void nResolveNamespace(
            string namespaceName, 
            string windowsSdkFilePath, 
            string[] packageGraphFilePaths, 
            int cPackageGraphFilePaths, 
            ObjectHandleOnStack retFileNames);
        
#if FEATURE_REFLECTION_ONLY_LOAD
        [method: System.Security.SecurityCritical]
        public static event EventHandler<NamespaceResolveEventArgs> ReflectionOnlyNamespaceResolve;

        internal static RuntimeAssembly[] OnReflectionOnlyNamespaceResolveEvent(AppDomain appDomain, RuntimeAssembly assembly, string namespaceName)
        {
            EventHandler<NamespaceResolveEventArgs> eventHandler = ReflectionOnlyNamespaceResolve;
            if (eventHandler != null)
            {
                Delegate[] ds = eventHandler.GetInvocationList();
                int len = ds.Length;
                for (int i = 0; i < len; i++)
                {
                    NamespaceResolveEventArgs eventArgs = new NamespaceResolveEventArgs(namespaceName, assembly);
                    
                    ((EventHandler<NamespaceResolveEventArgs>)ds[i])(appDomain, eventArgs);
                    
                    Collection<Assembly> assembliesCollection = eventArgs.ResolvedAssemblies;
                    if (assembliesCollection.Count > 0)
                    {
                        RuntimeAssembly[] retAssemblies = new RuntimeAssembly[assembliesCollection.Count];
                        int retIndex = 0;
                        foreach (Assembly asm in assembliesCollection)
                        {
                            retAssemblies[retIndex] = AppDomain.GetRuntimeAssembly(asm);
                            retIndex++;
                        }
                        return retAssemblies;
                    }
                }
            }
            
            return null;
        }
#endif //FEATURE_REFLECTION_ONLY

        [method: System.Security.SecurityCritical]
        public static event EventHandler<DesignerNamespaceResolveEventArgs> DesignerNamespaceResolve;

        internal static string[] OnDesignerNamespaceResolveEvent(AppDomain appDomain, string namespaceName)
        {
            EventHandler<DesignerNamespaceResolveEventArgs> eventHandler = DesignerNamespaceResolve;
            if (eventHandler != null)
            {
                Delegate[] ds = eventHandler.GetInvocationList();
                int len = ds.Length;
                for (int i = 0; i < len; i++)
                {
                    DesignerNamespaceResolveEventArgs eventArgs = new DesignerNamespaceResolveEventArgs(namespaceName);

                    ((EventHandler<DesignerNamespaceResolveEventArgs>)ds[i])(appDomain, eventArgs);

                    Collection<string> assemblyFilesCollection = eventArgs.ResolvedAssemblyFiles;
                    if (assemblyFilesCollection.Count > 0)
                    {
                        string[] retAssemblyFiles = new string[assemblyFilesCollection.Count];
                        int retIndex = 0;
                        foreach (string assemblyFile in assemblyFilesCollection)
                        {
                            if (String.IsNullOrEmpty(assemblyFile))
                            {   // DesignerNamespaceResolve event returned null or empty file name - that is not allowed
                                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullString"), "DesignerNamespaceResolveEventArgs.ResolvedAssemblyFiles");
                            }
                            retAssemblyFiles[retIndex] = assemblyFile;
                            retIndex++;
                        }

                        return retAssemblyFiles;
                    }
                }
            }
            
            return null;
        }
    }
    
#if FEATURE_REFLECTION_ONLY_LOAD
    [ComVisible(false)]
    public class NamespaceResolveEventArgs : EventArgs
    {
        private string _NamespaceName;
        private Assembly _RequestingAssembly;
        private Collection<Assembly> _ResolvedAssemblies;

        public string NamespaceName
        {
            get
            {
                return _NamespaceName;
            }
        }

        public Assembly RequestingAssembly
        {
            get
            {
                return _RequestingAssembly;
            }
        }

        public Collection<Assembly> ResolvedAssemblies
        {
            get
            {
                return _ResolvedAssemblies;
            }
        }
        
        public NamespaceResolveEventArgs(string namespaceName, Assembly requestingAssembly)
        {
            _NamespaceName = namespaceName;
            _RequestingAssembly = requestingAssembly;
            _ResolvedAssemblies = new Collection<Assembly>();
        }
    }
#endif //FEATURE_REFLECTION_ONLY

    [ComVisible(false)]
    public class DesignerNamespaceResolveEventArgs : EventArgs
    {
        private string _NamespaceName;
        private Collection<string> _ResolvedAssemblyFiles;

        public string NamespaceName
        {
            get
            {
                return _NamespaceName;
            }
        }

        public Collection<string> ResolvedAssemblyFiles
        {
            get
            {
                return _ResolvedAssemblyFiles;
            }
        }

        public DesignerNamespaceResolveEventArgs(string namespaceName)
        {
            _NamespaceName = namespaceName;
            _ResolvedAssemblyFiles = new Collection<string>();
        }
    }
}
