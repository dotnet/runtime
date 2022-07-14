// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeMemberEvent : CodeTypeMember
    {
        private CodeTypeReference _type;
        private CodeTypeReferenceCollection _implementationTypes;

        public CodeMemberEvent() { }

        public CodeTypeReference Type
        {
            get => _type ??= new CodeTypeReference("");
            set => _type = value;
        }

        public CodeTypeReference PrivateImplementationType { get; set; }

        public CodeTypeReferenceCollection ImplementationTypes => _implementationTypes ??= new CodeTypeReferenceCollection();
    }
}
