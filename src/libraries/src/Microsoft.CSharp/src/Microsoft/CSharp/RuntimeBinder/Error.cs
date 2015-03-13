// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class Error
    {
        internal static Exception InternalCompilerError()
        {
            return new RuntimeBinderInternalCompilerException(SR.InternalCompilerError);
        }

        internal static Exception BindRequireArguments()
        {
            return new ArgumentException(SR.BindRequireArguments);
        }

        internal static Exception BindCallFailedOverloadResolution()
        {
            return new RuntimeBinderException(SR.BindCallFailedOverloadResolution);
        }

        internal static Exception BindBinaryOperatorRequireTwoArguments()
        {
            return new ArgumentException(SR.BindBinaryOperatorRequireTwoArguments);
        }

        internal static Exception BindUnaryOperatorRequireOneArgument()
        {
            return new ArgumentException(SR.BindUnaryOperatorRequireOneArgument);
        }

        internal static Exception BindBinaryAssignmentRequireTwoArguments()
        {
            return new ArgumentException(SR.BindBinaryAssignmentRequireTwoArguments);
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

        internal static Exception BindImplicitConversionRequireOneArgument()
        {
            return new ArgumentException(SR.BindImplicitConversionRequireOneArgument);
        }

        internal static Exception BindExplicitConversionRequireOneArgument()
        {
            return new ArgumentException(SR.BindExplicitConversionRequireOneArgument);
        }

        internal static Exception BindBinaryAssignmentFailedNullReference()
        {
            return new RuntimeBinderException(SR.BindBinaryAssignmentFailedNullReference);
        }

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
    }
}
