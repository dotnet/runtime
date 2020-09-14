// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// System.Reflection.Emit/AssemblyBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

#if MONO_FEATURE_SRE
using System.IO;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    internal class GenericInstanceKey
    {
        private Type gtd;
        internal Type[] args;
        private int hash_code;

        internal GenericInstanceKey(Type gtd, Type[] args)
        {
            this.gtd = gtd;
            this.args = args;

            hash_code = gtd.GetHashCode();
            for (int i = 0; i < args.Length; ++i)
                hash_code ^= args[i].GetHashCode();
        }

        private static bool IsBoundedVector(Type type)
        {
            ArrayType? at = type as ArrayType;
            if (at != null)
                return at.GetEffectiveRank() == 1;
            return type.ToString().EndsWith("[*]", StringComparison.Ordinal); /*Super uggly hack, SR doesn't allow one to query for it */
        }

        private static bool TypeEquals(Type a, Type b)
        {
            if (a == b)
                return true;

            if (a.HasElementType)
            {
                if (!b.HasElementType)
                    return false;
                if (!TypeEquals(a.GetElementType()!, b.GetElementType()!))
                    return false;
                if (a.IsArray)
                {
                    if (!b.IsArray)
                        return false;
                    int rank = a.GetArrayRank();
                    if (rank != b.GetArrayRank())
                        return false;
                    if (rank == 1 && IsBoundedVector(a) != IsBoundedVector(b))
                        return false;
                }
                else if (a.IsByRef)
                {
                    if (!b.IsByRef)
                        return false;
                }
                else if (a.IsPointer)
                {
                    if (!b.IsPointer)
                        return false;
                }
                return true;
            }

            if (a.IsGenericType)
            {
                if (!b.IsGenericType)
                    return false;
                if (a.IsGenericParameter)
                    return a == b;
                if (a.IsGenericParameter) //previous test should have caught it
                    return false;

                if (a.IsGenericTypeDefinition)
                {
                    if (!b.IsGenericTypeDefinition)
                        return false;
                }
                else
                {
                    if (b.IsGenericTypeDefinition)
                        return false;
                    if (!TypeEquals(a.GetGenericTypeDefinition(), b.GetGenericTypeDefinition()))
                        return false;

                    Type[] argsA = a.GetGenericArguments();
                    Type[] argsB = b.GetGenericArguments();
                    for (int i = 0; i < argsA.Length; ++i)
                    {
                        if (!TypeEquals(argsA[i], argsB[i]))
                            return false;
                    }
                }
            }

            /*
            Now only non-generic, non compound types are left. To properly deal with user
            types we would have to call UnderlyingSystemType, but we let them have their
            own instantiation as this is MS behavior and mcs (pre C# 4.0, at least) doesn't
            depend on proper UT canonicalization.
            */
            return a == b;
        }

        public override bool Equals(object? obj)
        {
            GenericInstanceKey? other = obj as GenericInstanceKey;
            if (other == null)
                return false;
            if (gtd != other.gtd)
                return false;
            for (int i = 0; i < args.Length; ++i)
            {
                Type a = args[i];
                Type b = other.args[i];
                /*
                We must cannonicalize as much as we can. Using equals means that some resulting types
                won't have the exact same types as the argument ones.
                For example, flyweight types used array, pointer and byref will should this behavior.
                MCS seens to be resilient to this problem so hopefully this won't show up.
                */
                if (a != b && !a.Equals(b))
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return hash_code;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class AssemblyBuilder : Assembly
    {
        //
        // AssemblyBuilder inherits from Assembly, but the runtime thinks its layout inherits from RuntimeAssembly
        //
#region Sync with RuntimeAssembly.cs and ReflectionAssembly in object-internals.h
        internal IntPtr _mono_assembly;
        private object? _evidence;

        private UIntPtr dynamic_assembly; /* GC-tracked */
        private MethodInfo? entry_point;
        private ModuleBuilder[] modules;
        private string? name;
        private string? dir;
        private CustomAttributeBuilder[]? cattrs;
        private object? resources;
        private byte[]? public_key;
        private string? version;
        private string? culture;
        private uint algid;
        private uint flags;
        private PEFileKinds pekind = PEFileKinds.Dll;
        private bool delay_sign;
        private uint access;
        private Module[]? loaded_modules;
        private object? win32_resources;
        private object? permissions_minimum;
        private object? permissions_optional;
        private object? permissions_refused;
        private int peKind;
        private int machine;
        private bool corlib_internal;
        private Type[]? type_forwarders;
        private byte[]? pktoken;
#endregion

        private AssemblyName aname;
        private string? assemblyName;
        private bool created;
        private string? versioninfo_culture;
        private ModuleBuilder manifest_module;
        private bool manifest_module_used;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [DynamicDependency("RuntimeResolve", typeof(ModuleBuilder))]
        private static extern void basic_init(AssemblyBuilder ab);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void UpdateNativeCustomAttributes(AssemblyBuilder ab);

        [DynamicDependency(nameof(pktoken))] // Automatically keeps all previous fields too due to StructLayout
        private AssemblyBuilder(AssemblyName n, string? directory, AssemblyBuilderAccess access, bool corlib_internal)
        {
            aname = (AssemblyName)n.Clone();

            if (!Enum.IsDefined(typeof(AssemblyBuilderAccess), access))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Argument value {0} is not valid.", (int)access),
                    nameof(access));

            name = n.Name;
            this.access = (uint)access;
            flags = (uint)n.Flags;

            dir = directory;

            /* Set defaults from n */
            if (n.CultureInfo != null)
            {
                culture = n.CultureInfo.Name;
                versioninfo_culture = n.CultureInfo.Name;
            }
            Version? v = n.Version;
            if (v != null)
            {
                version = v.ToString();
            }

            basic_init(this);

            // Netcore only allows one module per assembly
            manifest_module = new ModuleBuilder(this, "RefEmit_InMemoryManifestModule", false);
            modules = new ModuleBuilder[] { manifest_module };
        }

        public override string? CodeBase
        {
            get { throw not_supported(); }
        }

        public override MethodInfo? EntryPoint
        {
            get
            {
                return entry_point;
            }
        }

        public override string Location
        {
            get
            {
                throw not_supported();
            }
        }

        public override bool ReflectionOnly
        {
            get { return base.ReflectionOnly; }
        }

        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return new AssemblyBuilder(name, null, access, false);
        }

        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            AssemblyBuilder ab = DefineDynamicAssembly(name, access);
            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder attr in assemblyAttributes)
                    ab.SetCustomAttribute(attr);
            }

            return ab;
        }

        public ModuleBuilder DefineDynamicModule(string name)
        {
            return DefineDynamicModule(name, false);
        }

        public ModuleBuilder DefineDynamicModule(string name, bool emitSymbolInfo)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException("Empty name is not legal.", nameof(name));
            if (name[0] == '\0')
                throw new ArgumentException(SR.Argument_InvalidName, nameof(name));

            if (manifest_module_used)
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            manifest_module_used = true;
            return manifest_module;
        }

        public ModuleBuilder? GetDynamicModule(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException("Empty name is not legal.", nameof(name));

            if (modules != null)
                for (int i = 0; i < modules.Length; ++i)
                    if (modules[i].name == name)
                        return modules[i];
            return null;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetExportedTypes()
        {
            throw not_supported();
        }

        public override FileStream GetFile(string name)
        {
            throw not_supported();
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            throw not_supported();
        }

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName)
        {
            throw not_supported();
        }

        public override string[] GetManifestResourceNames()
        {
            throw not_supported();
        }

        public override Stream? GetManifestResourceStream(string name)
        {
            throw not_supported();
        }
        public override Stream? GetManifestResourceStream(Type type, string name)
        {
            throw not_supported();
        }

        public override bool IsCollectible
        {
            get
            {
                return access == (uint)AssemblyBuilderAccess.RunAndCollect;
            }
        }

        internal string? AssemblyDir
        {
            get
            {
                return dir;
            }
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));

            if (cattrs != null)
            {
                CustomAttributeBuilder[] new_array = new CustomAttributeBuilder[cattrs.Length + 1];
                cattrs.CopyTo(new_array, 0);
                new_array[cattrs.Length] = customBuilder;
                cattrs = new_array;
            }
            else
            {
                cattrs = new CustomAttributeBuilder[1];
                cattrs[0] = customBuilder;
            }

            UpdateNativeCustomAttributes(this);
        }

        [ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException(nameof(con));
            if (binaryAttribute == null)
                throw new ArgumentNullException(nameof(binaryAttribute));

            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        private Exception not_supported()
        {
            // Strange message but this is what MS.NET prints...
            return new NotSupportedException("The invoked member is not supported in a dynamic module.");
        }

        private string create_assembly_version(string version)
        {
            string[] parts = version.Split('.');
            int[] ver = new int[4] { 0, 0, 0, 0 };

            if ((parts.Length < 0) || (parts.Length > 4))
                throw new ArgumentException("The version specified '" + version + "' is invalid");

            for (int i = 0; i < parts.Length; ++i)
            {
                if (parts[i] == "*")
                {
                    DateTime now = DateTime.Now;

                    if (i == 2)
                    {
                        ver[2] = (now - new DateTime(2000, 1, 1)).Days;
                        if (parts.Length == 3)
                            ver[3] = (now.Second + (now.Minute * 60) + (now.Hour * 3600)) / 2;
                    }
                    else
                        if (i == 3)
                        ver[3] = (now.Second + (now.Minute * 60) + (now.Hour * 3600)) / 2;
                    else
                        throw new ArgumentException("The version specified '" + version + "' is invalid");
                }
                else
                {
                    try
                    {
                        ver[i] = int.Parse(parts[i]);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException("The version specified '" + version + "' is invalid");
                    }
                }
            }

            return ver[0] + "." + ver[1] + "." + ver[2] + "." + ver[3];
        }

        private string GetCultureString(string str)
        {
            return (str == "neutral" ? string.Empty : str);
        }

        /*Warning, @typeArguments must be a mscorlib internal array. So make a copy before passing it in*/
        internal Type MakeGenericType(Type gtd, Type[] typeArguments)
        {
            return new TypeBuilderInstantiation(gtd, typeArguments);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException(name);
            if (name.Length == 0)
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Type res = InternalGetType(null, name, throwOnError, ignoreCase);
            if (res is TypeBuilder)
            {
                if (throwOnError)
                    throw new TypeLoadException(string.Format("Could not load type '{0}' from assembly '{1}'", name, this.name));
                return null;
            }
            return res;
        }

        public override Module? GetModule(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException("Name can't be empty");

            if (modules == null)
                return null;

            foreach (Module module in modules)
            {
                if (module.ScopeName == name)
                    return module;
            }

            return null;
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return (Module[])modules.Clone();
        }

        public override AssemblyName GetName(bool copiedName)
        {
            return AssemblyName.Create(_mono_assembly, null);
        }

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies() => RuntimeAssembly.GetReferencedAssemblies (this);

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModules(getResourceModules);
        }

        //FIXME MS has issues loading satelite assemblies from SRE
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            return GetSatelliteAssembly(culture, null);
        }

        //FIXME MS has issues loading satelite assemblies from SRE
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
        {
            return RuntimeAssembly.InternalGetSatelliteAssembly(this, culture, version, true)!;
        }

        public override Module ManifestModule
        {
            get
            {
                return manifest_module;
            }
        }

        [Obsolete(Obsoletions.GlobalAssemblyCacheMessage, DiagnosticId = Obsoletions.GlobalAssemblyCacheDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public override bool GlobalAssemblyCache
        {
            get
            {
                return false;
            }
        }

        public override bool IsDynamic
        {
            get { return true; }
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributes(this);
        }

        public override string? FullName
        {
            get
            {
                return aname.ToString();
            }
        }
    }
}
#endif
