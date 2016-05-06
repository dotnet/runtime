// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    Provides information about the properties and events
    ///    for a component. This class cannot be inherited.
    ///
    ///    This is only a stub to support the TypeConverter scenario.
    /// </summary>
    public sealed class TypeDescriptor
    {
        private TypeDescriptor()
        {
        }

        /// <summary>
        ///    Gets a type converter for the specified type.
        /// </summary>
        public static TypeConverter GetConverter(Type type)
        {
            return ReflectTypeDescriptionProvider.GetConverter(type);
        }
    }
}
