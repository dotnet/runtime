// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class JSGeneratorFactory : IMarshallingGeneratorFactory
    {
        public IMarshallingGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            Debug.Assert(context != null);
            if (info.IsByRef || info.ByValueContentsMarshalKind != ByValueContentsMarshalKind.Default)
            {
                // out of scope for Net7.0
                throw new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = SR.InOutRefNotSupported
                };
            }
            JSMarshallingInfo jsMarshalingInfo = info.MarshallingAttributeInfo as JSMarshallingInfo;

            Exception fail(string failReason)
            {
                return new MarshallingNotSupportedException(info, context)
                {
                    NotSupportedDetails = failReason
                };
            }
            bool isToJs = info.ManagedIndex != TypePositionInfo.ReturnIndex ^ context is JSExportCodeContext;

            switch (info)
            {
                // invalid
                case { ManagedType: JSInvalidTypeInfo }:
                    throw new MarshallingNotSupportedException(info, context);

                // void
                case { ManagedType: SpecialTypeInfo sd } when sd.SpecialType == SpecialType.System_Void && jsMarshalingInfo.JSType == JSTypeFlags.Discard:
                case { ManagedType: SpecialTypeInfo sv } when sv.SpecialType == SpecialType.System_Void && jsMarshalingInfo.JSType == JSTypeFlags.Void:
                case { ManagedType: SpecialTypeInfo sn } when sn.SpecialType == SpecialType.System_Void && jsMarshalingInfo.JSType == JSTypeFlags.None:
                case { ManagedType: SpecialTypeInfo sm } when sm.SpecialType == SpecialType.System_Void && jsMarshalingInfo.JSType == JSTypeFlags.Missing:
                    return new VoidGenerator(jsMarshalingInfo.JSType == JSTypeFlags.Void ? MarshalerType.Void : MarshalerType.Discard);

                // discard no void
                case { } when jsMarshalingInfo.JSType == JSTypeFlags.Discard:
                    throw fail(SR.DiscardOnlyVoid);

                // primitive
                case { ManagedType: JSSimpleTypeInfo simple }:
                    return Create(info, isToJs, simple.KnownType, Array.Empty<KnownManagedType>(), jsMarshalingInfo.JSType, Array.Empty<JSTypeFlags>(), fail);

                // nullable
                case { ManagedType: JSNullableTypeInfo nullable }:
                    return Create(info, isToJs, nullable.KnownType, new[] { nullable.ResultTypeInfo.KnownType }, jsMarshalingInfo.JSType, null, fail);

                // array
                case { ManagedType: JSArrayTypeInfo array }:
                    return Create(info, isToJs, array.KnownType, new[] { array.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // array segment
                case { ManagedType: JSArraySegmentTypeInfo segment }:
                    return Create(info, isToJs, segment.KnownType, new[] { segment.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // span
                case { ManagedType: JSSpanTypeInfo span }:
                    return Create(info, isToJs, span.KnownType, new[] { span.ElementTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // task
                case { ManagedType: JSTaskTypeInfo task } when task.ResultTypeInfo is JSSimpleTypeInfo taskRes && taskRes.FullTypeName == "void":
                    return Create(info, isToJs, task.KnownType, Array.Empty<KnownManagedType>(), jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);
                case { ManagedType: JSTaskTypeInfo task }:
                    return Create(info, isToJs, task.KnownType, new[] { task.ResultTypeInfo.KnownType }, jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                // action + function
                case { ManagedType: JSFunctionTypeInfo function }:
                    return Create(info, isToJs, function.KnownType, function.ArgsTypeInfo.Select(a => a.KnownType).ToArray(), jsMarshalingInfo.JSType, jsMarshalingInfo.JSTypeArguments, fail);

                default:
                    throw new MarshallingNotSupportedException(info, context);
            }
        }

        internal static BaseJSGenerator Create(TypePositionInfo info, bool isToJs, KnownManagedType marshaledType, KnownManagedType[] argumentTypes, JSTypeFlags jsType, JSTypeFlags[] jsTypeArguments, Func<string, Exception> failWithReason)
        {
            switch (marshaledType)
            {
                // primitive
                case KnownManagedType.Boolean when jsType == JSTypeFlags.Boolean: return new PrimitiveJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Byte when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Char when jsType == JSTypeFlags.String: return new PrimitiveJSGenerator(MarshalerType.Char);
                case KnownManagedType.Int16 when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Int32 when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Int64 when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Int52);
                case KnownManagedType.Int64 when jsType == JSTypeFlags.BigInt: return new PrimitiveJSGenerator(MarshalerType.BigInt64);
                case KnownManagedType.Single when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Single);
                case KnownManagedType.Double when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.Double);
                case KnownManagedType.IntPtr when jsType == JSTypeFlags.Number: return new PrimitiveJSGenerator(MarshalerType.IntPtr);
                case KnownManagedType.DateTime when jsType == JSTypeFlags.Date: return new PrimitiveJSGenerator(MarshalerType.DateTime);
                case KnownManagedType.DateTimeOffset when jsType == JSTypeFlags.Date: return new PrimitiveJSGenerator(MarshalerType.DateTimeOffset);
                case KnownManagedType.Exception when jsType == JSTypeFlags.Error: return new PrimitiveJSGenerator(MarshalerType.Exception);
                case KnownManagedType.JSObject when jsType == JSTypeFlags.Object: return new PrimitiveJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.String when jsType == JSTypeFlags.String: return new PrimitiveJSGenerator(MarshalerType.String);
                case KnownManagedType.Object when jsType == JSTypeFlags.Any: return new PrimitiveJSGenerator(MarshalerType.Object);

                // primitive missing
                case KnownManagedType.Boolean when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Byte when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Char when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Char);
                case KnownManagedType.Int16 when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Int32 when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Single when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Single);
                case KnownManagedType.Double when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Double);
                case KnownManagedType.IntPtr when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.IntPtr);
                case KnownManagedType.Exception when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.Exception);
                case KnownManagedType.JSObject when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.String when jsType == JSTypeFlags.Missing: return new PrimitiveJSGenerator(MarshalerType.String);

                // primitive forced
                case KnownManagedType.Int64 when jsType == JSTypeFlags.Missing:
                case KnownManagedType.DateTime when jsType == JSTypeFlags.Missing:
                case KnownManagedType.DateTimeOffset when jsType == JSTypeFlags.Missing:
                case KnownManagedType.Object when jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // nullable
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Boolean: return new NullableJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.String: return new NullableJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Int52);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.BigInt: return new NullableJSGenerator(MarshalerType.BigInt64);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Double);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.Single);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Number: return new NullableJSGenerator(MarshalerType.IntPtr);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Date: return new NullableJSGenerator(MarshalerType.DateTime);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Date: return new NullableJSGenerator(MarshalerType.DateTimeOffset);

                // nullable missing
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Single);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.Double);
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Missing: return new NullableJSGenerator(MarshalerType.IntPtr);

                // nullable forced
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Nullable when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                case KnownManagedType.Nullable:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // task
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes.Length == 0 && jsTypeArguments[0] == JSTypeFlags.Void: return new TaskJSGenerator(MarshalerType.Void);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Byte && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Boolean && jsTypeArguments[0] == JSTypeFlags.Boolean: return new TaskJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Char && jsTypeArguments[0] == JSTypeFlags.String: return new TaskJSGenerator(MarshalerType.Char);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int16 && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int32 && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int64 && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Int52);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int64 && jsTypeArguments[0] == JSTypeFlags.BigInt: return new TaskJSGenerator(MarshalerType.BigInt64);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.IntPtr && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.IntPtr);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Double && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Double);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Single && jsTypeArguments[0] == JSTypeFlags.Number: return new TaskJSGenerator(MarshalerType.Single);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.JSObject && jsTypeArguments[0] == JSTypeFlags.Object: return new TaskJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.String && jsTypeArguments[0] == JSTypeFlags.String: return new TaskJSGenerator(MarshalerType.String);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Exception && jsTypeArguments[0] == JSTypeFlags.Error: return new TaskJSGenerator(MarshalerType.Exception);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.DateTime && jsTypeArguments[0] == JSTypeFlags.Date: return new TaskJSGenerator(MarshalerType.DateTime);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.DateTimeOffset && jsTypeArguments[0] == JSTypeFlags.Date: return new TaskJSGenerator(MarshalerType.DateTimeOffset);
                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Object && jsTypeArguments[0] == JSTypeFlags.Any: return new TaskJSGenerator(MarshalerType.Object);

                // task missing
                case KnownManagedType.Task when jsType == JSTypeFlags.Missing && argumentTypes.Length == 0: return new TaskJSGenerator(MarshalerType.Void);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Boolean && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Boolean);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Char && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Char);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int16 && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Int16);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Single && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Single);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Double);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.IntPtr && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.IntPtr);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.JSObject && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.String && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.String);
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Exception && jsType == JSTypeFlags.Missing: return new TaskJSGenerator(MarshalerType.Exception);

                // task forced
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Int64 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.DateTime && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.DateTimeOffset && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Task when argumentTypes[0] == KnownManagedType.Object && jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                case KnownManagedType.Task when jsType == JSTypeFlags.Promise && jsTypeArguments.Length == 1:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // array
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Byte && jsTypeArguments[0] == JSTypeFlags.Number: return new ArrayJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.String && jsTypeArguments[0] == JSTypeFlags.String: return new ArrayJSGenerator(MarshalerType.String);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Double && jsTypeArguments[0] == JSTypeFlags.Number: return new ArrayJSGenerator(MarshalerType.Double);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Int32 && jsTypeArguments[0] == JSTypeFlags.Number: return new ArrayJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.JSObject && jsTypeArguments[0] == JSTypeFlags.Object: return new ArrayJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1 && argumentTypes[0] == KnownManagedType.Object && jsTypeArguments[0] == JSTypeFlags.Any: return new ArrayJSGenerator(MarshalerType.Object);

                // array missing
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing: return new ArrayJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.String && jsType == JSTypeFlags.Missing: return new ArrayJSGenerator(MarshalerType.String);
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing: return new ArrayJSGenerator(MarshalerType.Double);
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing: return new ArrayJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.JSObject && jsType == JSTypeFlags.Missing: return new ArrayJSGenerator(MarshalerType.JSObject);
                case KnownManagedType.Array when jsType == JSTypeFlags.Array && jsTypeArguments.Length == 1:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // array forced
                case KnownManagedType.Array when argumentTypes[0] == KnownManagedType.Object && jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // span view
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && jsTypeArguments.Length != 0:
                    throw failWithReason(null);
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Byte: return new SpanJSGenerator(MarshalerType.Byte);
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Int32: return new SpanJSGenerator(MarshalerType.Int32);
                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Double: return new SpanJSGenerator(MarshalerType.Double);

                case KnownManagedType.Span when jsType == JSTypeFlags.MemoryView:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // span forced
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.Span when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                // segment view
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && jsTypeArguments.Length != 0:
                    throw failWithReason(null);
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Byte: return new ArraySegmentJSGenerator(MarshalerType.Byte);
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Int32: return new ArraySegmentJSGenerator(MarshalerType.Int32);
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView && argumentTypes[0] == KnownManagedType.Double: return new ArraySegmentJSGenerator(MarshalerType.Double);
                case KnownManagedType.ArraySegment when jsType == JSTypeFlags.MemoryView:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));

                // segment forced
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Byte && jsType == JSTypeFlags.Missing:
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Int32 && jsType == JSTypeFlags.Missing:
                case KnownManagedType.ArraySegment when argumentTypes[0] == KnownManagedType.Double && jsType == JSTypeFlags.Missing:
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

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
                            throw failWithReason(string.Format(SR.FuncArgumentNotSupported, argumentTypes[i]));
                        }
                        var gen = Create(info, isToJs ^ (!isReturn), argumentTypes[i], Array.Empty<KnownManagedType>(), jsTypeArguments[i], Array.Empty<JSTypeFlags>(), failWithReason);
                        argsMarshalers.Add(gen.Type);
                    }
                    return new FuncJSGenerator(marshaledType == KnownManagedType.Action, argsMarshalers.ToArray());
                case KnownManagedType.Action when jsType == JSTypeFlags.Function:
                case KnownManagedType.Function when jsType == JSTypeFlags.Function:
                    throw failWithReason(SR.FuncWrongArgumentCount);

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
                            throw failWithReason(string.Format(SR.FuncArgumentNotSupported, argumentTypes[i]));
                        }
                    }
                    throw failWithReason(string.Format(SR.UseJSMarshalAsAttribute, info.ManagedType.FullTypeName));

                default:
                    throw failWithReason(string.Format(SR.TypeNotSupportedName, info.ManagedType.FullTypeName));
            }
        }
    }
}
