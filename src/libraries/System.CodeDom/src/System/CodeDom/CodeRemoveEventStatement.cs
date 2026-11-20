// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.CodeDom
{
    public class CodeRemoveEventStatement : CodeStatement
    {
        private CodeEventReferenceExpression? _eventRef;

        public CodeRemoveEventStatement() { }

        public CodeRemoveEventStatement(CodeEventReferenceExpression? eventRef, CodeExpression? listener)
        {
            _eventRef = eventRef;
            Listener = listener;
        }

        public CodeRemoveEventStatement(CodeExpression? targetObject, string? eventName, CodeExpression? listener) :
            this(new CodeEventReferenceExpression(targetObject, eventName), listener)
        {
        }

        [AllowNull]
        public CodeEventReferenceExpression Event
        {
            get => _eventRef ??= new CodeEventReferenceExpression();
            set => _eventRef = value;
        }

        public CodeExpression? Listener { get; set; }
    }
}
