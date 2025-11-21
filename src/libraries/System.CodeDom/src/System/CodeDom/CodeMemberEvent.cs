// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeMemberEvent : CodeTypeMember
    {
        public CodeMemberEvent() { }

        public CodeTypeReference Type
        {
            get => field ??= new CodeTypeReference("");
            set => field = value;
        }

        public CodeTypeReference PrivateImplementationType { get; set; }

        public CodeTypeReferenceCollection ImplementationTypes => field ??= new CodeTypeReferenceCollection();
    }
}
