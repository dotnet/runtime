// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class NoMarshallingInfoErrorMarshallingFactory : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;

        public ResolvedGenerator Create(
            TypePositionInfo info,
            StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is NoMarshallingInfo && CustomTypeToErrorMessageMap.TryGetValue(info.ManagedType, out string errorMessage))
            {
                return ResolvedGenerator.NotSupported(new(info, context)
                {
                    NotSupportedDetails = errorMessage
                });
            }
            return _inner.Create(info, context);
        }

        public NoMarshallingInfoErrorMarshallingFactory(IMarshallingGeneratorFactory inner, string stringMarshallingAttribute)
            : this(inner, DefaultTypeToErrorMessageMap(stringMarshallingAttribute))
        {
        }

        private NoMarshallingInfoErrorMarshallingFactory(IMarshallingGeneratorFactory inner, ImmutableDictionary<ManagedTypeInfo, string> customTypeToErrorMessageMap)
        {
            _inner = inner;
            CustomTypeToErrorMessageMap = customTypeToErrorMessageMap;
        }

        public ImmutableDictionary<ManagedTypeInfo, string> CustomTypeToErrorMessageMap { get; }

        private static ImmutableDictionary<ManagedTypeInfo, string> DefaultTypeToErrorMessageMap(string stringMarshallingAttribute)
            => ImmutableDictionary.CreateRange(new Dictionary<ManagedTypeInfo, string>
            {
                { SpecialTypeInfo.String, string.Format(SR.MarshallingStringOrCharAsUndefinedNotSupported, stringMarshallingAttribute) },
                { SpecialTypeInfo.Boolean, SR.MarshallingBoolAsUndefinedNotSupported },
            });
    }
}
