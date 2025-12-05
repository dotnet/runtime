// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;

namespace ILAssembler
{
    internal sealed record SignatureArg(ParameterAttributes Attributes, BlobBuilder SignatureBlob, BlobBuilder MarshallingDescriptor, string? Name)
    {
        public bool IsSentinel { get; private init; }

        public static SignatureArg CreateSentinelArgument()
        {
            var sentinelBlob = new BlobBuilder(1);
            sentinelBlob.WriteByte((byte)SignatureTypeCode.Sentinel);
            return new SignatureArg(ParameterAttributes.None, sentinelBlob, new BlobBuilder(0), null)
            {
                IsSentinel = true
            };
        }
    }
}
