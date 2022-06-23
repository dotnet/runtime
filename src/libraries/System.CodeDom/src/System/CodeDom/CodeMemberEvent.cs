// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodeMemberEvent : CodeTypeMember
#else
    public class CodeMemberEvent : CodeTypeMember
#endif
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
