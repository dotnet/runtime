﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal class ConvertibleArgBuilder : ArgBuilder
    {
        internal override Expression Marshal(Expression parameter)
        {
            return Helpers.Convert(parameter, typeof(IConvertible));
        }

        internal override Expression MarshalToRef(Expression parameter)
        {
            //we are not supporting convertible InOut
            throw new NotSupportedException();
        }
    }
}
