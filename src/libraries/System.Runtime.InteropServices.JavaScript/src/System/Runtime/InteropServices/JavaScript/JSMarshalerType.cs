// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents type metadata about marshaler, which are necessary for marshaling function arguments.
    /// It's used by JSImport code generator and should not be used by developers in source code.
    /// </summary>
    [SupportedOSPlatform("browser")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JSMarshalerType
    {
        internal JSFunctionBinding.JSBindingType _signatureType;

        private JSMarshalerType()
        {
            _signatureType = JSMarshalerType.Void._signatureType;
        }

        private JSMarshalerType(JSFunctionBinding.JSBindingType signatureType)
        {
            _signatureType = signatureType;
        }

        /// <summary>
        /// Marshaling validates that the JavaScript function returned undefined value.
        /// </summary>
        public static JSMarshalerType Void { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Void
        });

        /// <summary>
        /// Marshaling ignores result of the JavaScript function.
        /// </summary>
        public static JSMarshalerType Discard { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Discard
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Boolean">Boolean</see> type.
        /// </summary>
        public static JSMarshalerType Boolean { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Boolean
        });

        /// <summary>
        /// Marshal as JavaScript Number via <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Uint8Array">Uint8Array</see>.
        /// </summary>
        public static JSMarshalerType Byte { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Byte
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String">String</see> one character long.
        /// </summary>
        public static JSMarshalerType Char { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Char
        });

        /// <summary>
        /// Marshal as JavaScript Number via <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Int16Array">Int16Array</see>.
        /// </summary>
        public static JSMarshalerType Int16 { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Int16
        });


        /// <summary>
        /// Marshal as JavaScript Number via <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Int32Array">Int32Array</see>.
        /// </summary>
        public static JSMarshalerType Int32 { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Int32
        });

        /// <summary>
        /// Marshal as JavaScript Number within -9007199254740991 and 9007199254740991 range.
        /// </summary>
        public static JSMarshalerType Int52 { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Int52
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt">BigInt</see>.
        /// </summary>
        public static JSMarshalerType BigInt64 { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.BigInt64
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number">Number</see>.
        /// </summary>
        public static JSMarshalerType Double { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Double
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number">Number</see>.
        /// </summary>
        public static JSMarshalerType Single { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Single
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number">Number</see>.
        /// </summary>
        public static JSMarshalerType IntPtr { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.IntPtr
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object">Object</see> or it's JSObject proxy on managed side.
        /// </summary>
        public static JSMarshalerType JSObject { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.JSObject
        });

        /// <summary>
        /// Dynamic marshal as best fit. For <see cref="object"/> maps to ManagedObject proxy on JavaScript side.
        /// </summary>
        public static JSMarshalerType Object { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Object
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String">String</see>.
        /// </summary>
        public static JSMarshalerType String { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.String
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Error">Error</see>.
        /// </summary>
        public static JSMarshalerType Exception { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Exception
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date">Date</see>.
        /// </summary>
        public static JSMarshalerType DateTime { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.DateTime
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date">Date</see>.
        /// </summary>
        public static JSMarshalerType DateTimeOffset { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.DateTimeOffset
        });

        public static JSMarshalerType Nullable(JSMarshalerType primitive)
        {
            CheckNullable(primitive);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Nullable,
                ResultMarshalerType = primitive._signatureType.Type
            });
        }

        private static JSMarshalerType _task { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Task
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise">Promise</see>.
        /// </summary>
        public static JSMarshalerType Task()
        {
            return _task;
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise">Promise</see>.
        /// </summary>
        public static JSMarshalerType Task(JSMarshalerType result)
        {
            CheckTask(result);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Task,
                ResultMarshalerType = result._signatureType.Type
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array">Array</see>.
        /// </summary>
        public static JSMarshalerType Array(JSMarshalerType element)
        {
            CheckArray(element);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Array,
                Arg1MarshalerType = element._signatureType.Type
            });
        }

        /// <summary>
        /// Marshal as instance of ArraySegment class on JavaScript side.
        /// </summary>
        /// <remarks>
        /// ArraySegment will hold <see cref="GCHandleType.Pinned"/> to underlying array until disposed or garbage collected on JS side.
        /// </remarks>
        public static JSMarshalerType ArraySegment(JSMarshalerType element)
        {
            CheckArraySegment(element);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.ArraySegment,
                Arg1MarshalerType = element._signatureType.Type
            });
        }

        /// <summary>
        /// Marshal as instance of Span class on JavaScript side.
        /// </summary>
        /// <remarks>Marshaler is not pinning the data, caller is responsible for doing that.</remarks>
        public static JSMarshalerType Span(JSMarshalerType element)
        {
            CheckArraySegment(element);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Span,
                Arg1MarshalerType = element._signatureType.Type
            });
        }

        private static JSMarshalerType _action { get; } = new JSMarshalerType(new JSFunctionBinding.JSBindingType
        {
            Type = MarshalerType.Action
        });

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Action()
        {
            return _action;
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Action(JSMarshalerType arg1)
        {
            CheckTask(arg1);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Action,
                Arg1MarshalerType = arg1._signatureType.Type
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2)
        {
            CheckTask(arg1);
            CheckTask(arg2);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Action,
                Arg1MarshalerType = arg1._signatureType.Type,
                Arg2MarshalerType = arg2._signatureType.Type,
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Action(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3)
        {
            CheckTask(arg1);
            CheckTask(arg2);
            CheckTask(arg3);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Action,
                Arg1MarshalerType = arg1._signatureType.Type,
                Arg2MarshalerType = arg2._signatureType.Type,
                Arg3MarshalerType = arg3._signatureType.Type,
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Function(JSMarshalerType result)
        {
            CheckTask(result);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Function,
                ResultMarshalerType = result._signatureType.Type,
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType result)
        {
            CheckTask(arg1);
            CheckTask(result);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Function,
                Arg1MarshalerType = arg1._signatureType.Type,
                ResultMarshalerType = result._signatureType.Type,
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType result)
        {
            CheckTask(arg1);
            CheckTask(arg2);
            CheckTask(result);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Function,
                Arg1MarshalerType = arg1._signatureType.Type,
                Arg2MarshalerType = arg2._signatureType.Type,
                ResultMarshalerType = result._signatureType.Type,
            });
        }

        /// <summary>
        /// Marshal as JavaScript <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function">Function</see>.
        /// </summary>
        public static JSMarshalerType Function(JSMarshalerType arg1, JSMarshalerType arg2, JSMarshalerType arg3, JSMarshalerType result)
        {
            CheckTask(arg1);
            CheckTask(arg2);
            CheckTask(arg3);
            CheckTask(result);
            return new JSMarshalerType(new JSFunctionBinding.JSBindingType
            {
                Type = MarshalerType.Function,
                Arg1MarshalerType = arg1._signatureType.Type,
                Arg2MarshalerType = arg2._signatureType.Type,
                Arg3MarshalerType = arg3._signatureType.Type,
                ResultMarshalerType = result._signatureType.Type,
            });
        }

        internal static void CheckNullable(JSMarshalerType underlyingSig)
        {
            MarshalerType underlying = underlyingSig._signatureType.Type;
            if (underlying == MarshalerType.Boolean
                || underlying == MarshalerType.Byte
                || underlying == MarshalerType.Int16
                || underlying == MarshalerType.Int32
                || underlying == MarshalerType.BigInt64
                || underlying == MarshalerType.Int52
                || underlying == MarshalerType.IntPtr
                || underlying == MarshalerType.Double
                || underlying == MarshalerType.Single
                || underlying == MarshalerType.Single
                || underlying == MarshalerType.Char
                || underlying == MarshalerType.DateTime
                || underlying == MarshalerType.DateTimeOffset
                ) return;
            throw new ArgumentException("Bad nullable value type");
        }

        internal static void CheckArray(JSMarshalerType underlyingSig)
        {
            MarshalerType underlying = underlyingSig._signatureType.Type;
            if (underlying == MarshalerType.Byte
                || underlying == MarshalerType.Int32
                || underlying == MarshalerType.Double
                || underlying == MarshalerType.String
                || underlying == MarshalerType.Object
                || underlying == MarshalerType.JSObject
                ) return;
            throw new ArgumentException("Bad array element type");
        }

        internal static void CheckArraySegment(JSMarshalerType underlyingSig)
        {
            MarshalerType underlying = underlyingSig._signatureType.Type;
            if (underlying == MarshalerType.Byte
                || underlying == MarshalerType.Int32
                || underlying == MarshalerType.Double
                ) return;
            throw new ArgumentException("Bad array element type");
        }

        internal static void CheckTask(JSMarshalerType underlyingSig)
        {
            MarshalerType underlying = underlyingSig._signatureType.Type;
            // TODO maybe allow Task<byte[]> and Task<int[]> which don't need element marshaler
            if (underlying == MarshalerType.Array
                || underlying == MarshalerType.ArraySegment
                || underlying == MarshalerType.Span

                || underlying == MarshalerType.Task
                || underlying == MarshalerType.Action
                || underlying == MarshalerType.Function
                )
            {
                throw new ArgumentException("Bad task result type");
            }
        }
    }
}
