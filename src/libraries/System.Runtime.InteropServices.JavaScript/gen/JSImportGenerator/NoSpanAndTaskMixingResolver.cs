// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class NoSpanAndTaskMixingResolver : IMarshallingGeneratorResolver
    {
        private bool _hasSpan;
        private bool _hasTask;

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            bool foundInteresting = false;
            if (info.MarshallingAttributeInfo is JSMarshallingInfo(_, JSSpanTypeInfo))
            {
                _hasSpan = true;
                foundInteresting = true;
            }

            if (info.MarshallingAttributeInfo is JSMarshallingInfo(_, JSTaskTypeInfo) && info.IsManagedReturnPosition)
            {
                _hasTask = true;
                foundInteresting = true;
            }

            if (foundInteresting && _hasSpan && _hasTask)
            {
                return ResolvedGenerator.NotSupported(info, context,
                    new GeneratorDiagnostic.NotSupported(info)
                    {
                        NotSupportedDetails = SR.SpanAndTaskNotSupported
                    });
            }

            return ResolvedGenerator.UnresolvedGenerator;
        }
    }
}
