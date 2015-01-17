// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    internal class ParameterImportSite
    {
        private readonly ParameterInfo _pi;

        public ParameterImportSite(ParameterInfo pi)
        {
            _pi = pi;
        }

        public ParameterInfo Parameter { get { return _pi; } }

        public override string ToString()
        {
            return _pi.Name;
        }
    }
}
