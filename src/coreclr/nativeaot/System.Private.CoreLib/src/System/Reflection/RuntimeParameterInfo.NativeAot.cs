// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System.Reflection;

internal sealed partial class RuntimeParameterInfo : ParameterInfo
{
    private readonly ParameterHandle _tkParamDef;
    private readonly MetadataReader? _scope;
    private readonly QSignatureTypeHandle _signature;
    private readonly TypeContext _typeContext;

    internal static RuntimeParameterInfo[] GetParameters<TRuntimeMethodCommon>(
        ref TRuntimeMethodCommon method, MethodBase member, RuntimeTypeInfo[] methodTypeArguments)
        where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
    {
        return GetParameters(ref method, member, methodTypeArguments, out _, fetchReturnParameter: false);
    }

    internal static RuntimeParameterInfo GetReturnParameter<TRuntimeMethodCommon>(
        ref TRuntimeMethodCommon method, MethodBase member, RuntimeTypeInfo[] methodTypeArguments)
        where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
    {
        GetParameters(ref method, member, methodTypeArguments, out RuntimeParameterInfo? returnParameter, fetchReturnParameter: true);
        Debug.Assert(returnParameter is not null);
        return returnParameter;
    }

    private static RuntimeParameterInfo[] GetParameters<TRuntimeMethodCommon>(
        ref TRuntimeMethodCommon method, MethodBase member, RuntimeTypeInfo[] methodTypeArguments,
        out RuntimeParameterInfo? returnParameter, bool fetchReturnParameter)
        where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
    {
        TypeContext typeContext = member.DeclaringType.ToRuntimeTypeInfo().TypeContext;
        typeContext = new TypeContext(typeContext.GenericTypeArguments, methodTypeArguments);
        QSignatureTypeHandle[] sig = method.QualifiedMethodSignature;
        Debug.Assert(sig.Length > 0);

        returnParameter = null;
        int sigArgCount = sig.Length - 1;
        RuntimeParameterInfo[] args =
            fetchReturnParameter ? null! :
            sigArgCount == 0 ? [] :
            new RuntimeParameterInfo[sigArgCount];

        MetadataReader scope = method.GetMetadataReader();
        ParameterHandleCollection parameterHandles = method.ParameterHandles;
        int cParamDefs = parameterHandles.Count;
        if (cParamDefs > sigArgCount + 1)
            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

        foreach (ParameterHandle parameterHandle in parameterHandles)
        {
            Parameter parameter = scope.GetParameter(parameterHandle);
            int position = parameter.Sequence - 1;

            if (fetchReturnParameter && position == -1)
            {
                if (returnParameter is not null)
                    throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                returnParameter = new RuntimeParameterInfo(sig[0], typeContext, scope, parameterHandle, position, parameter.Flags, member);
            }
            else if (!fetchReturnParameter && position >= 0)
            {
                if (position >= sigArgCount)
                    throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                args[position] = new RuntimeParameterInfo(sig[position + 1], typeContext, scope, parameterHandle, position, parameter.Flags, member);
            }
        }

        if (fetchReturnParameter)
        {
            returnParameter ??= new RuntimeParameterInfo(sig[0], typeContext, null, default, -1, ParameterAttributes.None, member);
        }
        else if (cParamDefs < args.Length + 1)
        {
            for (int i = 0; i < args.Length; i++)
            {
                args[i] ??= new RuntimeParameterInfo(sig[i + 1], typeContext, null, default, i, ParameterAttributes.None, member);
            }
        }

        return args;
    }

    private RuntimeParameterInfo(
        QSignatureTypeHandle signature, TypeContext typeContext, MetadataReader? scope, ParameterHandle parameterHandle,
        int position, ParameterAttributes attributes, MemberInfo member)
    {
        Debug.Assert(parameterHandle.IsNil == (scope is null));

        PositionImpl = position;
        MemberImpl = member;
        _signature = signature;
        _typeContext = typeContext;
        _tkParamDef = parameterHandle;
        _scope = scope;
        AttrsImpl = attributes;
    }

    // Array members have no metadata signatures, but otherwise behave like ordinary tokenless parameters.
    internal RuntimeParameterInfo(MemberInfo owner, Type parameterType, int position)
    {
        MemberImpl = owner;
        ClassImpl = parameterType;
        PositionImpl = position;
        _nameIsCached = true;
    }

    private object? GetDefaultValueFromMetadata(bool raw)
    {
        Debug.Assert(_scope is not null && !_tkParamDef.IsNil);
        Type parameterType = ParameterType;
        Handle constantHandle = _scope.GetParameter(_tkParamDef).DefaultValue;
        if (constantHandle.IsNil)
            return DBNull.Value;

        object? value = constantHandle.ParseConstantValue(_scope);
        if (parameterType.IsEnum && !raw)
        {
            if (value is null)
                return null;

            if (value is not (char or sbyte or byte or short or ushort or int or uint or long or ulong))
                throw new FormatException(SR.Arg_BadLiteralFormat);

            if (!parameterType.ContainsGenericParameters)
                return Enum.ToObject(parameterType, value);
        }
        else if (parameterType == typeof(DateTime))
        {
            return value switch
            {
                null => null,
                long ticks => new DateTime(ticks),
                ulong ticks => new DateTime(unchecked((long)ticks)),
                _ => throw new FormatException(SR.Arg_BadLiteralFormat),
            };
        }

        return value;
    }

    internal MetadataReader? GetMetadataReader() => _scope;

    internal CustomAttributeHandleCollection GetCustomAttributeHandles()
    {
        if (_tkParamDef.IsNil)
            return default;

        Debug.Assert(_scope is not null);
        return _scope.GetParameter(_tkParamDef).CustomAttributes;
    }

    public override int MetadataToken => _tkParamDef.IsNil
        ? base.MetadataToken
        : throw new InvalidOperationException(SR.NoMetadataTokenAvailable);

    public override Type[] GetRequiredCustomModifiers() =>
        _signature.Reader is null ? [] : _signature.GetCustomModifiers(_typeContext, optional: false);

    public override Type[] GetOptionalCustomModifiers() =>
        _signature.Reader is null ? [] : _signature.GetCustomModifiers(_typeContext, optional: true);

    public override Type GetModifiedParameterType() =>
        _signature.GetModifiedType(ParameterType);
}
