// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Dynamic.Utils
{
    /// <summary>
    ///    Strongly-typed and parameterized exception factory.
    /// </summary>
    internal static partial class Error
    {
        /// <summary>
        /// ArgumentException with message like "Argument count must be greater than number of named arguments."
        /// </summary>
        internal static Exception ArgCntMustBeGreaterThanNameCnt()
        {
            return new ArgumentException(Strings.ArgCntMustBeGreaterThanNameCnt);
        }
        /// <summary>
        /// InvalidCastException with message like "The result type '{0}' of the dynamic binding produced by the object with type '{1}' for the binder '{2}' is not compatible with the result type '{3}' expected by the call site."
        /// </summary>
        internal static Exception DynamicObjectResultNotAssignable(object p0, object p1, object p2, object p3)
        {
            return new InvalidCastException(Strings.DynamicObjectResultNotAssignable(p0, p1, p2, p3));
        }
        /// <summary>
        /// InvalidOperationException with message like "No or Invalid rule produced"
        /// </summary>
        internal static Exception NoOrInvalidRuleProduced()
        {
            return new InvalidOperationException(Strings.NoOrInvalidRuleProduced);
        }
        /// <summary>
        /// InvalidOperationException with message like "Type parameter is {0}. Expected a delegate."
        /// </summary>
        internal static Exception TypeParameterIsNotDelegate(object p0)
        {
            return new InvalidOperationException(Strings.TypeParameterIsNotDelegate(p0));
        }
        /// <summary>
        /// ArgumentException with message like "First argument of delegate must be CallSite"
        /// </summary>
        internal static Exception FirstArgumentMustBeCallSite()
        {
            return new ArgumentException(Strings.FirstArgumentMustBeCallSite);
        }
        /// <summary>
        /// ArgumentOutOfRangeException with message like "{0} must be greater than or equal to {1}"
        /// </summary>
        internal static Exception OutOfRange(object p0, object p1)
        {
            return new ArgumentOutOfRangeException(Strings.OutOfRange(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "The result type '{0}' of the binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static Exception BinderNotCompatibleWithCallSite(object p0, object p1, object p2)
        {
            return new InvalidOperationException(Strings.BinderNotCompatibleWithCallSite(p0, p1, p2));
        }
        /// <summary>
        /// InvalidCastException with message like "The result type '{0}' of the dynamic binding produced by binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static Exception DynamicBinderResultNotAssignable(object p0, object p1, object p2)
        {
            return new InvalidCastException(Strings.DynamicBinderResultNotAssignable(p0, p1, p2));
        }
        /// <summary>
        /// InvalidOperationException with message like "The result of the dynamic binding produced by the object with type '{0}' for the binder '{1}' needs at least one restriction."
        /// </summary>
        internal static Exception DynamicBindingNeedsRestrictions(object p0, object p1)
        {
            return new InvalidOperationException(Strings.DynamicBindingNeedsRestrictions(p0, p1));
        }
        /// <summary>
        /// InvalidOperationException with message like "Bind cannot return null."
        /// </summary>
        internal static Exception BindingCannotBeNull()
        {
            return new InvalidOperationException(Strings.BindingCannotBeNull);
        }
        /// <summary>
        /// ArgumentException with message like "Type must be derived from System.Delegate"
        /// </summary>
        internal static Exception TypeMustBeDerivedFromSystemDelegate()
        {
            return new ArgumentException(Strings.TypeMustBeDerivedFromSystemDelegate);
        }
        /// <summary>
        /// InvalidOperationException with message like "An IDynamicMetaObjectProvider {0} created an invalid DynamicMetaObject instance."
        /// </summary>
        internal static Exception InvalidMetaObjectCreated(object p0)
        {
            return new InvalidOperationException(Strings.InvalidMetaObjectCreated(p0));
        }
        /// <summary>
        /// System.Reflection.AmbiguousMatchException with message like "More than one key matching '{0}' was found in the ExpandoObject."
        /// </summary>
        internal static Exception AmbiguousMatchInExpandoObject(object p0)
        {
            return new System.Reflection.AmbiguousMatchException(Strings.AmbiguousMatchInExpandoObject(p0));
        }
        /// <summary>
        /// ArgumentException with message like "An element with the same key '{0}' already exists in the ExpandoObject."
        /// </summary>
        internal static Exception SameKeyExistsInExpando(object p0)
        {
            return new ArgumentException(Strings.SameKeyExistsInExpando(p0));
        }
        /// <summary>
        /// System.Collections.Generic.KeyNotFoundException with message like "The specified key '{0}' does not exist in the ExpandoObject."
        /// </summary>
        internal static Exception KeyDoesNotExistInExpando(object p0)
        {
            return new System.Collections.Generic.KeyNotFoundException(Strings.KeyDoesNotExistInExpando(p0));
        }
        /// <summary>
        /// NotSupportedException with message like "Collection is read-only."
        /// </summary>
        internal static Exception CollectionReadOnly()
        {
            return new NotSupportedException(Strings.CollectionReadOnly);
        }
        /// <summary>
        /// ArgumentException with message like "Argument type cannot be void"
        /// </summary>
        internal static Exception ArgumentTypeCannotBeVoid()
        {
            return new ArgumentException(Strings.ArgumentTypeCannotBeVoid);
        }
    }
}