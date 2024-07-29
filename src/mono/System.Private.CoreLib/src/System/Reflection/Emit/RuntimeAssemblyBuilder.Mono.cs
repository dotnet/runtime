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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace System.Reflection.Emit
{
    internal sealed class GenericInstanceKey
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
            if (type is SymbolType st && st.IsArray)
                return st.GetArrayRank() == 1;
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
                We must canonicalize as much as we can. Using equals means that some resulting types
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

    public partial class AssemblyBuilder
    {
        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            ArgumentNullException.ThrowIfNull(name);

            return new RuntimeAssemblyBuilder(name, access);
        }

        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
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
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class RuntimeAssemblyBuilder : AssemblyBuilder
    {
        //
        // AssemblyBuilder inherits from Assembly, but the runtime thinks its layout inherits from RuntimeAssembly
        //
#region Sync with RuntimeAssembly.cs and ReflectionAssembly in object-internals.h
        internal IntPtr _mono_assembly;
        private LoaderAllocator? m_keepalive;

        private UIntPtr dynamic_assembly; /* GC-tracked */
        private RuntimeModuleBuilder[] modules;
        private string? name;
        private CustomAttributeBuilder[]? cattrs;
        private string? version;
        private string? culture;
        private byte[]? public_key_token;
        private Module[]? loaded_modules;
        private uint access;
#endregion

        private AssemblyName aname;
        private RuntimeModuleBuilder manifest_module;
        private bool manifest_module_used;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [DynamicDependency("RuntimeResolve", typeof(RuntimeModuleBuilder))]
        private static extern void basic_init(RuntimeAssemblyBuilder ab);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void UpdateNativeCustomAttributes(RuntimeAssemblyBuilder ab);

        [DynamicDependency(nameof(access))] // Automatically keeps all previous fields too due to StructLayout
        internal RuntimeAssemblyBuilder(AssemblyName n, AssemblyBuilderAccess access)
        {
            EnsureDynamicCodeSupported();

            aname = (AssemblyName)n.Clone();

            if (!Enum.IsDefined(typeof(AssemblyBuilderAccess), access))
                throw new ArgumentException(SR.Format(CultureInfo.InvariantCulture,
                    SR.Arg_EnumIllegalVal, (int)access),
                    nameof(access));

            name = n.Name;
            this.access = (uint)access;

            /* Set defaults from n */
            if (n.CultureInfo != null)
                culture = n.CultureInfo.Name;
            Version? v = n.Version;
            if (v != null)
            {
                version = v.ToString();
            }
            public_key_token = n.GetPublicKeyToken();

            basic_init(this);

            // Netcore only allows one module per assembly
            manifest_module = new RuntimeModuleBuilder(this, "RefEmit_InMemoryManifestModule");
            modules = new RuntimeModuleBuilder[] { manifest_module };

            AssemblyLoadContext.InvokeAssemblyLoadEvent(this);
        }

        public override bool ReflectionOnly
        {
            get { return base.ReflectionOnly; }
        }

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            if (name[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_InvalidName, nameof(name));
            }

            if (manifest_module_used)
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            manifest_module_used = true;
            return manifest_module;
        }

        internal static AssemblyBuilder InternalDefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            AssemblyLoadContext? _,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            return DefineDynamicAssembly(name, access, assemblyAttributes);
        }

        protected override ModuleBuilder? GetDynamicModuleCore(string name)
        {
            if (modules != null)
                for (int i = 0; i < modules.Length; ++i)
                    if (modules[i].name == name)
                        return modules[i];

            return null;
        }

        public override bool IsCollectible => access == (uint)AssemblyBuilderAccess.RunAndCollect;

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            CustomAttributeBuilder customBuilder = new CustomAttributeBuilder(con, binaryAttribute);
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

        /*Warning, @typeArguments must be a mscorlib internal array. So make a copy before passing it in*/
        internal static Type MakeGenericType(Type gtd, Type[] typeArguments) =>
            new TypeBuilderInstantiation(gtd, typeArguments);

        [RequiresUnreferencedCode("Types might be removed by trimming. If the type name is a string literal, consider using Type.GetType instead.")]
        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            Type res = InternalGetType(null, name, throwOnError, ignoreCase);
            if (res is TypeBuilder)
            {
                if (throwOnError)
                    throw new TypeLoadException(SR.Format(SR.ClassLoad_General, name, this.name));
                return null;
            }
            return res;
        }

        public override Module? GetModule(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

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
            if (modules == null)
                return Array.Empty<Module>();
            return (Module[])modules.Clone();
        }

        public override AssemblyName GetName(bool copiedName) => AssemblyName.Create(_mono_assembly, null);

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies() => RuntimeAssembly.GetReferencedAssemblies(this);

        public override Module[] GetLoadedModules(bool getResourceModules) => GetModules(getResourceModules);

        //FIXME MS has issues loading satellite assemblies from SRE
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Assembly GetSatelliteAssembly(CultureInfo culture) => GetSatelliteAssembly(culture, null);

        //FIXME MS has issues loading satellite assemblies from SRE
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version) =>
            RuntimeAssembly.InternalGetSatelliteAssembly(this, culture, version, true)!;

        public override Module ManifestModule => manifest_module;
        public override string? FullName => aname.ToString();

        public override bool Equals(object? obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefined(Type attributeType, bool inherit) =>
            CustomAttribute.IsDefined(this, attributeType, inherit);

        public override object[] GetCustomAttributes(bool inherit) => CustomAttribute.GetCustomAttributes(this, inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            CustomAttribute.GetCustomAttributes(this, attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttribute.GetCustomAttributesData(this);
    }
}
