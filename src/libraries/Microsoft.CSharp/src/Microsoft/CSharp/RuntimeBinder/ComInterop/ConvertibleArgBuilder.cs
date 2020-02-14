// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System.Linq.Expressions;

using System;
using System.Globalization;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop {

    internal class ConvertibleArgBuilder : ArgBuilder {

        internal override Expression Marshal(Expression parameter) {
            return Helpers.Convert(parameter, typeof(IConvertible));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            //we are not supporting convertible InOut
            throw Assert.Unreachable;
        }
    }
}

#endif
