// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.CustomAttributes;
using System.Reflection.Runtime.General;
using System.Text;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeNamedTypeInfo : RuntimeNamedTypeInfo
    {
        private NativeFormatRuntimeNamedTypeInfo(MetadataReader reader, TypeDefinitionHandle typeDefinitionHandle, RuntimeTypeHandle typeHandle) :
            base(typeHandle)
        {
            _reader = reader;
            _typeDefinitionHandle = typeDefinitionHandle;
            _typeDefinition = _typeDefinitionHandle.GetTypeDefinition(reader);
        }

        public sealed override Assembly Assembly
        {
            get
            {
                // If an assembly is split across multiple metadata blobs then the defining scope may
                // not be the canonical scope representing the assembly. We need to look up the assembly
                // by name to ensure we get the right one.

                ScopeDefinitionHandle scopeDefinitionHandle = NamespaceChain.DefiningScope;
                RuntimeAssemblyName runtimeAssemblyName = scopeDefinitionHandle.ToRuntimeAssemblyName(_reader);

                return RuntimeAssemblyInfo.GetRuntimeAssembly(runtimeAssemblyName);
            }
        }

        public sealed override bool IsByRefLike
        {
            get
            {
                // If we have a type handle, ask the runtime
                RuntimeTypeHandle typeHandle = InternalTypeHandleIfAvailable;
                if (!typeHandle.IsNull)
                    return Internal.Runtime.Augments.RuntimeAugments.IsByRefLike(typeHandle);

                // Otherwise fall back to attributes
                foreach (CustomAttributeHandle cah in _typeDefinition.CustomAttributes)
                {
                    if (cah.IsCustomAttributeOfType(_reader, ["System", "Runtime", "CompilerServices"], "IsByRefLikeAttribute"))
                        return true;
                }
                return false;
            }
        }

        protected sealed override Guid? ComputeGuidFromCustomAttributes()
        {
            //
            // Look for a [Guid] attribute. If found, return that.
            //
            foreach (CustomAttributeHandle cah in _typeDefinition.CustomAttributes)
            {
                // We can't reference the GuidAttribute class directly as we don't have an official dependency on System.Runtime.InteropServices.
                // Following age-old CLR tradition, we search for the custom attribute using a name-based search. Since this makes it harder
                // to be sure we won't run into custom attribute constructors that comply with the GuidAttribute(String) signature,
                // we'll check that it does and silently skip the CA if it doesn't match the expected pattern.
                if (cah.IsCustomAttributeOfType(_reader, ["System", "Runtime", "InteropServices"], "GuidAttribute"))
                {
                    CustomAttribute ca = cah.GetCustomAttribute(_reader);
                    HandleCollection.Enumerator fahEnumerator = ca.FixedArguments.GetEnumerator();
                    if (!fahEnumerator.MoveNext())
                        continue;
                    Handle guidStringArgumentHandle = fahEnumerator.Current;
                    if (fahEnumerator.MoveNext())
                        continue;
                    if (guidStringArgumentHandle.HandleType != HandleType.ConstantStringValue)
                        continue;
                    return new Guid(guidStringArgumentHandle.ToConstantStringValueHandle(_reader).GetString(_reader));
                }
            }
            return null;
        }

        protected sealed override void GetPackSizeAndSize(out int packSize, out int size)
        {
            packSize = _typeDefinition.PackingSize;
            size = unchecked((int)(_typeDefinition.Size));
        }

        public sealed override bool IsGenericTypeDefinition
        {
            get
            {
                return _typeDefinition.GenericParameters.GetEnumerator().MoveNext();
            }
        }

        public sealed override string Namespace
        {
            get
            {
                return NamespaceChain.NameSpace.EscapeTypeNameIdentifier();
            }
        }

        public sealed override Type GetGenericTypeDefinition()
        {
            if (_typeDefinition.GenericParameters.GetEnumerator().MoveNext())
                return this.ToType();
            return base.GetGenericTypeDefinition();
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        public sealed override string ToString()
        {
            StringBuilder? sb = null;

            foreach (GenericParameterHandle genericParameterHandle in _typeDefinition.GenericParameters)
            {
                if (sb == null)
                {
                    sb = new StringBuilder(FullName);
                    sb.Append('[');
                }
                else
                {
                    sb.Append(',');
                }

                sb.Append(genericParameterHandle.GetGenericParameter(_reader).Name.GetString(_reader));
            }

            if (sb == null)
            {
                return FullName;
            }
            else
            {
                return sb.Append(']').ToString();
            }
        }

        public sealed override TypeAttributes Attributes => _typeDefinition.Flags;

        public sealed override int GetHashCode()
        {
            return _typeDefinitionHandle.GetHashCode();
        }

        internal sealed override RuntimeTypeInfo InternalDeclaringType
        {
            get
            {
                RuntimeTypeInfo? declaringType = null;
                TypeDefinitionHandle enclosingTypeDefHandle = _typeDefinition.EnclosingType;
                if (!enclosingTypeDefHandle.IsNull(_reader))
                {
                    declaringType = enclosingTypeDefHandle.ResolveTypeDefinition(_reader);
                }
                return declaringType;
            }
        }

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                NamespaceChain namespaceChain = NamespaceChain;
                ScopeDefinitionHandle scopeDefinitionHandle = namespaceChain.DefiningScope;
                return scopeDefinitionHandle.ToRuntimeAssemblyName(_reader).FullName;
            }
        }

        public sealed override string Name
        {
            get
            {
                ConstantStringValueHandle nameHandle = _typeDefinition.Name;
                string name = nameHandle.GetString(_reader);

                return name.EscapeTypeNameIdentifier();
            }
        }

        protected sealed override IEnumerable<CustomAttributeData> TrueCustomAttributes => RuntimeCustomAttributeData.GetCustomAttributes(_reader, _typeDefinition.CustomAttributes);

        internal sealed override RuntimeTypeInfo[] RuntimeGenericTypeParameters
        {
            get
            {
                LowLevelList<RuntimeTypeInfo> genericTypeParameters = new LowLevelList<RuntimeTypeInfo>();

                foreach (GenericParameterHandle genericParameterHandle in _typeDefinition.GenericParameters)
                {
                    RuntimeTypeInfo genericParameterType = NativeFormat.NativeFormatRuntimeGenericParameterTypeInfoForTypes.GetRuntimeGenericParameterTypeInfoForTypes(this, genericParameterHandle);
                    genericTypeParameters.Add(genericParameterType);
                }

                return genericTypeParameters.ToArray();
            }
        }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                Handle baseType = _typeDefinition.BaseType;
                if (baseType.IsNull(_reader))
                    return QTypeDefRefOrSpec.Null;
                return new QTypeDefRefOrSpec(_reader, baseType);
            }
        }

        //
        // Returns the *directly implemented* interfaces as typedefs, specs or refs. ImplementedInterfaces will take care of the transitive closure and
        // insertion of the TypeContext.
        //
        internal sealed override QTypeDefRefOrSpec[] TypeRefDefOrSpecsForDirectlyImplementedInterfaces
        {
            get
            {
                LowLevelList<QTypeDefRefOrSpec> directlyImplementedInterfaces = new LowLevelList<QTypeDefRefOrSpec>();
                foreach (Handle ifcHandle in _typeDefinition.Interfaces)
                    directlyImplementedInterfaces.Add(new QTypeDefRefOrSpec(_reader, ifcHandle));
                return directlyImplementedInterfaces.ToArray();
            }
        }

        internal MetadataReader Reader
        {
            get
            {
                return _reader;
            }
        }

        internal TypeDefinitionHandle TypeDefinitionHandle
        {
            get
            {
                return _typeDefinitionHandle;
            }
        }

        internal EventHandleCollection DeclaredEventHandles
        {
            get
            {
                return _typeDefinition.Events;
            }
        }

        internal FieldHandleCollection DeclaredFieldHandles
        {
            get
            {
                return _typeDefinition.Fields;
            }
        }

        internal MethodHandleCollection DeclaredMethodAndConstructorHandles
        {
            get
            {
                return _typeDefinition.Methods;
            }
        }

        internal PropertyHandleCollection DeclaredPropertyHandles
        {
            get
            {
                return _typeDefinition.Properties;
            }
        }

        public bool Equals(NativeFormatRuntimeNamedTypeInfo? other)
        {
            // RuntimeTypeInfo.Equals(object) is the one that encapsulates our unification strategy so defer to him.
            object? otherAsObject = other;
            return base.Equals(otherAsObject);
        }

        internal sealed override QTypeDefRefOrSpec TypeDefinitionQHandle
        {
            get
            {
                return new QTypeDefRefOrSpec(_reader, _typeDefinitionHandle, true);
            }
        }

        private readonly MetadataReader _reader;
        private readonly TypeDefinitionHandle _typeDefinitionHandle;
        private readonly TypeDefinition _typeDefinition;

        private NamespaceChain NamespaceChain
        {
            get
            {
                return _lazyNamespaceChain ??= new NamespaceChain(_reader, _typeDefinition.NamespaceDefinition);
            }
        }

        private volatile NamespaceChain _lazyNamespaceChain;
    }
}
