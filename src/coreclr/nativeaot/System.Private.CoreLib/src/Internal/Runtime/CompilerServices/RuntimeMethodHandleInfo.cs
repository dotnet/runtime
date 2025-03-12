// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;

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

            if (GetName() != other.GetName())
                return false;

            // Comparing handles is enough if there's only one metadata blob
            Debug.Assert(Reader == other.Reader);
            return Reader.GetMethod(Handle).Signature.Equals(other.Reader.GetMethod(other.Handle).Signature);
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }
    }
}
