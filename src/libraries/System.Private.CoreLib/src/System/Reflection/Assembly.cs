// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
    public abstract partial class Assembly : ICustomAttributeProvider, ISerializable
    {
        protected Assembly() { }

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
                        throw new NotSupportedException(SR.Format(SR.NotSupported_NoTypeInfo, types[i].FullName));

                    typeinfos[i] = typeinfo;
                }
                return typeinfos;
            }
        }

        public virtual Type[] GetTypes()
        {
            Module[] m = GetModules(false);

            int finalLength = 0;
            Type[][] moduleTypes = new Type[m.Length][];

            for (int i = 0; i < moduleTypes.Length; i++)
            {
                moduleTypes[i] = m[i].GetTypes();
                finalLength += moduleTypes[i].Length;
            }

            int current = 0;
            Type[] ret = new Type[finalLength];
            for (int i = 0; i < moduleTypes.Length; i++)
            {
                int length = moduleTypes[i].Length;
                Array.Copy(moduleTypes[i], 0, ret, current, length);
                current += length;
            }

            return ret;
        }

        public virtual IEnumerable<Type> ExportedTypes => GetExportedTypes();
        public virtual Type[] GetExportedTypes() { throw NotImplemented.ByDesign; }
        public virtual Type[] GetForwardedTypes() { throw NotImplemented.ByDesign; }

        public virtual string CodeBase { get { throw NotImplemented.ByDesign; } }
        public virtual MethodInfo EntryPoint { get { throw NotImplemented.ByDesign; } }
        public virtual string FullName { get { throw NotImplemented.ByDesign; } }
        public virtual string ImageRuntimeVersion { get { throw NotImplemented.ByDesign; } }
        public virtual bool IsDynamic => false;
        public virtual string Location { get { throw NotImplemented.ByDesign; } }
        public virtual bool ReflectionOnly { get { throw NotImplemented.ByDesign; } }

        public virtual ManifestResourceInfo GetManifestResourceInfo(string resourceName) { throw NotImplemented.ByDesign; }
        public virtual string[] GetManifestResourceNames() { throw NotImplemented.ByDesign; }
        public virtual Stream GetManifestResourceStream(string name) { throw NotImplemented.ByDesign; }
        public virtual Stream GetManifestResourceStream(Type type, string name) { throw NotImplemented.ByDesign; }

        public bool IsFullyTrusted => true;

        public virtual AssemblyName GetName() => GetName(copiedName: false);
        public virtual AssemblyName GetName(bool copiedName) { throw NotImplemented.ByDesign; }

        public virtual Type GetType(string name) => GetType(name, throwOnError: false, ignoreCase: false);
        public virtual Type GetType(string name, bool throwOnError) => GetType(name, throwOnError: throwOnError, ignoreCase: false);
        public virtual Type GetType(string name, bool throwOnError, bool ignoreCase) { throw NotImplemented.ByDesign; }

        public virtual bool IsDefined(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }

        public virtual object[] GetCustomAttributes(bool inherit) { throw NotImplemented.ByDesign; }
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }

        public virtual string EscapedCodeBase => AssemblyName.EscapeCodeBase(CodeBase);

        public object CreateInstance(string typeName) => CreateInstance(typeName, false, BindingFlags.Public | BindingFlags.Instance, binder: null, args: null, culture: null, activationAttributes: null);
        public object CreateInstance(string typeName, bool ignoreCase) => CreateInstance(typeName, ignoreCase, BindingFlags.Public | BindingFlags.Instance, binder: null, args: null, culture: null, activationAttributes: null);
        public virtual object CreateInstance(string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            Type t = GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
            if (t == null)
                return null;

            return Activator.CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);
        }

        public virtual event ModuleResolveEventHandler ModuleResolve { add { throw NotImplemented.ByDesign; } remove { throw NotImplemented.ByDesign; } }

        public virtual Module ManifestModule { get { throw NotImplemented.ByDesign; } }
        public virtual Module GetModule(string name) { throw NotImplemented.ByDesign; }

        public Module[] GetModules() => GetModules(getResourceModules: false);
        public virtual Module[] GetModules(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual IEnumerable<Module> Modules => GetLoadedModules(getResourceModules: true);
        public Module[] GetLoadedModules() => GetLoadedModules(getResourceModules: false);
        public virtual Module[] GetLoadedModules(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual AssemblyName[] GetReferencedAssemblies() { throw NotImplemented.ByDesign; }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture) { throw NotImplemented.ByDesign; }
        public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version version) { throw NotImplemented.ByDesign; }

        public virtual FileStream GetFile(string name) { throw NotImplemented.ByDesign; }
        public virtual FileStream[] GetFiles() => GetFiles(getResourceModules: false);
        public virtual FileStream[] GetFiles(bool getResourceModules) { throw NotImplemented.ByDesign; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { throw NotImplemented.ByDesign; }

        public override string ToString()
        {
            string displayName = FullName;
            if (displayName == null)
                return base.ToString();
            else
                return displayName;
        }

        /*
          Returns true if the assembly was loaded from the global assembly cache.
        */
        public virtual bool GlobalAssemblyCache { get { throw NotImplemented.ByDesign; } }
        public virtual Int64 HostContext { get { throw NotImplemented.ByDesign; } }

        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(Assembly left, Assembly right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Assembly left, Assembly right)
        {
            return !(left == right);
        }

        public static string CreateQualifiedName(string assemblyName, string typeName) => typeName + ", " + assemblyName;

        public static Assembly GetAssembly(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Module m = type.Module;
            if (m == null)
                return null;
            else
                return m.Assembly;
        }

        public static Assembly Load(byte[] rawAssembly) => Load(rawAssembly, rawSymbolStore: null);

        [Obsolete("This method has been deprecated. Please use Assembly.Load() instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static Assembly LoadWithPartialName(string partialName)
        {
            if (partialName == null)
                throw new ArgumentNullException(nameof(partialName));

            return Load(partialName);
        }

        public static Assembly UnsafeLoadFrom(string assemblyFile) => LoadFrom(assemblyFile);

        public Module LoadModule(string moduleName, byte[] rawModule) => LoadModule(moduleName, rawModule, null);
        public virtual Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore) { throw NotImplemented.ByDesign; }

        public static Assembly ReflectionOnlyLoad(byte[] rawAssembly) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }
        public static Assembly ReflectionOnlyLoad(string assemblyString) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }
        public static Assembly ReflectionOnlyLoadFrom(string assemblyFile) { throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionOnly); }

        public virtual SecurityRuleSet SecurityRuleSet => SecurityRuleSet.None;
    }
}
