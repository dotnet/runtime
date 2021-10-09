// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert component objects to and
    /// from various other representations.
    /// </summary>
    public class ComponentConverter : ReferenceConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ComponentConverter'/> class.
        /// </summary>
        public ComponentConverter(Type type) : base(type)
        {
        }

        /// <summary>
        /// Gets a collection of properties for the type of component
        /// specified by the value parameter.
        /// </summary>
        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            return TypeDescriptor.GetProperties(value, attributes);
        }

        /// <summary>
        /// Gets a value indicating whether this object supports properties using the
        /// specified context.
        /// </summary>
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}
