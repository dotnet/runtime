// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Contains factory methods to create dynamic call site binders for CSharp.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Binder
    {
        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp binary operation binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="operation">The binary operation kind.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp binary operation binder.</returns>
        public static CallSiteBinder BinaryOperation(
            CSharpBinderFlags flags,
            ExpressionType operation,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
            bool isLogical = (flags & CSharpBinderFlags.BinaryOperationLogical) != 0;

            CSharpBinaryOperationFlags binaryOperationFlags = 0;
            if (isLogical)
            {
                binaryOperationFlags |= CSharpBinaryOperationFlags.LogicalOperation;
            }

            return new CSharpBinaryOperationBinder(operation, isChecked, binaryOperationFlags, context, argumentInfo);
        }


        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp convert binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="type">The type to convert to.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <returns>Returns a new CSharp convert binder.</returns>
        public static CallSiteBinder Convert(
            CSharpBinderFlags flags,
            Type type,
            Type context)
        {
            CSharpConversionKind conversionKind =
                ((flags & CSharpBinderFlags.ConvertExplicit) != 0) ?
                    CSharpConversionKind.ExplicitConversion :
                    ((flags & CSharpBinderFlags.ConvertArrayIndex) != 0) ?
                        CSharpConversionKind.ArrayCreationConversion :
                        CSharpConversionKind.ImplicitConversion;
            bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;

            return new CSharpConvertBinder(type, conversionKind, isChecked, context);
        }


        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp get index binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp get index binder.</returns>
        public static CallSiteBinder GetIndex(
            CSharpBinderFlags flags,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            return new CSharpGetIndexBinder(context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp get member binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="name">The name of the member to get.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp get member binder.</returns>
        public static CallSiteBinder GetMember(
            CSharpBinderFlags flags,
            string name,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool allowCallables = (flags & CSharpBinderFlags.ResultIndexed) != 0;
            return new CSharpGetMemberBinder(name, allowCallables, context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp invoke binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp invoke binder.</returns>
        public static CallSiteBinder Invoke(
            CSharpBinderFlags flags,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool resultDiscarded = (flags & CSharpBinderFlags.ResultDiscarded) != 0;

            CSharpCallFlags callFlags = 0;
            if (resultDiscarded)
            {
                callFlags |= CSharpCallFlags.ResultDiscarded;
            }

            return new CSharpInvokeBinder(callFlags, context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp invoke member binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="name">The name of the member to invoke.</param>
        /// <param name="typeArguments">The list of type arguments specified for this invoke.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp invoke member binder.</returns>
        public static CallSiteBinder InvokeMember(
            CSharpBinderFlags flags,
            string name,
            IEnumerable<Type> typeArguments,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool invokeSimpleName = (flags & CSharpBinderFlags.InvokeSimpleName) != 0;
            bool invokeSpecialName = (flags & CSharpBinderFlags.InvokeSpecialName) != 0;
            bool resultDiscarded = (flags & CSharpBinderFlags.ResultDiscarded) != 0;

            CSharpCallFlags callFlags = 0;
            if (invokeSimpleName)
            {
                callFlags |= CSharpCallFlags.SimpleNameCall;
            }
            if (invokeSpecialName)
            {
                callFlags |= CSharpCallFlags.EventHookup;
            }
            if (resultDiscarded)
            {
                callFlags |= CSharpCallFlags.ResultDiscarded;
            }

            return new CSharpInvokeMemberBinder(callFlags, name, context, typeArguments, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp invoke constructor binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp invoke constructor binder.</returns>
        public static CallSiteBinder InvokeConstructor(
            CSharpBinderFlags flags,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            return new CSharpInvokeConstructorBinder(CSharpCallFlags.None, context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp is event binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="name">The name of the event to look for.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <returns>Returns a new CSharp is event binder.</returns>
        public static CallSiteBinder IsEvent(
            CSharpBinderFlags flags,
            string name,
            Type context)
        {
            return new CSharpIsEventBinder(name, context);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp set index binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp set index binder.</returns>
        public static CallSiteBinder SetIndex(
            CSharpBinderFlags flags,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool isCompoundAssignment = (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
            bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
            return new CSharpSetIndexBinder(isCompoundAssignment, isChecked, context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp set member binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="name">The name of the member to set.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp set member binder.</returns>
        public static CallSiteBinder SetMember(
            CSharpBinderFlags flags,
            string name,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool isCompoundAssignment = (flags & CSharpBinderFlags.ValueFromCompoundAssignment) != 0;
            bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
            return new CSharpSetMemberBinder(name, isCompoundAssignment, isChecked, context, argumentInfo);
        }

        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new CSharp unary operation binder.
        /// </summary>
        /// <param name="flags">The flags with which to initialize the binder.</param>
        /// <param name="operation">The unary operation kind.</param>
        /// <param name="context">The <see cref="System.Type"/> that indicates where this operation is used.</param>
        /// <param name="argumentInfo">The sequence of <see cref="CSharpArgumentInfo"/> instances for the arguments to this operation.</param>
        /// <returns>Returns a new CSharp unary operation binder.</returns>
        public static CallSiteBinder UnaryOperation(
            CSharpBinderFlags flags,
            ExpressionType operation,
            Type context,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            bool isChecked = (flags & CSharpBinderFlags.CheckedContext) != 0;
            return new CSharpUnaryOperationBinder(operation, isChecked, context, argumentInfo);
        }
    }
}
