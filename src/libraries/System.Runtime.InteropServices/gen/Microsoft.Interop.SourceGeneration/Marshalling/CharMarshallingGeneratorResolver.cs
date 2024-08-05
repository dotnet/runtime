// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class CharMarshallingGeneratorResolver : IMarshallingGeneratorResolver
    {
        private static readonly IMarshallingGenerator s_blittable = new BlittableMarshaller();
        private static readonly IMarshallingGenerator s_utf16Char = new Utf16CharMarshaller();

        private readonly bool _useBlittableMarshallerForUtf16;
        private readonly string _stringMarshallingAttribute;

        public CharMarshallingGeneratorResolver(bool useBlittableMarshallerForUtf16, string stringMarshallingAttribute)
        {
            _useBlittableMarshallerForUtf16 = useBlittableMarshallerForUtf16;
            _stringMarshallingAttribute = stringMarshallingAttribute;
        }

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.ManagedType is SpecialTypeInfo { SpecialType: SpecialType.System_Char })
            {
                return CreateCharMarshaller(info, context);
            }

            return ResolvedGenerator.UnresolvedGenerator;
        }

        private ResolvedGenerator CreateCharMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            MarshallingInfo marshalInfo = info.MarshallingAttributeInfo;
            if (marshalInfo is NoMarshallingInfo)
            {
                // [Compat] Require explicit marshalling information.
                return ResolvedGenerator.NotSupported(new(info, context)
                {
                    NotSupportedDetails = string.Format(SR.MarshallingStringOrCharAsUndefinedNotSupported, _stringMarshallingAttribute)
                });
            }

            // Explicit MarshalAs takes precedence over string encoding info
            if (marshalInfo is MarshalAsInfo marshalAsInfo)
            {
                switch (marshalAsInfo.UnmanagedType)
                {
                    case UnmanagedType.I2:
                    case UnmanagedType.U2:
                        return ResolvedGenerator.Resolved(_useBlittableMarshallerForUtf16 ? s_blittable : s_utf16Char);
                }
            }
            else if (marshalInfo is MarshallingInfoStringSupport marshalStringInfo)
            {
                switch (marshalStringInfo.CharEncoding)
                {
                    case CharEncoding.Utf16:
                        return ResolvedGenerator.Resolved(_useBlittableMarshallerForUtf16 ? s_blittable : s_utf16Char);
                    case CharEncoding.Utf8:
                        return ResolvedGenerator.NotSupported(new(info, context) // [Compat] UTF-8 is not supported for char
                        {
                            NotSupportedDetails = SR.Format(SR.MarshallingCharAsSpecifiedStringMarshallingNotSupported, nameof(CharEncoding.Utf8))
                        });
                    case CharEncoding.Custom:
                        return ResolvedGenerator.NotSupported(new(info, context)
                        {
                            NotSupportedDetails = SR.MarshallingCharAsStringMarshallingCustomNotSupported
                        });
                }
            }

            return ResolvedGenerator.NotSupported(new(info, context));
        }
    }
}
