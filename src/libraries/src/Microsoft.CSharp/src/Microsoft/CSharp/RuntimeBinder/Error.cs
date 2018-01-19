// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class Error
    {
        internal static Exception InternalCompilerError()
        {
            return new RuntimeBinderInternalCompilerException(SR.InternalCompilerError);
        }

        internal static Exception BindPropertyFailedMethodGroup(object p0)
        {
            return new RuntimeBinderException(SR.Format(SR.BindPropertyFailedMethodGroup, p0));
        }

        internal static Exception BindPropertyFailedEvent(object p0)
        {
            return new RuntimeBinderException(SR.Format(SR.BindPropertyFailedEvent, p0));
        }

        internal static Exception BindInvokeFailedNonDelegate()
        {
            return new RuntimeBinderException(SR.BindInvokeFailedNonDelegate);
        }

        internal static Exception BindStaticRequiresType(string paramName) =>
            new ArgumentException(SR.TypeArgumentRequiredForStaticCall, paramName);

        internal static Exception NullReferenceOnMemberException()
        {
            return new RuntimeBinderException(SR.NullReferenceOnMemberException);
        }

        internal static Exception BindCallToConditionalMethod(object p0)
        {
            return new RuntimeBinderException(SR.Format(SR.BindCallToConditionalMethod, p0));
        }

        internal static Exception BindToVoidMethodButExpectResult()
        {
            return new RuntimeBinderException(SR.BindToVoidMethodButExpectResult);
        }

        internal static Exception ArgumentNull(string paramName) => new ArgumentNullException(paramName);

        internal static Exception DynamicArgumentNeedsValue(string paramName) =>
            new ArgumentException(SR.DynamicArgumentNeedsValue, paramName);

        internal static Exception BindingNameCollision() => new RuntimeBinderException(SR.BindingNameCollision);
    }
}
