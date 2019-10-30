// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all PropertyInfo objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed class EcmaProperty : RoProperty
    {
        private readonly EcmaModule _module;
        private readonly PropertyDefinitionHandle _handle;

        internal EcmaProperty(RoInstantiationProviderType declaringType, PropertyDefinitionHandle handle, Type reflectedType)
            : base(declaringType, reflectedType)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(declaringType != null);
            Debug.Assert(reflectedType != null);
            Debug.Assert(declaringType.Module is EcmaModule);

            _handle = handle;
            _module = (EcmaModule)(declaringType.Module);
            _neverAccessThisExceptThroughPropertyDefinitionProperty = handle.GetPropertyDefinition(Reader);
        }

        internal sealed override RoModule GetRoModule() => _module;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => PropertyDefinition.GetCustomAttributes().ToTrueCustomAttributes(_module);

        public sealed override int MetadataToken => _handle.GetToken();

        public sealed override bool Equals(object obj)
        {
            if (!(obj is EcmaProperty other))
                return false;

            if (_handle != other._handle)
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            if (ReflectedType != other.ReflectedType)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => _handle.GetHashCode() ^ DeclaringType.GetHashCode();

        protected sealed override string ComputeName() => PropertyDefinition.Name.GetString(Reader);
        protected sealed override PropertyAttributes ComputeAttributes() => PropertyDefinition.Attributes;
        protected sealed override Type ComputePropertyType() => PropertyDefinition.DecodeSignature(_module, TypeContext).ReturnType;

        protected sealed override object ComputeRawConstantValue() => PropertyDefinition.GetDefaultValue().ToRawObject(Reader);

        public sealed override Type[] GetOptionalCustomModifiers() => GetCustomModifiers(isRequired: false);
        public sealed override Type[] GetRequiredCustomModifiers() => GetCustomModifiers(isRequired: true);

        private Type[] GetCustomModifiers(bool isRequired)
        {
            RoType type = PropertyDefinition.DecodeSignature(new EcmaModifiedTypeProvider(_module), TypeContext).ReturnType;
            return type.ExtractCustomModifiers(isRequired);
        }

        public sealed override string ToString()
        {
            string disposedString = Loader.GetDisposedString();
            if (disposedString != null)
                return disposedString;

            StringBuilder sb = new StringBuilder();
            ISignatureTypeProvider<string, TypeContext> typeProvider = EcmaSignatureTypeProviderForToString.Instance;
            MethodSignature<string> sig = PropertyDefinition.DecodeSignature(typeProvider, TypeContext);
            sb.Append(sig.ReturnType);
            sb.Append(' ');
            sb.Append(Name);
            if (sig.ParameterTypes.Length != 0)
            {
                sb.Append('[');
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append(sig.ParameterTypes[i]);
                }
                sb.Append(']');
            }
            return sb.ToString();
        }

        protected sealed override RoMethod ComputeGetterMethod() => PropertyDefinition.GetAccessors().Getter.ToMethodOrNull(GetRoDeclaringType(), ReflectedType);
        protected sealed override RoMethod ComputeSetterMethod() => PropertyDefinition.GetAccessors().Setter.ToMethodOrNull(GetRoDeclaringType(), ReflectedType);

        private MetadataReader Reader => _module.Reader;
        private MetadataLoadContext Loader => GetRoModule().Loader;

        private ref readonly PropertyDefinition PropertyDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughPropertyDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly PropertyDefinition _neverAccessThisExceptThroughPropertyDefinitionProperty;
    }
}
