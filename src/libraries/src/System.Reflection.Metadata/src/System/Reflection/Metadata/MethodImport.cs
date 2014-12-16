// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct MethodImport
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