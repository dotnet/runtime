// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.General.EcmaFormat;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.EcmaFormat;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.EcmaFormat;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Runtime.PropertyInfos.EcmaFormat
{
    //
    // The runtime's implementation of PropertyInfo's
    //
    [DebuggerDisplay("{_debugName}")]
    internal sealed partial class EcmaFormatRuntimePropertyInfo : RuntimePropertyInfo
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
        private EcmaFormatRuntimePropertyInfo(PropertyDefinitionHandle propertyHandle, EcmaFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType) :
            base(contextTypeInfo, reflectedType)
        {
            _propertyHandle = propertyHandle;
            _definingTypeInfo = definingTypeInfo;
            _reader = definingTypeInfo.Reader;
            _property = _reader.GetPropertyDefinition(propertyHandle);
        }

        public sealed override PropertyAttributes Attributes
        {
            get
            {
                return _property.Attributes;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return RuntimeCustomAttributeData.GetCustomAttributes(_reader, _property.GetCustomAttributes());
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is EcmaFormatRuntimePropertyInfo otherProperty))
                return false;
            if (!(_reader == otherProperty._reader))
                return false;
            if (!(_propertyHandle.Equals(otherProperty._propertyHandle)))
                return false;
            return true;
        }

        public sealed override bool Equals(Object obj)
        {
            if (!(obj is EcmaFormatRuntimePropertyInfo other))
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
                return MetadataTokens.GetToken(_propertyHandle);
            }
        }

        protected sealed override QSignatureTypeHandle PropertyTypeHandle
        {
            get
            {
                return new QSignatureTypeHandle(_reader, _reader.GetBlobReader(_property.Signature));
            }
        }

        protected sealed override bool GetDefaultValueIfAny(bool raw, out object defaultValue)
        {
            return DefaultValueProcessing.GetDefaultValueIfAny(_reader, ref _property, this, raw, out defaultValue);
        }

        protected sealed override RuntimeNamedMethodInfo GetPropertyMethod(PropertyMethodSemantics whichMethod)
        {
            MethodDefinitionHandle methodHandle;
            PropertyAccessors propertyAccessors = _property.GetAccessors();

            switch (whichMethod)
            {
                case PropertyMethodSemantics.Getter:
                    methodHandle = propertyAccessors.Getter;
                    break;

                case PropertyMethodSemantics.Setter:
                    methodHandle = propertyAccessors.Setter;
                    break;

                default:
                    return null;
            }

            bool inherited = !_reflectedType.Equals(ContextTypeInfo);
            if (inherited)
            {
                MethodAttributes flags = _reader.GetMethodDefinition(methodHandle).Attributes;
                if ((flags & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                    return null;
            }

            return RuntimeNamedMethodInfo<EcmaFormatMethodCommon>.GetRuntimeNamedMethodInfo(new EcmaFormatMethodCommon(methodHandle, _definingTypeInfo, ContextTypeInfo), _reflectedType);
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

        private readonly EcmaFormatRuntimeNamedTypeInfo _definingTypeInfo;
        private readonly PropertyDefinitionHandle _propertyHandle;

        private readonly MetadataReader _reader;
        private PropertyDefinition _property;
    }
}
