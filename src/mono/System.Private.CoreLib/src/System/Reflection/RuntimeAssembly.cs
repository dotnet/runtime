// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace System.Reflection
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimeAssembly : Assembly
    {
        private enum AssemblyInfoKind
        {
            Location = 1,
            CodeBase = 2,
            FullName = 3,
            ImageRuntimeVersion = 4
        }

        private sealed class ResolveEventHolder
        {
            public event ModuleResolveEventHandler? ModuleResolve;
        }

        private sealed class UnmanagedMemoryStreamForModule : UnmanagedMemoryStream
        {
            private Module module;

            public unsafe UnmanagedMemoryStreamForModule(byte* pointer, long length, Module module)
                : base(pointer, length)
            {
                this.module = module;
            }
        }

        //
        // KEEP IN SYNC WITH _MonoReflectionAssembly in /mono/mono/metadata/object-internals.h
        // and AssemblyBuilder.cs.
        //
        #region VM dependency
        private IntPtr _mono_assembly;
        private LoaderAllocator? m_keepalive;
        #endregion

        internal IntPtr GetUnderlyingNativeHandle() { return _mono_assembly; }

        private ResolveEventHolder? resolve_event_holder;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetEntryPoint(QCallAssembly assembly, ObjectHandleOnStack res);

        public override MethodInfo? EntryPoint
        {
            get
            {
                var this_assembly = this;
                MethodInfo? res = null;
                GetEntryPoint(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref res));
                return res;
            }
        }

        public override bool ReflectionOnly => false;

        [Obsolete("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override string? CodeBase => GetInfo(AssemblyInfoKind.CodeBase);

        public override string? FullName => GetInfo(AssemblyInfoKind.FullName);

        //
        // We can't store the event directly in this class, since the
        // compiler would silently insert the fields before _mono_assembly
        //
        public override event ModuleResolveEventHandler? ModuleResolve
        {
            add
            {
                resolve_event_holder!.ModuleResolve += value;
            }
            remove
            {
                resolve_event_holder!.ModuleResolve -= value;
            }
        }

        public override Module ManifestModule
        {
            get
            {
                var this_assembly = this;
                Module? res = null;
                GetManifestModuleInternal(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref res));
                return res!;
            }
        }

        [Obsolete(Obsoletions.GlobalAssemblyCacheMessage, DiagnosticId = Obsoletions.GlobalAssemblyCacheDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override bool GlobalAssemblyCache => false;

        public override long HostContext => 0;

        public override string ImageRuntimeVersion => GetInfo(AssemblyInfoKind.ImageRuntimeVersion)!;

        public override string Location => GetInfo(AssemblyInfoKind.Location)!;

        // TODO: consider a dedicated icall instead
        public override bool IsCollectible => AssemblyLoadContext.GetLoadContext((Assembly)this)!.IsCollectible;

        internal static AssemblyName? CreateAssemblyName(string assemblyString, out RuntimeAssembly? assemblyFromResolveEvent)
        {
            ArgumentNullException.ThrowIfNull(assemblyString);

            if ((assemblyString.Length == 0) ||
                (assemblyString[0] == '\0'))
                throw new ArgumentException(SR.Format_StringZeroLength);

            assemblyFromResolveEvent = null;
            try
            {
                return new AssemblyName(assemblyString);
            }
            catch (Exception)
            {
                assemblyFromResolveEvent = (RuntimeAssembly?)AssemblyLoadContext.DoAssemblyResolve(assemblyString);
                if (assemblyFromResolveEvent == null)
                    throw new FileLoadException(assemblyString);
                return null;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetManifestResourceNames(QCallAssembly assembly_h, ObjectHandleOnStack res);

        public override string[] GetManifestResourceNames()
        {
            var this_assembly = this;
            string[]? res = null;
            GetManifestResourceNames(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetExportedTypes(QCallAssembly assembly_h, ObjectHandleOnStack res);

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetExportedTypes()
        {
            var this_assembly = this;
            Type[]? res = null;
            GetExportedTypes(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetTopLevelForwardedTypes(QCallAssembly assembly_h, ObjectHandleOnStack res);

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetForwardedTypes()
        {
            var this_assembly = this;
            Type[]? topLevelTypes = null;
            GetTopLevelForwardedTypes(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref topLevelTypes));
            List<Type> forwardedTypes = new List<Type>(topLevelTypes!);
            List<Exception> exceptions = new List<Exception>();

            foreach (Type t in topLevelTypes!)
                AddPublicNestedTypes(t, forwardedTypes, exceptions);

            if (exceptions.Count > 0)
            {
                forwardedTypes.AddRange(new Type[exceptions.Count]); // add one null Type for each exception
                exceptions.InsertRange(0, new Exception[forwardedTypes.Count]); // align the Exceptions with the null Types
                throw new ReflectionTypeLoadException(forwardedTypes.ToArray(), exceptions.ToArray());
            }

            return forwardedTypes.ToArray();
        }

        [RequiresUnreferencedCode("Types might be removed")]
        private static void AddPublicNestedTypes(Type type, List<Type> types, List<Exception> exceptions)
        {
            Type[] nestedTypes;

            try
            {
                nestedTypes = type.GetNestedTypes(BindingFlags.Public);
            }
            catch (FileLoadException e) { exceptions.Add(e); return; }
            catch (FileNotFoundException e) { exceptions.Add(e); return; }
            catch (TypeLoadException e) { exceptions.Add(e); return; }
            catch (IOException e) { exceptions.Add(e); return; }
            catch (UnauthorizedAccessException e) { exceptions.Add(e); return; }

            foreach (Type nestedType in nestedTypes)
            {
                types.Add(nestedType);
                AddPublicNestedTypes(nestedType, types, exceptions);
            }
        }

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            ArgumentException.ThrowIfNullOrEmpty(resourceName);

            ManifestResourceInfo result = new ManifestResourceInfo(null, null, 0);
            var this_assembly = this;
            bool found = GetManifestResourceInfoInternal(new QCallAssembly(ref this_assembly), resourceName, result);
            if (found)
                return result;
            else
                return null;
        }

        public override Stream? GetManifestResourceStream(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            unsafe
            {
                int length;
                Module? resourceModule = null;
                RuntimeAssembly? assembly = this;
                byte* data = (byte*)GetManifestResourceInternal(new QCallAssembly(ref assembly), name, out length, ObjectHandleOnStack.Create(ref resourceModule));
                if (data == null) {
                    assembly = AssemblyLoadContext.OnResourceResolve(assembly!, name);
                    if (assembly != null)
                        data = (byte*)GetManifestResourceInternal(new QCallAssembly(ref assembly), name, out length, ObjectHandleOnStack.Create(ref resourceModule));
                    if (data == null)
                        return null;
                }

                // It cannot use SafeBuffer mode because not all methods are supported in this
                // mode (e.g. UnmanagedMemoryStream.get_PositionPointer)
                return new UnmanagedMemoryStreamForModule(data, length, resourceModule!);
            }
        }

        public override Stream? GetManifestResourceStream(Type type, string name)
        {
            if (name == null)
                ArgumentNullException.ThrowIfNull(type);

            string? nameSpace = type?.Namespace;

            string resourceName = nameSpace != null && name != null ?
                nameSpace + Type.Delimiter + name :
                nameSpace + name;

            return GetManifestResourceStream(resourceName);
        }

        public override AssemblyName GetName(bool copiedName)
        {
            return AssemblyName.Create(_mono_assembly, GetInfo(AssemblyInfoKind.CodeBase));
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return InternalGetType(null, name, throwOnError, ignoreCase);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
        }

        public override Module? GetModule(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            Module[] modules = GetModules(true);
            foreach (Module module in modules)
            {
                if (module.ScopeName == name)
                    return module;
            }

            return null;
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            var this_assembly = this;
            Module[]? tmp = null;
            GetModulesInternal(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref tmp));
            Module[] modules = tmp!;

            if (!getResourceModules)
            {
                var result = new List<Module>(modules.Length);
                foreach (Module m in modules)
                    if (!m.IsResource())
                        result.Add(m);
                return result.ToArray();
            }
            else
                return modules;
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModules(getResourceModules);
        }

        internal static AssemblyName[] GetReferencedAssemblies(Assembly assembly)
        {
            // Can't use QCallAssembly as assembly can be an AssemblyBuilder
            using (var nativeNames = new Mono.SafeGPtrArrayHandle(InternalGetReferencedAssemblies(assembly)))
            {
                int numAssemblies = nativeNames.Length;
                try
                {
                    AssemblyName[] result = new AssemblyName[numAssemblies];
                    const bool addVersion = true;
                    const bool addPublicKey = false;
                    const bool defaultToken = true;
                    for (int i = 0; i < numAssemblies; i++)
                    {
                        AssemblyName name = new AssemblyName();
                        unsafe
                        {
                            Mono.MonoAssemblyName* nativeName = (Mono.MonoAssemblyName*)nativeNames[i];
                            name.FillName(nativeName, null, addVersion, addPublicKey, defaultToken);
                            result[i] = name;
                        }
                    }
                    return result;
                }
                finally
                {
                    for (int i = 0; i < numAssemblies; i++)
                    {
                        unsafe
                        {
                            Mono.MonoAssemblyName* nativeName = (Mono.MonoAssemblyName*)nativeNames[i];
                            AssemblyName.FreeAssemblyName(ref *nativeName, true);
                        }
                    }
                }
            }
        }

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies() => RuntimeAssembly.GetReferencedAssemblies (this);

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            return GetSatelliteAssembly(culture, null);
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
        {
            ArgumentNullException.ThrowIfNull(culture);

            return InternalGetSatelliteAssembly(this, culture, version, true)!;
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        internal static Assembly? InternalGetSatelliteAssembly(Assembly assembly, CultureInfo culture, Version? version, bool throwOnFileNotFound)
        {
            AssemblyName aname = assembly.GetName();

            var an = new AssemblyName();
            if (version == null)
                an.Version = aname.Version;
            else
                an.Version = version;

            an.CultureInfo = culture;
            an.Name = aname.Name + ".resources";

            Assembly? res = null;
            try
            {
                StackCrawlMark unused = default;
                res = Load(an, ref unused, AssemblyLoadContext.GetLoadContext(assembly));
            }
            catch
            {
            }

            if (res == assembly)
                res = null;
            if (res == null && throwOnFileNotFound)
                throw new FileNotFoundException(SR.Format(culture, SR.IO_FileNotFound_FileName, an.Name));
            return res;
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override FileStream? GetFile(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            if (Location.Length == 0)
            {
                // Throw if the assembly was loaded from memory, indicated by Location returning an empty string
                throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
            }

            RuntimeModule? m = (RuntimeModule?)GetModule(name);

            if (m != null)
                return new FileStream(m.FullyQualifiedName, FileMode.Open, FileAccess.Read);
            else
                return null;
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override FileStream[] GetFiles(bool getResourceModules)
        {
            if (Location.Length == 0)
            {
                // Throw if the assembly was loaded from memory, indicated by Location returning an empty string
                throw new FileNotFoundException(SR.IO_NoFileTableInInMemoryAssemblies);
            }

            Module[] modules = GetModules(getResourceModules);

            if (modules.Length == 0)
                return Array.Empty<FileStream>();

            FileStream[] res = new FileStream[modules.Length];

            for (int i = 0; i < modules.Length; i++)
            {
                RuntimeModule m = (RuntimeModule)modules[i];
                res[i] = new FileStream(m.FullyQualifiedName, FileMode.Open, FileAccess.Read);
            }

            return res;
        }

        internal static RuntimeAssembly InternalLoad(AssemblyName assemblyRef, ref StackCrawlMark stackMark, AssemblyLoadContext? assemblyLoadContext)
        {
            var assembly = (RuntimeAssembly)InternalLoad(assemblyRef.FullName, ref stackMark, assemblyLoadContext != null ? assemblyLoadContext.NativeALC : IntPtr.Zero);
            if (assembly == null)
                throw new FileNotFoundException(null, assemblyRef.Name);
            return assembly;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetInfo(QCallAssembly assembly, ObjectHandleOnStack res, AssemblyInfoKind kind);

        private string? GetInfo(AssemblyInfoKind kind)
        {
            var this_assembly = this;
            string? res = null;
            GetInfo(new QCallAssembly(ref this_assembly), ObjectHandleOnStack.Create(ref res), kind);
            return res;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool GetManifestResourceInfoInternal(QCallAssembly assembly, string name, ManifestResourceInfo info);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr /* byte* */ GetManifestResourceInternal(QCallAssembly assembly, string name, out int size, ObjectHandleOnStack module);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetManifestModuleInternal(QCallAssembly assembly, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetModulesInternal(QCallAssembly assembly, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalGetReferencedAssemblies(Assembly assembly);

        internal string? GetSimpleName()
        {
            // TODO: Make this cheaper and faster
            return GetName().Name;
        }
    }
}
