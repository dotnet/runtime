// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Metadata.NativeFormat;

namespace Internal.Runtime.CompilerServices
{
    [CLSCompliant(false)]
    public class MethodNameAndSignature
    {
        public MetadataReader Reader { get; }
        public MethodHandle Handle { get; }

        public MethodNameAndSignature(MetadataReader reader, MethodHandle handle)
        {
            Reader = reader;
            Handle = handle;
        }

        public string GetName()
        {
            Method method = Reader.GetMethod(Handle);
            return Reader.GetString(method.Name);
        }

        public override bool Equals(object? compare)
        {
            if (compare == null)
                return false;

            MethodNameAndSignature? other = compare as MethodNameAndSignature;
            if (other == null)
                return false;

            // Comparing handles is enough if there's only one metadata blob
            // (Same assumption in GetHashCode below!)
            Debug.Assert(Reader == other.Reader);

            Method thisMethod = Reader.GetMethod(Handle);
            Method otherMethod = other.Reader.GetMethod(other.Handle);

            return thisMethod.Signature.Equals(otherMethod.Signature)
                && thisMethod.Name.Equals(otherMethod.Name);
        }

        public override int GetHashCode()
        {
            Method method = Reader.GetMethod(Handle);

            // Assumes we only have one metadata blob
            return method.Signature.GetHashCode() ^ method.Name.GetHashCode();
        }
    }
}
