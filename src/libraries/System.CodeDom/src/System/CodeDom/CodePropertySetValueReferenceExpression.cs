// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodePropertySetValueReferenceExpression : CodeExpression
#else
    public class CodePropertySetValueReferenceExpression : CodeExpression
#endif
    {
    }
}
