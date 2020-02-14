// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Security;
using System.Security.Permissions;

using Microsoft.Scripting.Utils;

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Dynamic")]
namespace Microsoft.Scripting.ComInterop {

    /// <summary>
    /// Provides helper methods to bind COM objects dynamically.
    /// </summary>
    public static class ComBinder {

        /// <summary>
        /// Determines if an object is a COM object.
        /// </summary>
        /// <param name="value">The object to test.</param>
        /// <returns>true if the object is a COM object, false otherwise.</returns>
        public static bool IsComObject(object value) {
            return Utils.TypeUtils.IsComObject(value);
        }

        public static bool CanComBind(object value) {
            return IsComObject(value) || value is IPseudoComObject;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic get member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <param name="delayInvocation">true if member evaluation may be delayed.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result, bool delayInvocation) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));

            if (TryGetMetaObject(ref instance)) {
                var comGetMember = new ComGetMemberBinder(binder, delayInvocation);
                result = instance.BindGetMember(comGetMember);
                if (result.Expression.Type.IsValueType) {
                    result = new DynamicMetaObject(
                        Expression.Convert(result.Expression, typeof(object)),
                        result.Restrictions
                    );
                }
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic get member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result) {
            return TryBindGetMember(binder, instance, out result, false);
        }

        /// <summary>
        /// Tries to perform binding of the dynamic set member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set member operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindSetMember(SetMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject value, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(value, nameof(value));

            if (TryGetMetaObject(ref instance)) {
                result = instance.BindSetMember(binder, value);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic invoke operation.
        /// </summary>    
        /// <param name="binder">An instance of the <see cref="InvokeBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke member operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindInvoke(InvokeBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(args, nameof(args));
            
            if (TryGetMetaObjectInvoke(ref instance)) {
                result = instance.BindInvoke(binder, args);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic invoke member operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="InvokeMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke member operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(args, nameof(args));

            if (TryGetMetaObject(ref instance)) {
                result = instance.BindInvokeMember(binder, args);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic get index operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke member operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindGetIndex(GetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(args, nameof(args));

            if (TryGetMetaObjectInvoke(ref instance)) {
                result = instance.BindGetIndex(binder, args);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic set index operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation. </param>
        /// <param name="args">An array of <see cref="DynamicMetaObject"/> instances - arguments to the invoke member operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set index operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryBindSetIndex(SetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, DynamicMetaObject value, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));
            ContractUtils.RequiresNotNull(args, nameof(args));
            ContractUtils.RequiresNotNull(value, nameof(value));

            if (TryGetMetaObjectInvoke(ref instance)) {
                result = instance.BindSetIndex(binder, args, value);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to perform binding of the dynamic Convert operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="ConvertBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="instance">The target of the dynamic operation.</param>
        /// <param name="result">The new <see cref="DynamicMetaObject"/> representing the result of the binding.</param>
        /// <returns>true if operation was bound successfully; otherwise, false.</returns>
        public static bool TryConvert(ConvertBinder binder, DynamicMetaObject instance, out DynamicMetaObject result) {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(instance, nameof(instance));

            if (IsComObject(instance.Value)) {

                // Converting a COM object to any interface is always considered possible - it will result in 
                // a QueryInterface at runtime
                if (binder.Type.IsInterface) {
                    result = new DynamicMetaObject(
                        Expression.Convert(
                            instance.Expression,
                            binder.Type
                        ),
                        BindingRestrictions.GetExpressionRestriction(
                            Expression.Call(
                                typeof(ComBinder).GetMethod(nameof(ComBinder.IsComObject), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public),
                                Helpers.Convert(instance.Expression, typeof(object))
                            )
                        )
                    );
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Gets the member names associated with the object.
        /// This function can operate only with objects for which <see cref="IsComObject"/> returns true.
        /// </summary>
        /// <param name="value">The object for which member names are requested.</param>
        /// <returns>The collection of member names.</returns>
        public static IEnumerable<string> GetDynamicMemberNames(object value) {
            ContractUtils.RequiresNotNull(value, nameof(value));
            ContractUtils.Requires(IsComObject(value), nameof(value), Strings.ComObjectExpected);

            return ComObject.ObjectToComObject(value).GetMemberNames(false);
        }

        /// <summary>
        /// Gets the member names of the data-like members associated with the object.
        /// This function can operate only with objects for which <see cref="IsComObject"/> returns true.
        /// </summary>
        /// <param name="value">The object for which member names are requested.</param>
        /// <returns>The collection of member names.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static IList<string> GetDynamicDataMemberNames(object value) {
            ContractUtils.RequiresNotNull(value, nameof(value));
            ContractUtils.Requires(IsComObject(value), nameof(value), Strings.ComObjectExpected);

            return ComObject.ObjectToComObject(value).GetMemberNames(true);
        }

        /// <summary>
        /// Gets the data-like members and associated data for an object.
        /// This function can operate only with objects for which <see cref="IsComObject"/> returns true.
        /// </summary>
        /// <param name="value">The object for which data members are requested.</param>
        /// <param name="names">The enumeration of names of data members for which to retrieve values.</param>
        /// <returns>The collection of pairs that represent data member's names and their data.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static IList<KeyValuePair<string, object>> GetDynamicDataMembers(object value, IEnumerable<string> names) {
            ContractUtils.RequiresNotNull(value, nameof(value));
            ContractUtils.Requires(IsComObject(value), nameof(value), Strings.ComObjectExpected);

            return ComObject.ObjectToComObject(value).GetMembers(names);
        }

        private static bool TryGetMetaObject(ref DynamicMetaObject instance) {
            // If we're already a COM MO don't make a new one
            // (we do this to prevent recursion if we call Fallback from COM)
            if (instance is ComUnwrappedMetaObject) {
                return false;
            }

            if (IsComObject(instance.Value)) {
                instance = new ComMetaObject(instance.Expression, instance.Restrictions, instance.Value);
                return true;
            }

            return false;
        }

        private static bool TryGetMetaObjectInvoke(ref DynamicMetaObject instance) {
            // If we're already a COM MO don't make a new one
            // (we do this to prevent recursion if we call Fallback from COM)
            if (TryGetMetaObject(ref instance)) {
                return true;
            }

            if (instance.Value is IPseudoComObject o) {
                instance = o.GetMetaObject(instance.Expression);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Special binder that indicates special semantics for COM GetMember operation.
        /// </summary>
        internal class ComGetMemberBinder : GetMemberBinder {
            private readonly GetMemberBinder _originalBinder;
            internal bool _CanReturnCallables;

            internal ComGetMemberBinder(GetMemberBinder originalBinder, bool CanReturnCallables) :
                base(originalBinder.Name, originalBinder.IgnoreCase) {
                _originalBinder = originalBinder;
                _CanReturnCallables = CanReturnCallables;
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion) {
                return _originalBinder.FallbackGetMember(target, errorSuggestion);
            }

            public override int GetHashCode() {
                return _originalBinder.GetHashCode() ^ (_CanReturnCallables ? 1 : 0);
            }

            public override bool Equals(object obj) {
                return obj is ComGetMemberBinder other &&
                    _CanReturnCallables == other._CanReturnCallables &&
                    _originalBinder.Equals(other._originalBinder);
            }
        }
    }
}

#endif
