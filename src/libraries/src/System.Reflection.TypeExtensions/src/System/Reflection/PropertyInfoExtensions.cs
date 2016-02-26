// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    /// <summary>
    /// Extension methods offering source-code compatibility with certain instance methods of <see cref="System.Reflection.PropertyInfo"/> on other platforms.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Returns an array whose elements reflect the public get, set, and other accessors of the property reflected by the current instance
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <returns>An array of MethodInfo objects that reflect the public get, set, and other accessors of the property reflected by the current instance, if found; otherwise, this method returns an array with zero (0) elements.</returns>
        public static MethodInfo[] GetAccessors(this PropertyInfo property)
        {
            return GetAccessors(property, nonPublic: false);
        }

        /// <summary>
        /// Returns an array whose elements reflect the public and, if specified, non-public get, set, and other accessors of the property reflected by the current instance.
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <param name="nonPublic">Indicates whether non-public methods should be returned in the MethodInfo array. true if non-public methods are to be included; otherwise, false. </param>
        /// <returns>An array of MethodInfo objects whose elements reflect the get, set, and other accessors of the property reflected by the current instance. If nonPublic is true, this array contains public and non-public get, set, and other accessors. If nonPublic is false, this array contains only public get, set, and other accessors. If no accessors with the specified visibility are found, this method returns an array with zero (0) elements. </returns>
        public static MethodInfo[] GetAccessors(this PropertyInfo property, bool nonPublic)
        {
            // NOTE: Technically, this isn't strictly compatible with desktop as there can be
            //       "other" accessors which are neither getters nor setters in metadata and
            //       desktop can return them. However, they are extraordinarily rare and there
            //       is no portable way to get them from the core reflection contract.

            Requires.NotNull(property, nameof(property));

            MethodInfo getMethod = Helpers.FilterAccessor(property.GetMethod, nonPublic);
            MethodInfo setMethod = Helpers.FilterAccessor(property.SetMethod, nonPublic);

            if (getMethod == null && setMethod == null)
            {
                return Helpers.EmptyMethodArray;
            }

            if (getMethod == null)
            {
                return new MethodInfo[] { setMethod };
            }

            if (setMethod == null)
            {
                return new MethodInfo[] { getMethod };
            }

            return new MethodInfo[] { getMethod, setMethod };
        }

        /// <summary>
        /// Returns the public get accessor for this property.
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <returns>A MethodInfo object representing the public get accessor for this property, or null if the get accessor is non-public or does not exist.</returns>
        public static MethodInfo GetGetMethod(this PropertyInfo property)
        {
            return GetGetMethod(property, nonPublic: false);
        }

        /// <summary>
        /// Returns the public or non-public get accessor for this property.
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <param name="nonPublic">Indicates whether a non-public get accessor should be returned. true if a non-public accessor is to be returned; otherwise, false. </param>
        /// <returns>A MethodInfo object representing the get accessor for this property, if nonPublic is true. Returns null if nonPublic is false and the get accessor is non-public, or if nonPublic is true but no get accessors exist.</returns>
        public static MethodInfo GetGetMethod(this PropertyInfo property, bool nonPublic)
        {
            Requires.NotNull(property, nameof(property));
            return Helpers.FilterAccessor(property.GetMethod, nonPublic);
        }

        /// <summary>
        /// Returns the public set accessor for this property.
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <returns>The MethodInfo object representing the Set method for this property if the set accessor is public, or null if the set accessor is not public.</returns>
        public static MethodInfo GetSetMethod(this PropertyInfo property)
        {
            return GetSetMethod(property, nonPublic: false);
        }

        /// <summary>
        /// Returns the set accessor for this property.
        /// </summary>
        /// <param name="property">PropertyInfo object on which to perform lookup</param>
        /// <param name="nonPublic">Indicates whether the accessor should be returned if it is non-public. true if a non-public accessor is to be returned; otherwise, false. </param>
        /// <returns>A MethodInfo object representing the Set method for this property. 
        /// - or -
        /// null </returns>
        public static MethodInfo GetSetMethod(this PropertyInfo property, bool nonPublic)
        {
            Requires.NotNull(property, nameof(property));
            return Helpers.FilterAccessor(property.SetMethod, nonPublic);
        }
    }
}
