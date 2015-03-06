// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    Provides information about the properties and events
    ///    for a component. This class cannot be inherited.
    ///
    ///    This is only a stub to support the TypeConverter scenario.
    ///
    /// </devdoc>
    public sealed class TypeDescriptor
    {
        private TypeDescriptor()
        {
        }

        /// <devdoc>
        ///    Gets a type converter for the specified type.
        /// </devdoc>
        public static TypeConverter GetConverter(Type type)
        {
            return ReflectTypeDescriptionProvider.GetConverter(type);
        }
    }
}