// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class AssemblySignatureKeyAttribute : Attribute
    {
        public AssemblySignatureKeyAttribute(string publicKey, string countersignature)
        {
            PublicKey = publicKey;
            Countersignature = countersignature;
        }

        public string PublicKey { get; }

        public string Countersignature { get; }
    }
}

