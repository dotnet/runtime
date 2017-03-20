// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public abstract class Module : ICustomAttributeProvider, ISerializable
    {
        protected Module() { }

        public virtual Assembly Assembly { get { throw NotImplemented.ByDesign; } }
        public virtual string FullyQualifiedName { get { throw NotImplemented.ByDesign; } }
        public virtual string Name { get { throw NotImplemented.ByDesign; } }

        public virtual int MDStreamVersion { get { throw NotImplemented.ByDesign; } }
        public virtual Guid ModuleVersionId { get { throw NotImplemented.ByDesign; } }
        public virtual string ScopeName { get { throw NotImplemented.ByDesign; } }
        public ModuleHandle ModuleHandle => GetModuleHandleImpl();
        protected virtual ModuleHandle GetModuleHandleImpl() => ModuleHandle.EmptyHandle; // Not an api but declared protected because of Reflection.Core/Corelib divide (when built by CoreRt)
        public virtual void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine) { throw NotImplemented.ByDesign; }
        public virtual bool IsResource() { throw NotImplemented.ByDesign; }

        public virtual bool IsDefined(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }
        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }
        public virtual object[] GetCustomAttributes(bool inherit) { throw NotImplemented.ByDesign; }
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit) { throw NotImplemented.ByDesign; }

        public MethodInfo GetMethod(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, null, null);
        }

        public MethodInfo GetMethod(string name, Type[] types) => GetMethod(name, Module.DefaultLookup, null, CallingConventions.Any, types, null);
        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }
            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        protected virtual MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) { throw NotImplemented.ByDesign; }

        public MethodInfo[] GetMethods() => GetMethods(Module.DefaultLookup);
        public virtual MethodInfo[] GetMethods(BindingFlags bindingFlags) { throw NotImplemented.ByDesign; }

        public FieldInfo GetField(string name) => GetField(name, Module.DefaultLookup);
        public virtual FieldInfo GetField(string name, BindingFlags bindingAttr) { throw NotImplemented.ByDesign; }

        public FieldInfo[] GetFields() => GetFields(Module.DefaultLookup);
        public virtual FieldInfo[] GetFields(BindingFlags bindingFlags) { throw NotImplemented.ByDesign; }

        public virtual Type[] GetTypes() { throw NotImplemented.ByDesign; }

        public virtual Type GetType(string className) => GetType(className, throwOnError: false, ignoreCase: false);
        public virtual Type GetType(string className, bool ignoreCase) => GetType(className, throwOnError: false, ignoreCase: ignoreCase);
        public virtual Type GetType(string className, bool throwOnError, bool ignoreCase) { throw NotImplemented.ByDesign; }

        public virtual Type[] FindTypes(TypeFilter filter, object filterCriteria)
        {
            Type[] c = GetTypes();
            int cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (filter != null && !filter(c[i], filterCriteria))
                    c[i] = null;
                else
                    cnt++;
            }
            if (cnt == c.Length)
                return c;

            Type[] ret = new Type[cnt];
            cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            }
            return ret;
        }

        public virtual int MetadataToken { get { throw NotImplemented.ByDesign; } }

        public FieldInfo ResolveField(int metadataToken) => ResolveField(metadataToken, null, null);
        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) { throw NotImplemented.ByDesign; }

        public MemberInfo ResolveMember(int metadataToken) => ResolveMember(metadataToken, null, null);
        public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) { throw NotImplemented.ByDesign; }

        public MethodBase ResolveMethod(int metadataToken) => ResolveMethod(metadataToken, null, null);
        public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) { throw NotImplemented.ByDesign; }

        public virtual byte[] ResolveSignature(int metadataToken) { throw NotImplemented.ByDesign; }
        public virtual string ResolveString(int metadataToken) { throw NotImplemented.ByDesign; }

        public Type ResolveType(int metadataToken) => ResolveType(metadataToken, null, null);
        public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) { throw NotImplemented.ByDesign; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { throw NotImplemented.ByDesign; }

        public override bool Equals(object o) => base.Equals(o);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(Module left, Module right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Module left, Module right) => !(left == right);

        public override string ToString() => ScopeName;

        public static readonly TypeFilter FilterTypeName = FilterTypeNameImpl;
        public static readonly TypeFilter FilterTypeNameIgnoreCase = FilterTypeNameIgnoreCaseImpl;

        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

        // FilterTypeName 
        // This method will filter the class based upon the name.  It supports
        //    a trailing wild card.
        private static bool FilterTypeNameImpl(Type cls, object filterCriteria)
        {
            // Check that the criteria object is a String object
            if (filterCriteria == null || !(filterCriteria is string))
                throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);

            string str = (string)filterCriteria;

            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return cls.Name.StartsWith(str, StringComparison.Ordinal);
            }

            return cls.Name.Equals(str);
        }

        // FilterFieldNameIgnoreCase
        // This method filter the Type based upon name, it ignores case.
        private static bool FilterTypeNameIgnoreCaseImpl(Type cls, object filterCriteria)
        {
            // Check that the criteria object is a String object
            if (filterCriteria == null || !(filterCriteria is string))
                throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);

            string str = (string)filterCriteria;

            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                string name = cls.Name;
                if (name.Length >= str.Length)
                    return (string.Compare(name, 0, str, 0, str.Length, StringComparison.OrdinalIgnoreCase) == 0);
                else
                    return false;
            }
            return (string.Compare(str, cls.Name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
