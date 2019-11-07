// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeAttachEventStatement : CodeStatement
    {
        private CodeEventReferenceExpression _eventRef;

        public CodeAttachEventStatement() { }

        public CodeAttachEventStatement(CodeEventReferenceExpression eventRef, CodeExpression listener)
        {
            _eventRef = eventRef;
            Listener = listener;
        }

        public CodeAttachEventStatement(CodeExpression targetObject, string eventName, CodeExpression listener) :
            this(new CodeEventReferenceExpression(targetObject, eventName), listener)
        {
        }

        public CodeEventReferenceExpression Event
        {
            get => _eventRef ?? (_eventRef = new CodeEventReferenceExpression());
            set => _eventRef = value;
        }

        public CodeExpression Listener { get; set; }
    }
}
