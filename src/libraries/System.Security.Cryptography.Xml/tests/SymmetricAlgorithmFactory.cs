// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Xml.Tests
{
    public class SymmetricAlgorithmFactory
    {
        Func<SymmetricAlgorithm> _constructor;
        string _name;

        public SymmetricAlgorithmFactory(string name, Func<SymmetricAlgorithm> constructor)
        {
            _name = name;
            _constructor = constructor;
        }

        public SymmetricAlgorithm Create()
        {
            return _constructor();
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
