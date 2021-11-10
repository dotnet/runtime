// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all FieldInfo objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed class EcmaField : RoField
    {
        private readonly EcmaModule _module;
        private readonly FieldDefinitionHandle _handle;

        internal EcmaField(RoInstantiationProviderType declaringType, FieldDefinitionHandle handle, Type reflectedType)
            : base(declaringType, reflectedType)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(declaringType != null);
            Debug.Assert(reflectedType != null);
            Debug.Assert(declaringType.Module is EcmaModule);

            _handle = handle;
            _module = (EcmaModule)(declaringType.Module);
            _neverAccessThisExceptThroughFieldDefinitionProperty = handle.GetFieldDefinition(Reader);
        }

        internal sealed override RoModule GetRoModule() => _module;

        protected sealed override IEnumerable<CustomAttributeData> GetTrueCustomAttributes() => FieldDefinition.GetCustomAttributes().ToTrueCustomAttributes(_module);

        protected sealed override int GetExplicitFieldOffset() => FieldDefinition.GetOffset();
        protected sealed override MarshalAsAttribute ComputeMarshalAsAttribute() => FieldDefinition.GetMarshallingDescriptor().ToMarshalAsAttribute(_module);

        public sealed override int MetadataToken => _handle.GetToken();

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is EcmaField other))
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

        protected sealed override string ComputeName() => FieldDefinition.Name.GetString(Reader);
        protected sealed override FieldAttributes ComputeAttributes() => FieldDefinition.Attributes;
        protected sealed override Type ComputeFieldType() => FieldDefinition.DecodeSignature(_module, TypeContext);

        public sealed override Type[] GetOptionalCustomModifiers() => GetCustomModifiers(isRequired: false);
        public sealed override Type[] GetRequiredCustomModifiers() => GetCustomModifiers(isRequired: true);

        private Type[] GetCustomModifiers(bool isRequired)
        {
            RoType type = FieldDefinition.DecodeSignature(new EcmaModifiedTypeProvider(_module), TypeContext)!;
            return type.ExtractCustomModifiers(isRequired);
        }

        protected sealed override object? ComputeRawConstantValue() => FieldDefinition.GetDefaultValue().ToRawObject(Reader);

        public sealed override string ToString()
        {
            string? disposedString = Loader.GetDisposedString();
            if (disposedString != null)
                return disposedString;

            return
                FieldDefinition.DecodeSignature(EcmaSignatureTypeProviderForToString.Instance, TypeContext) +
                " " +
                Name;
        }

        private MetadataReader Reader => _module.Reader;
        private MetadataLoadContext Loader => GetRoModule().Loader;

        private ref readonly FieldDefinition FieldDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughFieldDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly FieldDefinition _neverAccessThisExceptThroughFieldDefinitionProperty;
    }
}
