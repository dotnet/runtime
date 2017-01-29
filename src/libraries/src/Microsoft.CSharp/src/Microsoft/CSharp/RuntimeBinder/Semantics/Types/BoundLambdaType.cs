// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // BoundLambdaType - a placeholder typesym used only as the type of an anonymous 
    // method expression. There is exactly one of these.
    // ----------------------------------------------------------------------------

    internal sealed class BoundLambdaType : CType
    { };
}
