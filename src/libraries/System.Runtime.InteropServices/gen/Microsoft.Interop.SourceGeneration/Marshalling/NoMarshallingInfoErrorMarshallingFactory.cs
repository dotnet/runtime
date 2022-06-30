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

        public IMarshallingGenerator Create(
            TypePositionInfo info,
            StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is NoMarshallingInfo && CustomTypeToErrorMessageMap.TryGetValue(info.ManagedType, out string errorMessage))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = errorMessage
                };
            }
            return _inner.Create(info, context);
        }

        public NoMarshallingInfoErrorMarshallingFactory(IMarshallingGeneratorFactory inner)
            : this(inner, DefaultTypeToErrorMessageMap)
        {
        }

        private NoMarshallingInfoErrorMarshallingFactory(IMarshallingGeneratorFactory inner, ImmutableDictionary<ManagedTypeInfo, string> customTypeToErrorMessageMap)
        {
            _inner = inner;
            CustomTypeToErrorMessageMap = customTypeToErrorMessageMap;
        }

        public ImmutableDictionary<ManagedTypeInfo, string> CustomTypeToErrorMessageMap { get; }

        private static ImmutableDictionary<ManagedTypeInfo, string> DefaultTypeToErrorMessageMap { get; } =
            ImmutableDictionary.CreateRange(new Dictionary<ManagedTypeInfo, string>
            {
                { SpecialTypeInfo.String, SR.MarshallingStringOrCharAsUndefinedNotSupported },
                { SpecialTypeInfo.Boolean, SR.MarshallingBoolAsUndefinedNotSupported },
            });
    }
}
