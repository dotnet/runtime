// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    //
    // Note: these helpers are kept as simple wrappers so they have a better 
    // chance of being inlined.
    //
    public static partial class RuntimeOps
    {
        /// <summary>
        /// Gets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <param name="value">The out parameter containing the value of the member.</param>
        /// <returns>True if the member exists in the expando object, otherwise false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryGetValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase, out object value)
        {
            return expando.TryGetValue(indexClass, index, name, ignoreCase, out value);
        }

        /// <summary>
        /// Sets the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="value">The value of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>
        /// Returns the index for the set member.
        /// </returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static object ExpandoTrySetValue(ExpandoObject expando, object indexClass, int index, object value, string name, bool ignoreCase)
        {
            expando.TrySetValue(indexClass, index, value, name, ignoreCase, false);
            return value;
        }

        /// <summary>
        /// Deletes the value of an item in an expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="indexClass">The class of the expando object.</param>
        /// <param name="index">The index of the member.</param>
        /// <param name="name">The name of the member.</param>
        /// <param name="ignoreCase">true if the name should be matched ignoring case; false otherwise.</param>
        /// <returns>true if the item was successfully removed; otherwise, false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoTryDeleteValue(ExpandoObject expando, object indexClass, int index, string name, bool ignoreCase)
        {
            return expando.TryDeleteValue(indexClass, index, name, ignoreCase, ExpandoObject.Uninitialized);
        }

        /// <summary>
        /// Checks the version of the expando object.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="version">The version to check.</param>
        /// <returns>true if the version is equal; otherwise, false.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ExpandoCheckVersion(ExpandoObject expando, object version)
        {
            return expando.Class == version;
        }

        /// <summary>
        /// Promotes an expando object from one class to a new class.
        /// </summary>
        /// <param name="expando">The expando object.</param>
        /// <param name="oldClass">The old class of the expando object.</param>
        /// <param name="newClass">The new class of the expando object.</param>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static void ExpandoPromoteClass(ExpandoObject expando, object oldClass, object newClass)
        {
            expando.PromoteClass(oldClass, newClass);
        }
    }
}

