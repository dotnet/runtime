// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop {
    /// <summary>
    /// The ComObject class wraps a runtime-callable-wrapper and enables it to be used with the Dynamic Language Runtime and the C# dynamic keyword.
    /// </summary>
    internal class ComObject : IDynamicMetaObjectProvider {
        internal ComObject(object rcw) {
            Debug.Assert(Utils.TypeUtils.IsComObject(rcw));
            RuntimeCallableWrapper = rcw;
        }

        internal object RuntimeCallableWrapper { get; }

        private readonly static object _ComObjectInfoKey = new object();

        /// <summary>
        /// Gets a <see cref="ComObject"/> that wraps the runtime-callable-wrapper, or creates one if none currently exists.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public static ComObject ObjectToComObject(object rcw) {
            Debug.Assert(Utils.TypeUtils.IsComObject(rcw));

            // Marshal.Get/SetComObjectData has a LinkDemand for UnmanagedCode which will turn into
            // a full demand. We could avoid this by making this method SecurityCritical
            object data = Marshal.GetComObjectData(rcw, _ComObjectInfoKey);
            if (data != null) {
                return (ComObject)data;
            }

            lock (_ComObjectInfoKey) {
                data = Marshal.GetComObjectData(rcw, _ComObjectInfoKey);
                if (data != null) {
                    return (ComObject)data;
                }

                ComObject comObjectInfo = CreateComObject(rcw);
                if (!Marshal.SetComObjectData(rcw, _ComObjectInfoKey, comObjectInfo)) {
                    throw Error.SetComObjectDataFailed();
                }

                return comObjectInfo;
            }
        }

        // Expression that unwraps ComObject
        internal static MemberExpression RcwFromComObject(Expression comObject) {
            Debug.Assert(comObject != null && (typeof(ComObject).IsAssignableFrom(comObject.Type) || comObject.Type == typeof(object)), "must be ComObject");

            return Expression.Property(
                Helpers.Convert(comObject, typeof(ComObject)),
                typeof(ComObject).GetProperty("RuntimeCallableWrapper", BindingFlags.NonPublic | BindingFlags.Instance)
            );
        }

        // Expression that finds or creates a ComObject that corresponds to given Rcw
        internal static MethodCallExpression RcwToComObject(Expression rcw) {
            return Expression.Call(
                typeof(ComObject).GetMethod("ObjectToComObject"),
                Helpers.Convert(rcw, typeof(object))
            );
        }

        private static ComObject CreateComObject(object rcw) {
            if (rcw is IDispatch dispatchObject) {
                // We can do method invocations on IDispatch objects
                return new IDispatchComObject(dispatchObject);
            }

            // There is not much we can do in this case
            return new ComObject(rcw);
        }

        internal virtual IList<string> GetMemberNames(bool dataOnly) {
            return new string[0];
        }

        internal virtual IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names) {
            return new KeyValuePair<string, object>[0];
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            return new ComFallbackMetaObject(parameter, BindingRestrictions.Empty, this);
        }
    }
}

#endif
