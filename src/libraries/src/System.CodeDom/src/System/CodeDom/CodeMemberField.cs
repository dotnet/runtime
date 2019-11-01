// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeMemberField : CodeTypeMember
    {
        private CodeTypeReference _type;

        public CodeMemberField() { }

        public CodeMemberField(CodeTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }

        public CodeMemberField(string type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeMemberField(Type type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        public CodeTypeReference Type
        {
            get => _type ?? (_type = new CodeTypeReference(""));
            set => _type = value;
        }

        public CodeExpression InitExpression { get; set; }
    }
}
