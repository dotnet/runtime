// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Dynamic.Utils
{
    /// <summary>
    ///    Strongly-typed and parameterized string resources.
    /// </summary>
    internal static partial class Strings
    {
        /// <summary>
        /// A string like "Argument count must be greater than number of named arguments."
        /// </summary>
        internal static string ArgCntMustBeGreaterThanNameCnt
        {
            get
            {
                return SR.ArgCntMustBeGreaterThanNameCnt;
            }
        }

        /// <summary>
        /// A string like "The result type '{0}' of the dynamic binding produced by the object with type '{1}' for the binder '{2}' is not compatible with the result type '{3}' expected by the call site."
        /// </summary>
        internal static string DynamicObjectResultNotAssignable(object p0, object p1, object p2, object p3)
        {
            return SR.Format(SR.DynamicObjectResultNotAssignable, p0, p1, p2, p3);
        }

        /// <summary>
        /// A string like "No or Invalid rule produced"
        /// </summary>
        internal static string NoOrInvalidRuleProduced
        {
            get
            {
                return SR.NoOrInvalidRuleProduced;
            }
        }

        /// <summary>
        /// A string like "Type parameter is {0}. Expected a delegate."
        /// </summary>
        internal static string TypeParameterIsNotDelegate(object p0)
        {
            return SR.Format(SR.TypeParameterIsNotDelegate, p0);
        }

        /// <summary>
        /// A string like "First argument of delegate must be CallSite"
        /// </summary>
        internal static string FirstArgumentMustBeCallSite
        {
            get
            {
                return SR.FirstArgumentMustBeCallSite;
            }
        }

        /// <summary>
        /// A string like "{0} must be greater than or equal to {1}"
        /// </summary>
        internal static string OutOfRange(object p0, object p1)
        {
            return SR.Format(SR.OutOfRange, p0, p1);
        }

        /// <summary>
        /// A string like "The result type '{0}' of the binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static string BinderNotCompatibleWithCallSite(object p0, object p1, object p2)
        {
            return SR.Format(SR.BinderNotCompatibleWithCallSite, p0, p1, p2);
        }

        /// <summary>
        /// A string like "The result type '{0}' of the dynamic binding produced by binder '{1}' is not compatible with the result type '{2}' expected by the call site."
        /// </summary>
        internal static string DynamicBinderResultNotAssignable(object p0, object p1, object p2)
        {
            return SR.Format(SR.DynamicBinderResultNotAssignable, p0, p1, p2);
        }

        /// <summary>
        /// A string like "The result of the dynamic binding produced by the object with type '{0}' for the binder '{1}' needs at least one restriction."
        /// </summary>
        internal static string DynamicBindingNeedsRestrictions(object p0, object p1)
        {
            return SR.Format(SR.DynamicBindingNeedsRestrictions, p0, p1);
        }

        /// <summary>
        /// A string like "Bind cannot return null."
        /// </summary>
        internal static string BindingCannotBeNull
        {
            get
            {
                return SR.BindingCannotBeNull;
            }
        }

        /// <summary>
        /// A string like "Type must be derived from System.Delegate"
        /// </summary>
        internal static string TypeMustBeDerivedFromSystemDelegate
        {
            get
            {
                return SR.TypeMustBeDerivedFromSystemDelegate;
            }
        }

        /// <summary>
        /// A string like "An IDynamicMetaObjectProvider {0} created an invalid DynamicMetaObject instance."
        /// </summary>
        internal static string InvalidMetaObjectCreated(object p0)
        {
            return SR.Format(SR.InvalidMetaObjectCreated, p0);
        }

        /// <summary>
        /// A string like "More than one key matching '{0}' was found in the ExpandoObject."
        /// </summary>
        internal static string AmbiguousMatchInExpandoObject(object p0)
        {
            return SR.Format(SR.AmbiguousMatchInExpandoObject, p0);
        }

        /// <summary>
        /// A string like "An element with the same key '{0}' already exists in the ExpandoObject."
        /// </summary>
        internal static string SameKeyExistsInExpando(object p0)
        {
            return SR.Format(SR.SameKeyExistsInExpando, p0);
        }

        /// <summary>
        /// A string like "The specified key '{0}' does not exist in the ExpandoObject."
        /// </summary>
        internal static string KeyDoesNotExistInExpando(object p0)
        {
            return SR.Format(SR.KeyDoesNotExistInExpando, p0);
        }

        /// <summary>
        /// A string like "Collection is read-only."
        /// </summary>
        internal static string CollectionReadOnly
        {
            get
            {
                return SR.CollectionReadOnly;
            }
        }

        /// <summary>
        /// A string like "Argument type cannot be void"
        /// </summary>
        internal static string ArgumentTypeCannotBeVoid
        {
            get
            {
                return SR.ArgumentTypeCannotBeVoid;
            }
        }

        /// <summary>
        /// A string like "Method precondition violated"
        /// </summary>
        internal static string MethodPreconditionViolated
        {
            get
            {
                return SR.MethodPreconditionViolated;
            }
        }
    }
}