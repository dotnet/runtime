// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public readonly struct MethodImport
    {
        private readonly MethodImportAttributes _attributes;
        private readonly StringHandle _name;
        private readonly ModuleReferenceHandle _module;

        internal MethodImport(MethodImportAttributes attributes, StringHandle name, ModuleReferenceHandle module)
        {
            _attributes = attributes;
            _name = name;
            _module = module;
        }

        public MethodImportAttributes Attributes { get { return _attributes; } }
        public StringHandle Name { get { return _name; } }
        public ModuleReferenceHandle Module { get { return _module; } }
    }
}
