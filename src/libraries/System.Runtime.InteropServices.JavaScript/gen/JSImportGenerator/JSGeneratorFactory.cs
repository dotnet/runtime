// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSGeneratorResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            Debug.Assert(context != null);
            if (!info.IsManagedExceptionPosition && !info.IsManagedReturnPosition && (info.IsByRef || info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default))
            {
                // out of scope for Net7.0
                return ResolvedGenerator.NotSupported(info, context, new(info)
                {
                    NotSupportedDetails = SR.InOutRefNotSupported
                });
            }

            if (MarshallerHelpers.GetMarshalDirection(info, context) != MarshalDirection.UnmanagedToManaged && info.ManagedIndex == TypePositionInfo.UnsetIndex)
            {
                // Use the InitOnlyJSGenerator for native-only parameters.
                return ResolvedGenerator.Resolved(new ImplicitArgumentGenerator(info, context));
            }

            return Create(info, info.MarshallingAttributeInfo as JSMarshallingInfo, context).Generator;
        }

        public static (MarshalerType BaseType, IEnumerable<MarshalerType>? SubTypes) GetMarshallerTypeForBinding(TypePositionInfo info, StubCodeContext context)
        {
            var (_, baseType, subTypes) = Create(info, info.MarshallingAttributeInfo as JSMarshallingInfo, context);
            return (baseType, subTypes);
        }

        private record struct ResolvedGeneratorAndType(ResolvedGenerator Generator, MarshalerType Type, IEnumerable<MarshalerType>? SubTypes = null);

        private static ResolvedGeneratorAndType Create(TypePositionInfo info, JSMarshallingInfo jsMarshalingInfo, StubCodeContext context)
        {
            bool isToJs = MarshallerHelpers.GetMarshalDirection(info, context) == MarshalDirection.ManagedToUnmanaged;

            switch (jsMarshalingInfo)
            {
                // invalid
                case { TypeInfo: JSInvalidTypeInfo }:
                    return new(ResolvedGenerator.NotSupported(info, context, new(info)), MarshalerType.None);

                // primitive
                case { TypeInfo: JSSimpleTypeInfo simple }:
                    return Create(info, context, isToJs, simple.KnownType, [], jsMarshalingInfo.JSType, []);

                // nullable
                case { TypeInfo: JSNullableTypeInfo nullable }:
                    return Create(info, context, isToJs, nullable.KnownType, [nullable.ResultTypeInfo.KnownType], jsMarshalingInfo.JSType, null);

                // array
                case { TypeInfo: JSArrayTypeInfo array }:
                    return Create(info, context, isToJs, array.KnownType, [array.ElementTypeInfo.KnownType], jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);

                // array segment
                case { TypeInfo: JSArraySegmentTypeInfo segment }:
                    return Create(info, context, isToJs, segment.KnownType, [segment.ElementTypeInfo.KnownType], jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);

                // span
                case { TypeInfo: JSSpanTypeInfo span }:
                    return Create(info, context, isToJs, span.KnownType, [span.ElementTypeInfo.KnownType], jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);

                // task
                case { TypeInfo: JSTaskTypeInfo(JSSimpleTypeInfo(KnownManagedType.Void)) task }:
                    return Create(info, context, isToJs, task.KnownType, [], jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);
                case { TypeInfo: JSTaskTypeInfo task }:
                    return Create(info, context, isToJs, task.KnownType, [task.ResultTypeInfo.KnownType], jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);

                // action + function
                case { TypeInfo: JSFunctionTypeInfo function }:
                    return Create(info, context, isToJs, function.KnownType, function.ArgsTypeInfo.Select(a => a.KnownType).ToArray(), jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments);

                default:
                    return new(ResolvedGenerator.NotSupported(info, context, new(info)), MarshalerType.None);
            }
        }

        private static ResolvedGeneratorAndType Create(TypePositionInfo info, StubCodeContext context, bool isToJs, KnownManagedType marshaledType, KnownManagedType[] argumentTypes, JSTypeFlags jsType, JSTypeFlags[] jsTypeArguments)
        {
            return (marshaledType, jsType, argumentTypes, jsTypeArguments) switch
            {
                // void
                (KnownManagedType.Void, JSTypeFlags.Void, _, _) => resolved(new Forwarder().Bind(info, context), MarshalerType.Void),
                (KnownManagedType.Void, JSTypeFlags.None or JSTypeFlags.Discard, _, _) => resolved(new Forwarder().Bind(info, context), MarshalerType.Discard),
                (KnownManagedType.Void, JSTypeFlags.DiscardNoWait, _, _) => resolved(new Forwarder().Bind(info, context), MarshalerType.DiscardNoWait),

                // void missing
                (KnownManagedType.Void, JSTypeFlags.Missing, _, _) => resolved(new Forwarder().Bind(info, context), MarshalerType.Discard),

                // primitive
                (KnownManagedType.Boolean, JSTypeFlags.Boolean, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Boolean),
                (KnownManagedType.Byte, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Byte),
                (KnownManagedType.Char, JSTypeFlags.String, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Char), MarshalerType.Char),
                (KnownManagedType.Int16, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Int16),
                (KnownManagedType.Int32, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Int32),
                (KnownManagedType.Int64, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int52), MarshalerType.Int52),
                (KnownManagedType.Int64, JSTypeFlags.BigInt, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.BigInt64), MarshalerType.BigInt64),
                (KnownManagedType.Single, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Single), MarshalerType.Single),
                (KnownManagedType.Double, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Double), MarshalerType.Double),
                (KnownManagedType.IntPtr, JSTypeFlags.Number, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.IntPtr),
                (KnownManagedType.DateTime, JSTypeFlags.Date, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.DateTime), MarshalerType.DateTime),
                (KnownManagedType.DateTimeOffset, JSTypeFlags.Date, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.DateTimeOffset), MarshalerType.DateTimeOffset),
                (KnownManagedType.Exception, JSTypeFlags.Error, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Exception), MarshalerType.Exception),
                (KnownManagedType.JSObject, JSTypeFlags.Object, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.JSObject), MarshalerType.JSObject),
                (KnownManagedType.String, JSTypeFlags.String, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.String), MarshalerType.String),
                (KnownManagedType.Object, JSTypeFlags.Any, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Object), MarshalerType.Object),

                // primitive missing
                (KnownManagedType.Boolean, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Boolean),
                (KnownManagedType.Byte, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Byte),
                (KnownManagedType.Char, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Char), MarshalerType.Char),
                (KnownManagedType.Int16, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Int16),
                (KnownManagedType.Int32, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Int32),
                (KnownManagedType.Single, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Single), MarshalerType.Single),
                (KnownManagedType.Double, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Double), MarshalerType.Double),
                (KnownManagedType.IntPtr, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.IntPtr),
                (KnownManagedType.Exception, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Exception), MarshalerType.Exception),
                (KnownManagedType.JSObject, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.JSObject), MarshalerType.JSObject),
                (KnownManagedType.String, JSTypeFlags.Missing, _, _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.String), MarshalerType.String),

                // primitive forced
                (KnownManagedType.Int64, JSTypeFlags.Missing, _, _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.DateTime, JSTypeFlags.Missing, _, _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.DateTimeOffset, JSTypeFlags.Missing, _, _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Object, JSTypeFlags.Missing, _, _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                // nullable
                (KnownManagedType.Nullable, JSTypeFlags.Boolean, [KnownManagedType.Boolean], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Nullable, [MarshalerType.Boolean]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Byte], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Nullable, [MarshalerType.Byte]),
                (KnownManagedType.Nullable, JSTypeFlags.String, [KnownManagedType.Char], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Char), MarshalerType.Nullable, [MarshalerType.Char]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Int16], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Nullable, [MarshalerType.Int16]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Int32], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Nullable, [MarshalerType.Int32]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Int64], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int52), MarshalerType.Nullable, [MarshalerType.Int52]),
                (KnownManagedType.Nullable, JSTypeFlags.BigInt, [KnownManagedType.Int64], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.BigInt64), MarshalerType.Nullable, [MarshalerType.BigInt64]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Single], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Single), MarshalerType.Nullable, [MarshalerType.Single]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.Double], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Double), MarshalerType.Nullable, [MarshalerType.Double]),
                (KnownManagedType.Nullable, JSTypeFlags.Number, [KnownManagedType.IntPtr], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.Nullable, [MarshalerType.IntPtr]),
                (KnownManagedType.Nullable, JSTypeFlags.Date, [KnownManagedType.DateTime], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.DateTime), MarshalerType.Nullable, [MarshalerType.DateTime]),
                (KnownManagedType.Nullable, JSTypeFlags.Date, [KnownManagedType.DateTimeOffset], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.DateTimeOffset), MarshalerType.Nullable, [MarshalerType.DateTimeOffset]),

                // nullable missing
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Boolean], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Nullable, [MarshalerType.Boolean]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Byte], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Nullable, [MarshalerType.Byte]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Char], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Char), MarshalerType.Nullable, [MarshalerType.Char]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Int16], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Nullable, [MarshalerType.Int16]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Int32], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Nullable, [MarshalerType.Int32]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Single], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Single), MarshalerType.Nullable, [MarshalerType.Single]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Double], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Double), MarshalerType.Nullable, [MarshalerType.Double]),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.IntPtr], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.Nullable, [MarshalerType.IntPtr]),

                // nullable forced
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.Int64], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.DateTime], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Nullable, JSTypeFlags.Missing, [KnownManagedType.DateTimeOffset], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                (KnownManagedType.Nullable, _, _, _) => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),


                // task
                (KnownManagedType.Task, JSTypeFlags.Promise, [], [JSTypeFlags.Void]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Void), MarshalerType.Task, []),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Boolean], [JSTypeFlags.Boolean]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Task, [MarshalerType.Boolean]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Byte], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Task, [MarshalerType.Byte]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Char], [JSTypeFlags.String]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Char), MarshalerType.Task, [MarshalerType.Char]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Int16], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Task, [MarshalerType.Int16]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Int32], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Task, [MarshalerType.Int32]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Int64], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Int52), MarshalerType.Task, [MarshalerType.Int52]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Int64], [JSTypeFlags.BigInt]) => resolved(new TaskJSGenerator(info, context, MarshalerType.BigInt64), MarshalerType.Task, [MarshalerType.BigInt64]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Single], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Single), MarshalerType.Task, [MarshalerType.Single]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Double], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Double), MarshalerType.Task, [MarshalerType.Double]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.IntPtr], [JSTypeFlags.Number]) => resolved(new TaskJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.Task, [MarshalerType.IntPtr]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.DateTime], [JSTypeFlags.Date]) => resolved(new TaskJSGenerator(info, context, MarshalerType.DateTime), MarshalerType.Task, [MarshalerType.DateTime]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.DateTimeOffset], [JSTypeFlags.Date]) => resolved(new TaskJSGenerator(info, context, MarshalerType.DateTimeOffset), MarshalerType.Task, [MarshalerType.DateTimeOffset]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Exception], [JSTypeFlags.Error]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Exception), MarshalerType.Task, [MarshalerType.Exception]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.JSObject], [JSTypeFlags.Object]) => resolved(new TaskJSGenerator(info, context, MarshalerType.JSObject), MarshalerType.Task, [MarshalerType.JSObject]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.String], [JSTypeFlags.String]) => resolved(new TaskJSGenerator(info, context, MarshalerType.String), MarshalerType.Task, [MarshalerType.String]),
                (KnownManagedType.Task, JSTypeFlags.Promise, [KnownManagedType.Object], [JSTypeFlags.Any]) => resolved(new TaskJSGenerator(info, context, MarshalerType.Object), MarshalerType.Task, [MarshalerType.Object]),

                // task missing
                (KnownManagedType.Task, JSTypeFlags.Missing, [], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Void), MarshalerType.Task, []),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Boolean], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Boolean), MarshalerType.Task, [MarshalerType.Boolean]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Byte], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Byte), MarshalerType.Task, [MarshalerType.Byte]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Char], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Char), MarshalerType.Task, [MarshalerType.Char]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Int16], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Int16), MarshalerType.Task, [MarshalerType.Int16]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Int32], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Int32), MarshalerType.Task, [MarshalerType.Int32]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Single], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Single), MarshalerType.Task, [MarshalerType.Single]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Double], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Double), MarshalerType.Task, [MarshalerType.Double]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.IntPtr], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.IntPtr), MarshalerType.Task, [MarshalerType.IntPtr]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Exception], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.Exception), MarshalerType.Task, [MarshalerType.Exception]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.JSObject], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.JSObject), MarshalerType.Task, [MarshalerType.JSObject]),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.String], _) => resolved(new TaskJSGenerator(info, context, MarshalerType.String), MarshalerType.Task, [MarshalerType.String]),

                // task forced
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Int64], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.DateTime], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.DateTimeOffset], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Task, JSTypeFlags.Missing, [KnownManagedType.Object], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                (KnownManagedType.Task, JSTypeFlags.Promise, _, [_]) => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),

                // array
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.Byte], [JSTypeFlags.Number]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Byte]),
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.String], [JSTypeFlags.String]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.String]),
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.Double], [JSTypeFlags.Number]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Double]),
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.Int32], [JSTypeFlags.Number]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Int32]),
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.JSObject], [JSTypeFlags.Object]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.JSObject]),
                (KnownManagedType.Array, JSTypeFlags.Array, [KnownManagedType.Object], [JSTypeFlags.Any]) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Object]),

                // array missing
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.Byte], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Byte]),
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.String], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.String]),
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.Double], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Double]),
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.Int32], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.Int32]),
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.JSObject], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Array), MarshalerType.Array, [MarshalerType.JSObject]),

                (KnownManagedType.Array, JSTypeFlags.Array, _, [_]) => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),

                // array forced
                (KnownManagedType.Array, JSTypeFlags.Missing, [KnownManagedType.Object], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                // span view
                (KnownManagedType.Span, JSTypeFlags.MemoryView, _, [_]) => failWithReason(null!),
                (KnownManagedType.Span, JSTypeFlags.MemoryView, [KnownManagedType.Byte], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Span), MarshalerType.Span, [MarshalerType.Byte]),
                (KnownManagedType.Span, JSTypeFlags.MemoryView, [KnownManagedType.Int32], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Span), MarshalerType.Span, [MarshalerType.Int32]),
                (KnownManagedType.Span, JSTypeFlags.MemoryView, [KnownManagedType.Double], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.Span), MarshalerType.Span, [MarshalerType.Double]),

                (KnownManagedType.Span, JSTypeFlags.MemoryView, _, _) => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),

                // span forced
                (KnownManagedType.Span, JSTypeFlags.Missing, [KnownManagedType.Byte], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Span, JSTypeFlags.Missing, [KnownManagedType.Int32], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.Span, JSTypeFlags.Missing, [KnownManagedType.Double], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                // segment view
                (KnownManagedType.ArraySegment, JSTypeFlags.MemoryView, _, [_]) => failWithReason(null!),
                (KnownManagedType.ArraySegment, JSTypeFlags.MemoryView, [KnownManagedType.Byte], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.ArraySegment), MarshalerType.ArraySegment, [MarshalerType.Byte]),
                (KnownManagedType.ArraySegment, JSTypeFlags.MemoryView, [KnownManagedType.Int32], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.ArraySegment), MarshalerType.ArraySegment, [MarshalerType.Int32]),
                (KnownManagedType.ArraySegment, JSTypeFlags.MemoryView, [KnownManagedType.Double], _) => resolved(new PrimitiveJSGenerator(info, context, MarshalerType.ArraySegment), MarshalerType.ArraySegment, [MarshalerType.Double]),

                (KnownManagedType.ArraySegment, JSTypeFlags.MemoryView, _, _) => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),

                // segment forced
                (KnownManagedType.ArraySegment, JSTypeFlags.Missing, [KnownManagedType.Byte], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.ArraySegment, JSTypeFlags.Missing, [KnownManagedType.Int32], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),
                (KnownManagedType.ArraySegment, JSTypeFlags.Missing, [KnownManagedType.Double], _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                // function + action
                (KnownManagedType.Function or KnownManagedType.Action, JSTypeFlags.Function, var argTypes, var argJSTypes) when argTypes.Length != argJSTypes.Length
                    => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),

                (KnownManagedType.Function or KnownManagedType.Action, JSTypeFlags.Function or JSTypeFlags.Missing, var argTypes, _) when FindFirstInvalidArgType(argTypes) is KnownManagedType invalidArgType
                    => failWithReason(SR.Format(SR.FuncArgumentNotSupported, invalidArgType)),

                (KnownManagedType.Function or KnownManagedType.Action, JSTypeFlags.Function, var argTypes, var argJSTypes) => ResolveCallback(marshaledType, argTypes, argJSTypes),

                // function + action forced
                (KnownManagedType.Function or KnownManagedType.Action, JSTypeFlags.Missing, _, _) => failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName)),

                // void only JSType on non-void
                (not KnownManagedType.Void, JSTypeFlags.Discard, _, _) => failWithReason(SR.DiscardOnlyVoid),
                (not KnownManagedType.Void, JSTypeFlags.DiscardNoWait, _, _) => failWithReason(SR.DiscardNoWaitOnlyVoid),


                _ => failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName)),
            };

            KnownManagedType? FindFirstInvalidArgType(KnownManagedType[] argumentTypes)
            {
                foreach (KnownManagedType type in argumentTypes)
                {
                    if (type is KnownManagedType.Array
                        or KnownManagedType.Span
                        or KnownManagedType.ArraySegment
                        or KnownManagedType.Task
                        or KnownManagedType.Function
                        or KnownManagedType.Action
                        or KnownManagedType.Unknown
                        )
                    {
                        return type;
                    }
                }
                return null;
            }

            ResolvedGeneratorAndType ResolveCallback(KnownManagedType managedType, KnownManagedType[] argTypes, JSTypeFlags[] argJSTypes)
            {
                var argsMarshalers = new List<MarshalerType>();
                for (int i = 0; i < argTypes.Length; i++)
                {
                    var isReturn = managedType == KnownManagedType.Function && i == argJSTypes.Length - 1;

                    var gen = Create(info, context, isToJs ^ (!isReturn), argTypes[i], Array.Empty<KnownManagedType>(), argJSTypes[i], Array.Empty<JSTypeFlags>());
                    argsMarshalers.Add(gen.Type);
                }
                var maxArgs = managedType == KnownManagedType.Action ? 3 : 4;
                MarshalerType[] argsMarshallerTypes = [.. argsMarshalers];
                if (argsMarshallerTypes.Length > maxArgs)
                {
                    return failWithReason(SR.FuncTooManyArgs);
                }
                return resolved(
                    new FuncJSGenerator(info, context, managedType == KnownManagedType.Action, argsMarshallerTypes),
                    managedType == KnownManagedType.Action ? MarshalerType.Action : MarshalerType.Function,
                    argsMarshallerTypes);
            }

            ResolvedGeneratorAndType failWithReason(string failReason)
            {
                return new(
                    ResolvedGenerator.NotSupported(info, context, new(info)
                    {
                        NotSupportedDetails = failReason
                    }),
                    MarshalerType.None);
            }

            ResolvedGeneratorAndType resolved(IBoundMarshallingGenerator generator, MarshalerType baseType, IEnumerable<MarshalerType>? subTypes = null)
            {
                return new(ResolvedGenerator.Resolved(generator), baseType, subTypes);
            }
        }
    }
}
