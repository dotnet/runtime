// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class UnsupportedMarshallingFactory : IMarshallingGeneratorFactory
    {
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
            throw new MarshallingNotSupportedException(info, context);
        }

        public UnsupportedMarshallingFactory()
            : this(DefaultTypeToErrorMessageMap)
        {

        }

        private UnsupportedMarshallingFactory(ImmutableDictionary<ManagedTypeInfo, string> customTypeToErrorMessageMap)
        {
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
