// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Specifies that a type, property, field, or method provides a particular export.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class MetadataViewImplementationAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MetadataViewImplementationAttribute"/> class, declaring the
        ///     type that holds the implementation for the view.
        /// </summary>
        /// <param name="implementationType">
        ///     A <see cref="Type"/> for the implementation of the MetadataView.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         By default MetadataViews are generated using reflection emit.  This attribute 
        ///         allows the developer to specify the ttype that implements the view rather than 
        ///         using a generated type.
        ///     </para>
        /// </remarks>
        public MetadataViewImplementationAttribute(Type implementationType)
        {
            ImplementationType = implementationType;
        }

        /// <summary>
        ///     Get the type that is used to implement the view to which this attribute is attached.
        /// </summary>
        /// <value>
        ///     A <see cref="Type"/> of the export that is be provided. The default value is
        ///     <see langword="null"/> which means that the type will be obtained by looking at the type on
        ///     the member that this export is attached to. 
        /// </value>
        public Type ImplementationType { get; private set; }
    }
}
