// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSGeneratorResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            Debug.Assert(context != null);
            if (info.IsByRef || info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                // out of scope for Net7.0
                return ResolvedGenerator.NotSupported(new(info, context)
                {
                    NotSupportedDetails = SR.InOutRefNotSupported
                });
            }
            JSMarshallingInfo jsMarshalingInfo = info.MarshallingAttributeInfo as JSMarshallingInfo;

            ResolvedGenerator fail(string failReason)
            {
                return ResolvedGenerator.NotSupported(new(info, context)
                {
                    NotSupportedDetails = failReason
                });
            }
            bool isToJs = info.ManagedIndex != TypePositionInfo.ReturnIndex ^ context is JSExportCodeContext;

            switch (jsMarshalingInfo)
            {
                // invalid
                case { TypeInfo: JSInvalidTypeInfo }:
                    return ResolvedGenerator.NotSupported(new(info, context));

                // void
                case { TypeInfo: JSSimpleTypeInfo(KnownManagedType.Void), JSType: JSTypeFlags.DiscardNoWait }:
                    return ResolvedGenerator.Resolved(new VoidGenerator(MarshalerType.DiscardNoWait));
                case { TypeInfo: JSSimpleTypeInfo(KnownManagedType.Void), JSType: JSTypeFlags.Discard }:
                case { TypeInfo: JSSimpleTypeInfo(KnownManagedType.Void), JSType: JSTypeFlags.Void }:
                case { TypeInfo: JSSimpleTypeInfo(KnownManagedType.Void), JSType: JSTypeFlags.None }:
                case { TypeInfo: JSSimpleTypeInfo(KnownManagedType.Void), JSType: JSTypeFlags.Missing }:
                    return ResolvedGenerator.Resolved(new VoidGenerator(jsMarshalingInfo.JSType == JSTypeFlags.Void ? MarshalerType.Void : MarshalerType.Discard));

                // discard no void
                case { JSType: JSTypeFlags.Discard }:
                    return fail(SR.DiscardOnlyVoid);

                // oneway no void
                case { JSType: JSTypeFlags.DiscardNoWait }:
                    return fail(SR.DiscardNoWaitOnlyVoid);

                // primitive
                case { TypeInfo: JSSimpleTypeInfo simple }:
                    return Create(info, isToJs, simple.KnownType, Array.Empty<KnownManagedType>(), jsMarshalingInfo.JSType, Array.Empty<JSTypeFlags>(), fail);

                // nullable
                case { TypeInfo: JSNullableTypeInfo nullable }:
                    return Create(info, isToJs, nullable.KnownType, new[] { nullable.ResultTypeInfo.KnownType }, jsMarshalingInfo.JSType, null, fail);

                // array
                case { TypeInfo: JSArrayTypeInfo array }:
                    return Create(info, isToJs, array.KnownType, new[] { array.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // array segment
                case { TypeInfo: JSArraySegmentTypeInfo segment }:
                    return Create(info, isToJs, segment.KnownType, new[] { segment.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // span
                case { TypeInfo: JSSpanTypeInfo span }:
                    return Create(info, isToJs, span.KnownType, new[] { span.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // task
                case { TypeInfo: JSTaskTypeInfo(JSSimpleTypeInfo(KnownManagedType.Void)) task }:
                    return Create(info, isToJs, task.KnownType, Array.Empty<KnownManagedType>(), jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);
                case { TypeInfo: JSTaskTypeInfo task }:
                    return Create(info, isToJs, task.KnownType, new[] { task.ResultTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // action + function
                case { TypeInfo: JSFunctionTypeInfo function }:
                    return Create(info, isToJs, function.KnownType, function.ArgsTypeInfo.Select(a => a.KnownType).ToArray(), jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                default:
                    return ResolvedGenerator.NotSupported(new(info, context));
            }
        }

        internal static ResolvedGenerator Create(TypePositionInfo info, bool isToJs, KnownManagedType marshaledType, KnownManagedType[] argumentTypes, JSTypeFlags jsType, JSTypeFlags[] jsTypeArguments, Func<string, ResolvedGenerator> failWithReason)
        {
            switch (marshaledType)
            {
                // primitive
                case KnownManagedType.Boolean when jsType == JSTypeFlags.Boolean: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Byte when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Char when jsType == JSTypeFlags.String: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Char));
                case KnownManagedType.Int16 when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Int32 when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Int64 when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Int52));
                case KnownManagedType.Int64 when jsType == JSTypeFlags.BigInt: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.BigInt64));
                case KnownManagedType.Single when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Single));
                case KnownManagedType.Double when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Double));
                case KnownManagedType.IntPtr when jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.IntPtr));
                case KnownManagedType.DateTime when jsType == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.DateTime));
                case KnownManagedType.DateTimeOffset when jsType == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.DateTimeOffset));
                case KnownManagedType.Exception when jsType == JSTypeFlags.Error: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Exception));
                case KnownManagedType.JSObject when jsType == JSTypeFlags.Object: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.String when jsType == JSTypeFlags.String: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.String));
                case KnownManagedType.Object when jsType == JSTypeFlags.Any: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Object));

                // primitive missing
                case KnownManagedType.Boolean when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Byte when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Char when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Char));
                case KnownManagedType.Int16 when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Int32 when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Single when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Single));
                case KnownManagedType.Double when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Double));
                case KnownManagedType.IntPtr when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.IntPtr));
                case KnownManagedType.Exception when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.Exception));
                case KnownManagedType.JSObject when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.String when jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new PrimitiveJSGenerator(MarshalerType.String));

                // primitive forced
                case KnownManagedType.Int64 when jsType == JSTypeFlags.Missing:
                case KnownManagedType.DateTime when jsType == JSTypeFlags.Missing:
                case KnownManagedType.DateTimeOffset when jsType == JSTypeFlags.Missing:
                case KnownManagedType.Object when jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // nullable
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Boolean: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.String: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Int52));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.BigInt: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.BigInt64));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Double));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Single));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.IntPtr));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.DateTime));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.DateTimeOffset));

                // nullable missing
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Single));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.Double));
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new NullableJSGenerator(MarshalerType.IntPtr));

                // nullable forced
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                case KnownManagedType.Nullable:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // task
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes.Length == 0 && jsTypeArguments[0] == JSTypeFlags.Void: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Void));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Byte && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Boolean && jsTypeArguments[0] == JSTypeFlags.Boolean: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Char && jsTypeArguments[0] == JSTypeFlags.String: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Char));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int16 && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int32 && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int64 && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Int52));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int64 && jsTypeArguments[0] == JSTypeFlags.BigInt: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.BigInt64));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.IntPtr && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.IntPtr));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Double && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Double));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Single && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Single));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.JSObject && jsTypeArguments[0] == JSTypeFlags.Object: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.String && jsTypeArguments[0] == JSTypeFlags.String: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.String));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Exception && jsTypeArguments[0] == JSTypeFlags.Error: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Exception));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.DateTime && jsTypeArguments[0] == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.DateTime));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.DateTimeOffset && jsTypeArguments[0] == JSTypeFlags.Date: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.DateTimeOffset));
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Object && jsTypeArguments[0] == JSTypeFlags.Any: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Object));

                // task missing
                case KnownManagedType.Task when jsType == JSTypeFlags.Missing && argumentTypes.Length == 0: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Void));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Boolean));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Char));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Int16));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Single));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Double));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.IntPtr));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.JSObject && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.String && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.String));
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Exception && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new TaskJSGenerator(MarshalerType.Exception));

                // task forced
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Object && jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // array
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Byte && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.String && jsTypeArguments[0] == JSTypeFlags.String: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.String));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Double && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Double));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int32 && jsTypeArguments[0] == JSTypeFlags.Number: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.JSObject && jsTypeArguments[0] == JSTypeFlags.Object: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Object && jsTypeArguments[0] == JSTypeFlags.Any: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Object));

                // array missing
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.String && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.String));
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Double));
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.JSObject && jsType == JSTypeFlags.Missing: return ResolvedGenerator.Resolved(new ArrayJSGenerator(MarshalerType.JSObject));
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // array forced
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Object && jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // span view
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && jsTypeArguments.Length != 0:
                    return failWithReason(null);
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Byte: return ResolvedGenerator.Resolved(new SpanJSGenerator(MarshalerType.Byte));
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Int32: return ResolvedGenerator.Resolved(new SpanJSGenerator(MarshalerType.Int32));
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Double: return ResolvedGenerator.Resolved(new SpanJSGenerator(MarshalerType.Double));

                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // span forced
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // segment view
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && jsTypeArguments.Length != 0:
                    return failWithReason(null);
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Byte: return ResolvedGenerator.Resolved(new ArraySegmentJSGenerator(MarshalerType.Byte));
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Int32: return ResolvedGenerator.Resolved(new ArraySegmentJSGenerator(MarshalerType.Int32));
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Double: return ResolvedGenerator.Resolved(new ArraySegmentJSGenerator(MarshalerType.Double));
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // segment forced
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing:
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing:
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // function + action
                case KnownManagedType.Function when jsType == JSTypeFlags.Function && jsTypeArguments.Length == argumentTypes.Length:
                case KnownManagedType.Action when jsType == JSTypeFlags.Function && jsTypeArguments.Length == argumentTypes.Length:
                    var argsMarshalers = new List<MarshalerType>();
                    for (int i = 0; i < argumentTypes.Length; i++)
                    {
                        var isReturn = marshaledType == KnownManagedType.Function && i == jsTypeArguments.Length - 1;
                        if (argumentTypes[i] == KnownManagedType.Array
                            || argumentTypes[i] == KnownManagedType.Span
                            || argumentTypes[i] == KnownManagedType.ArraySegment
                            || argumentTypes[i] == KnownManagedType.Task
                            || argumentTypes[i] == KnownManagedType.Function
                            || argumentTypes[i] == KnownManagedType.Action
                            || argumentTypes[i] == KnownManagedType.Unknown
                            )
                        {
                            return failWithReason(SR.Format(SR.FuncArgumentNotSupported, argumentTypes[i]));
                        }
                        var gen = Create(info, isToJs ^ (!isReturn), argumentTypes[i], Array.Empty<KnownManagedType>(), jsTypeArguments[i], Array.Empty<JSTypeFlags>(), failWithReason);
                        argsMarshalers.Add(((BaseJSGenerator)gen.Generator).Type);
                    }
                    var maxArgs = marshaledType == KnownManagedType.Action ? 3 : 4;
                    var argsMarshallerTypes = argsMarshalers.ToArray();
                    if (argsMarshallerTypes.Length > maxArgs)
                    {
                        return failWithReason(SR.FuncTooManyArgs);
                    }
                    return ResolvedGenerator.Resolved(new FuncJSGenerator(marshaledType == KnownManagedType.Action, argsMarshallerTypes));
                case KnownManagedType.Action when jsType == JSTypeFlags.Function:
                case KnownManagedType.Function when jsType == JSTypeFlags.Function:
                    return failWithReason(SR.FuncWrongArgumentCount);

                // function + action forced
                case KnownManagedType.Function when jsType == JSTypeFlags.Missing:
                case KnownManagedType.Action when jsType == JSTypeFlags.Missing:
                    for (int i = 0; i < argumentTypes.Length; i++)
                    {
                        if (argumentTypes[i] == KnownManagedType.Array
                            || argumentTypes[i] == KnownManagedType.Span
                            || argumentTypes[i] == KnownManagedType.ArraySegment
                            || argumentTypes[i] == KnownManagedType.Task
                            || argumentTypes[i] == KnownManagedType.Function
                            || argumentTypes[i] == KnownManagedType.Action
                            || argumentTypes[i] == KnownManagedType.Unknown
                            )
                        {
                            return failWithReason(SR.Format(SR.FuncArgumentNotSupported, argumentTypes[i]));
                        }
                    }
                    return failWithReason(SR.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                default:
                    return failWithReason(SR.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));
            }
        }
    }
}
