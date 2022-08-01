// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// An <see cref="IMarshallingGeneratorFactory"/> implementation that wraps an inner <see cref="IMarshallingGeneratorFactory"/> instance and validates that the <see cref="TypePositionInfo.ByValueContentsMarshalKind"/> on the provided <see cref="TypePositionInfo"/> is valid in the current marshalling scenario.
    /// </summary>
    public class ByValueContentsMarshalKindValidator : IMarshallingGeneratorFactory
    {
        private readonly IMarshallingGeneratorFactory _inner;

        public ByValueContentsMarshalKindValidator(IMarshallingGeneratorFactory inner)
        {
            _inner = inner;
        }

        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            return ValidateByValueMarshalKind(info, context, _inner.Create(info, context));
        }

        private static IMarshallingGenerator ValidateByValueMarshalKind(TypePositionInfo info, StubCodeContext context, IMarshallingGenerator generator)
        {
            if (generator is Forwarder)
            {
                // Forwarder allows everything since it just forwards to a P/Invoke.
                return generator;
            }

            if (info.IsByRef && info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.InOutAttributeByRefNotSupported
                };
            }
            else if (info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.In)
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.InAttributeNotSupportedWithoutOut
                };
            }
            else if (info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default
                && !generator.SupportsByValueMarshalKind(info.ByValueContentsMarshalKind, context))
            {
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.InOutAttributeMarshalerNotSupported
                };
            }
            return generator;
        }
    }
}
