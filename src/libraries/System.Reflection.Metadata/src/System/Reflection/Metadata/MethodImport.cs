// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct MethodImport
    {
        private readonly MethodImportAttributes attributes;
        private readonly StringHandle name;
        private readonly ModuleReferenceHandle module;

        internal MethodImport(MethodImportAttributes attributes, StringHandle name, ModuleReferenceHandle module)
        {
            this.attributes = attributes;
            this.name = name;
            this.module = module;
        }

        public MethodImportAttributes Attributes { get { return attributes; } }
        public StringHandle Name { get { return name; } }
        public ModuleReferenceHandle Module { get { return module; } }
    }
}