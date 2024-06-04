// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.CustomAttributes;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.General.NativeFormat;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

using NativeFormatMethodSemanticsAttributes = global::Internal.Metadata.NativeFormat.MethodSemanticsAttributes;

namespace System.Reflection.Runtime.PropertyInfos.NativeFormat
{
    //
    // The runtime's implementation of PropertyInfo's
    //
    [DebuggerDisplay("{_debugName}")]
    internal sealed partial class NativeFormatRuntimePropertyInfo : RuntimePropertyInfo
    {
        //
        // propertyHandle - the "tkPropertyDef" that identifies the property.
        // definingType   - the "tkTypeDef" that defined the field (this is where you get the metadata reader that created propertyHandle.)
        // contextType    - the type that supplies the type context (i.e. substitutions for generic parameters.) Though you
        //                  get your raw information from "definingType", you report "contextType" as your DeclaringType property.
        //
        //  For example:
        //
        //       typeof(Foo<>).GetTypeInfo().DeclaredMembers
        //
        //           The definingType and contextType are both Foo<>
        //
        //       typeof(Foo<int,String>).GetTypeInfo().DeclaredMembers
        //
        //          The definingType is "Foo<,>"
        //          The contextType is "Foo<int,String>"
        //
        //  We don't report any DeclaredMembers for arrays or generic parameters so those don't apply.
        //
        private NativeFormatRuntimePropertyInfo(PropertyHandle propertyHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType) :
            base(contextTypeInfo, reflectedType)
        {
            _propertyHandle = propertyHandle;
            _definingTypeInfo = definingTypeInfo;
            _reader = definingTypeInfo.Reader;
            _property = propertyHandle.GetProperty(_reader);
        }

        public sealed override PropertyAttributes Attributes
        {
            get
            {
                return _property.Flags;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return RuntimeCustomAttributeData.GetCustomAttributes(_reader, _property.CustomAttributes);
            }
        }

        public override Type GetModifiedPropertyType()
        {
            return ModifiedType.Create(PropertyType, _reader, _reader.GetPropertySignature(_property.Signature).Type);

        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (!(other is NativeFormatRuntimePropertyInfo otherProperty))
                return false;
            if (!(_reader == otherProperty._reader))
                return false;
            if (!(_propertyHandle.Equals(otherProperty._propertyHandle)))
                return false;
            if (!(_definingTypeInfo.Equals(otherProperty._definingTypeInfo)))
                return false;
            return true;
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is NativeFormatRuntimePropertyInfo other))
                return false;
            if (!(_reader == other._reader))
                return false;
            if (!(_propertyHandle.Equals(other._propertyHandle)))
                return false;
            if (!(ContextTypeInfo.Equals(other.ContextTypeInfo)))
                return false;
            if (!(_reflectedType.Equals(other._reflectedType)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return _propertyHandle.GetHashCode();
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        protected sealed override QSignatureTypeHandle PropertyTypeHandle
        {
            get
            {
                return new QSignatureTypeHandle(_reader, _property.Signature.GetPropertySignature(_reader).Type);
            }
        }

        protected sealed override bool GetDefaultValueIfAny(bool raw, out object? defaultValue)
        {
            return DefaultValueParser.GetDefaultValueFromConstantIfAny(_reader, _property.DefaultValue, PropertyType, raw, out defaultValue)
                || DefaultValueParser.GetDefaultValueFromAttributeIfAny(CustomAttributes, raw, out defaultValue);
        }

        protected sealed override RuntimeNamedMethodInfo GetPropertyMethod(PropertyMethodSemantics whichMethod)
        {
            NativeFormatMethodSemanticsAttributes localMethodSemantics;
            switch (whichMethod)
            {
                case PropertyMethodSemantics.Getter:
                    localMethodSemantics = NativeFormatMethodSemanticsAttributes.Getter;
                    break;

                case PropertyMethodSemantics.Setter:
                    localMethodSemantics = NativeFormatMethodSemanticsAttributes.Setter;
                    break;

                default:
                    return null;
            }

            bool inherited = !_reflectedType.Equals(ContextTypeInfo);

            foreach (MethodSemanticsHandle methodSemanticsHandle in _property.MethodSemantics)
            {
                MethodSemantics methodSemantics = methodSemanticsHandle.GetMethodSemantics(_reader);
                if (methodSemantics.Attributes == localMethodSemantics)
                {
                    MethodHandle methodHandle = methodSemantics.Method;

                    if (inherited)
                    {
                        MethodAttributes flags = methodHandle.GetMethod(_reader).Flags;
                        if ((flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                            continue;
                    }

                    return RuntimeNamedMethodInfo<NativeFormatMethodCommon>.GetRuntimeNamedMethodInfo(new NativeFormatMethodCommon(methodHandle, _definingTypeInfo, ContextTypeInfo), _reflectedType);
                }
            }

            return null;
        }

        protected sealed override string MetadataName
        {
            get
            {
                return _property.Name.GetString(_reader);
            }
        }

        protected sealed override RuntimeTypeInfo DefiningTypeInfo
        {
            get
            {
                return _definingTypeInfo;
            }
        }

        private readonly NativeFormatRuntimeNamedTypeInfo _definingTypeInfo;
        private readonly PropertyHandle _propertyHandle;

        private readonly MetadataReader _reader;
        private readonly Property _property;
    }
}
