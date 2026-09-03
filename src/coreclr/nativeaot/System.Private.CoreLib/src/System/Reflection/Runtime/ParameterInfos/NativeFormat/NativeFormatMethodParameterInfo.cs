// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.General.NativeFormat;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.ParameterInfos.NativeFormat
{
    //
    // This implements ParameterInfo objects owned by MethodBase objects that have an associated Parameter metadata entity.
    //
    internal sealed partial class NativeFormatMethodParameterInfo : RuntimeFatMethodParameterInfo
    {
        private NativeFormatMethodParameterInfo(MethodBase member, int position, ParameterHandle parameterHandle, QSignatureTypeHandle qualifiedParameterTypeHandle, TypeContext typeContext)
            : base(member, position, qualifiedParameterTypeHandle, typeContext)
        {
            _parameter = parameterHandle.GetParameter(Reader);
        }

        private MetadataReader Reader
        {
            get
            {
                Debug.Assert(QualifiedParameterTypeHandle.Reader is MetadataReader);
                return (MetadataReader)QualifiedParameterTypeHandle.Reader;
            }
        }

        public sealed override ParameterAttributes Attributes
        {
            get
            {
                return _parameter.Flags;
            }
        }

        public sealed override string Name
        {
            get
            {
                return _parameter.Name.GetStringOrNull(this.Reader);
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        internal sealed override MetadataReader GetMetadataReader() => Reader;

        internal sealed override CustomAttributeHandleCollection GetCustomAttributeHandles() => _parameter.CustomAttributes;

        protected sealed override bool GetDefaultValueIfAvailable(bool raw, out object? defaultValue)
        {
            if (DefaultValueParser.GetDefaultValueFromConstantIfAny(Reader, _parameter.DefaultValue, ParameterType, raw, out defaultValue))
                return true;

            defaultValue = raw ? GetDefaultValueFromCustomAttributeData() : GetDefaultValueFromCustomAttributes();
            if (defaultValue != DBNull.Value)
                return true;

            defaultValue = null;
            return false;
        }

        private object? GetDefaultValueFromCustomAttributeData()
        {
            foreach (CustomAttributeData attributeData in GetCustomAttributesData())
            {
                Type attributeType = attributeData.AttributeType;
                if (attributeType == typeof(DecimalConstantAttribute))
                {
                    return GetRawDecimalConstant(attributeData);
                }
                else if (attributeType.IsSubclassOf(typeof(CustomConstantAttribute)))
                {
                    if (attributeType == typeof(DateTimeConstantAttribute))
                    {
                        return GetRawDateTimeConstant(attributeData);
                    }
                    return GetRawConstant(attributeData);
                }
            }
            return DBNull.Value;
        }

        private object? GetDefaultValueFromCustomAttributes()
        {
            object[] customAttributes = GetCustomAttributes(typeof(CustomConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((CustomConstantAttribute)customAttributes[0]).Value;

            customAttributes = GetCustomAttributes(typeof(DecimalConstantAttribute), false);
            if (customAttributes.Length != 0)
                return ((DecimalConstantAttribute)customAttributes[0]).Value;

            return DBNull.Value;
        }

        private static decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.Constructor.DeclaringType == typeof(DecimalConstantAttribute));
            IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;
            Debug.Assert(args.Count == 5);

            return new decimal(
                lo: GetConstructorArgument(args, 4),
                mid: GetConstructorArgument(args, 3),
                hi: GetConstructorArgument(args, 2),
                isNegative: ((byte)args[1].Value!) != 0,
                scale: (byte)args[0].Value!);

            static int GetConstructorArgument(IList<CustomAttributeTypedArgument> args, int index)
            {
                // The constructor is overloaded to accept both signed and unsigned arguments
                object obj = args[index].Value!;
                return (obj is int value) ? value : (int)(uint)obj;
            }
        }

        private static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.Constructor.DeclaringType == typeof(DateTimeConstantAttribute));
            Debug.Assert(attr.ConstructorArguments.Count == 1);

            return new DateTime((long)attr.ConstructorArguments[0].Value!);
        }

        private static object? GetRawConstant(CustomAttributeData attr)
        {
            Debug.Assert(attr.AttributeType.IsSubclassOf(typeof(CustomConstantAttribute)));

            // We are relying only on named arguments for historical reasons
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }
            return DBNull.Value;
        }

        private readonly Parameter _parameter;
    }
}
