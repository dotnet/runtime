// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    public sealed class XmlSchemaCompilationSettings
    {
        private bool _enableUpaCheck;

        public XmlSchemaCompilationSettings()
        {
            _enableUpaCheck = true;
        }

        public bool EnableUpaCheck
        {
            get
            {
                return _enableUpaCheck;
            }
            set
            {
                _enableUpaCheck = value;
            }
        }
    }
}
