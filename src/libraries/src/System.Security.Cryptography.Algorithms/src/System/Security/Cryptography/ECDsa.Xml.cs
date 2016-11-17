// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : AsymmetricAlgorithm
    {
        // There is currently not a standard XML format for ECC keys, so we will not implement the default
        // To/FromXmlString so that we're not tied to one format when a standard one does exist. Instead we'll
        // use an overload which allows the user to specify the format they'd like to serialize into.
        public override void FromXmlString(string xmlString)
        {
            throw new NotImplementedException(SR.Cryptography_ECXmlSerializationFormatRequired);
        }

        public override string ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException(SR.Cryptography_ECXmlSerializationFormatRequired);
        }
    }
}
