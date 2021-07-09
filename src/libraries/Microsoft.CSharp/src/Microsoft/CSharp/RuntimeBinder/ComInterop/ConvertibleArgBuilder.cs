// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class ConvertibleArgBuilder : ArgBuilder
    {
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression Marshal(Expression parameter)
        {
            return Helpers.Convert(parameter, typeof(IConvertible));
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal override Expression MarshalToRef(Expression parameter)
        {
            //we are not supporting convertible InOut
            throw new NotSupportedException();
        }
    }
}
