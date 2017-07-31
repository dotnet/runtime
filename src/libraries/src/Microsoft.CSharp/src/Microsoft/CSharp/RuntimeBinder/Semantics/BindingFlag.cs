// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    [Flags]
    internal enum BindingFlag
    {
        BIND_RVALUEREQUIRED = 0x0001, // this is a get of expr, not an assignment to expr
        BIND_MEMBERSET = 0x0002, // indicates that an lvalue is needed
        BIND_FIXEDVALUE = 0x0010, // ok to take address of unfixed
        BIND_ARGUMENTS = 0x0020, // this is an argument list to a call...
        BIND_BASECALL = 0x0040, // this is a base method or prop call
        BIND_USINGVALUE = 0x0080, // local in a using stmt decl
        BIND_STMTEXPRONLY = 0x0100, // only allow expressions that are valid in a statement
        BIND_TYPEOK = 0x0200, // types are ok to be returned
        BIND_MAYBECONFUSEDNEGATIVECAST = 0x0400, // this may be a mistaken negative cast
        BIND_METHODNOTOK = 0x0800, // naked methods are not ok to be returned
        BIND_DECLNOTOK = 0x1000, // var decls are not ok to be returned
        BIND_NOPARAMS = 0x2000, // Do not do params expansion during overload resolution 
        BIND_SPECULATIVELY = 0x4000, // We're doing a speculative bind.  Don't make any stateful changes that might affect the actual compilation
    }
}
