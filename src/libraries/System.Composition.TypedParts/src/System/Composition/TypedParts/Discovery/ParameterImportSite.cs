// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    internal sealed class ParameterImportSite
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
