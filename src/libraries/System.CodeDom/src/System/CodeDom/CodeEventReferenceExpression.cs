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
    internal sealed class CodeEventReferenceExpression : CodeExpression
#else
    public class CodeEventReferenceExpression : CodeExpression
#endif
    {
        private string _eventName;

        public CodeEventReferenceExpression() { }

        public CodeEventReferenceExpression(CodeExpression targetObject, string eventName)
        {
            TargetObject = targetObject;
            _eventName = eventName;
        }

        public CodeExpression TargetObject { get; set; }

        public string EventName
        {
            get => _eventName ?? string.Empty;
            set => _eventName = value;
        }
    }
}
