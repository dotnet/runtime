// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Reflection
{
    [Serializable]
    public abstract class Module : ISerializable, ICustomAttributeProvider
    {
        #region Static Constructor
        static Module()
        {
            __Filters _fltObj;
            _fltObj = new __Filters();
            FilterTypeName = new TypeFilter(_fltObj.FilterTypeName);
            FilterTypeNameIgnoreCase = new TypeFilter(_fltObj.FilterTypeNameIgnoreCase);
        }
        #endregion

        #region Constructor
        protected Module()
        {
        }
        #endregion

        #region Public Statics
        public static readonly TypeFilter FilterTypeName;
        public static readonly TypeFilter FilterTypeNameIgnoreCase;

        public static bool operator ==(Module left, Module right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeModule || right is RuntimeModule)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Module left, Module right)
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
        #endregion

        #region Literals
        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        #endregion

        #region object overrides
        public override String ToString()
        {
            return ScopeName;
        }
        #endregion

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }
        #region ICustomAttributeProvider Members
        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
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
        #endregion

        #region public instances members
        public MethodBase ResolveMethod(int metadataToken)
        {
            return ResolveMethod(metadataToken, null, null);
        }

        public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);

            throw new NotImplementedException();
        }

        public FieldInfo ResolveField(int metadataToken)
        {
            return ResolveField(metadataToken, null, null);
        }

        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);

            throw new NotImplementedException();
        }

        public Type ResolveType(int metadataToken)
        {
            return ResolveType(metadataToken, null, null);
        }

        public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);

            throw new NotImplementedException();
        }

        public MemberInfo ResolveMember(int metadataToken)
        {
            return ResolveMember(metadataToken, null, null);
        }

        public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);

            throw new NotImplementedException();
        }

        public virtual byte[] ResolveSignature(int metadataToken)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveSignature(metadataToken);

            throw new NotImplementedException();
        }

        public virtual string ResolveString(int metadataToken)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveString(metadataToken);

            throw new NotImplementedException();
        }

        public virtual void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                rtModule.GetPEKind(out peKind, out machine);

            throw new NotImplementedException();
        }

        public virtual int MDStreamVersion
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.MDStreamVersion;

                throw new NotImplementedException();
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public virtual Type GetType(String className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }

        public virtual Type GetType(String className)
        {
            return GetType(className, false, false);
        }

        public virtual Type GetType(String className, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public virtual String FullyQualifiedName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Type[] FindTypes(TypeFilter filter, Object filterCriteria)
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

        public virtual Type[] GetTypes()
        {
            throw new NotImplementedException();
        }

        public virtual Guid ModuleVersionId
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.ModuleVersionId;

                throw new NotImplementedException();
            }
        }

        public virtual int MetadataToken
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.MetadataToken;

                throw new NotImplementedException();
            }
        }

        public virtual bool IsResource()
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.IsResource();

            throw new NotImplementedException();
        }

        public FieldInfo[] GetFields()
        {
            return GetFields(Module.DefaultLookup);
        }

        public virtual FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetFields(bindingFlags);

            throw new NotImplementedException();
        }

        public FieldInfo GetField(String name)
        {
            return GetField(name, Module.DefaultLookup);
        }

        public virtual FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetField(name, bindingAttr);

            throw new NotImplementedException();
        }

        public MethodInfo[] GetMethods()
        {
            return GetMethods(Module.DefaultLookup);
        }

        public virtual MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            // This API was made virtual in V4. Code compiled against V2 might use
            // "call" rather than "callvirt" to call it.
            // This makes sure those code still works.
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetMethods(bindingFlags);

            throw new NotImplementedException();
        }

        public MethodInfo GetMethod(
            String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (types == null)
                throw new ArgumentNullException(nameof(types));
            Contract.EndContractBlock();

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }

            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public MethodInfo GetMethod(String name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (types == null)
                throw new ArgumentNullException(nameof(types));
            Contract.EndContractBlock();

            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }

            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, types, null);
        }

        public MethodInfo GetMethod(String name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Contract.EndContractBlock();

            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any,
                null, null);
        }

        protected virtual MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public virtual String ScopeName
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.ScopeName;

                throw new NotImplementedException();
            }
        }

        public virtual String Name
        {
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.Name;

                throw new NotImplementedException();
            }
        }

        public virtual Assembly Assembly
        {
            [Pure]
            get
            {
                // This API was made virtual in V4. Code compiled against V2 might use
                // "call" rather than "callvirt" to call it.
                // This makes sure those code still works.
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.Assembly;

                throw new NotImplementedException();
            }
        }

        // This API never fails, it will return an empty handle for non-runtime handles and 
        // a valid handle for reflection only modules.
        public ModuleHandle ModuleHandle
        {
            get
            {
                return GetModuleHandle();
            }
        }

        // Used to provide implementation and overriding point for ModuleHandle.
        // To get a module handle inside mscorlib, use GetNativeHandle instead.
        internal virtual ModuleHandle GetModuleHandle()
        {
            return ModuleHandle.EmptyHandle;
        }
        #endregion
    }
}
