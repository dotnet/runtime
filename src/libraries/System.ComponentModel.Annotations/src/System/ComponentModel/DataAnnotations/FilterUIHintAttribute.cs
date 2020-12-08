// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    /// An attribute used to specify the filtering behavior for a column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    [Obsolete("This attribute is no longer in use and will be ignored if applied.")]
    public sealed class FilterUIHintAttribute : Attribute
    {
        private readonly UIHintAttribute.UIHintImplementation _implementation;

        /// <summary>
        /// Gets the name of the control that is most appropriate for this associated
        /// property or field
        /// </summary>
        public string FilterUIHint => _implementation.UIHint;

        /// <summary>
        /// Gets the name of the presentation layer that supports the control type
        /// in <see cref="FilterUIHint"/>
        /// </summary>
        public string? PresentationLayer => _implementation.PresentationLayer;

        /// <summary>
        /// Gets the name-value pairs used as parameters to the control's constructor
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute
        /// is ill-formed.</exception>
        public IDictionary<string, object?> ControlParameters => _implementation.ControlParameters;

        /// <summary>
        /// Constructor that accepts the name of the control, without specifying
        /// which presentation layer to use
        /// </summary>
        /// <param name="filterUIHint">The name of the UI control.</param>
        public FilterUIHintAttribute(string filterUIHint)
            : this(filterUIHint, null, Array.Empty<object>())
        {
        }

        /// <summary>
        /// Constructor that accepts both the name of the control as well as the
        /// presentation layer
        /// </summary>
        /// <param name="filterUIHint">The name of the control to use</param>
        /// <param name="presentationLayer">The name of the presentation layer that
        /// supports this control</param>
        public FilterUIHintAttribute(string filterUIHint, string? presentationLayer)
            : this(filterUIHint, presentationLayer, Array.Empty<object>())
        {
        }

        /// <summary>
        /// Full constructor that accepts the name of the control, presentation layer,
        /// and optional parameters to use when constructing the control
        /// </summary>
        /// <param name="filterUIHint">The name of the control</param>
        /// <param name="presentationLayer">The presentation layer</param>
        /// <param name="controlParameters">The list of parameters for the control</param>
        public FilterUIHintAttribute(string filterUIHint, string? presentationLayer,
            params object?[] controlParameters)
        {
            _implementation = new UIHintAttribute.UIHintImplementation(
                filterUIHint, presentationLayer, controlParameters);
        }

        /// <summary>
        /// Returns the hash code for this FilterUIHintAttribute.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _implementation.GetHashCode();

        /// <summary>
        /// Determines whether this instance of FilterUIHintAttribute and a specified object,
        /// which must also be a FilterUIHintAttribute object, have the same value.
        /// </summary>
        /// <param name="obj">An System.Object.</param>
        /// <returns>true if obj is a FilterUIHintAttribute and its value is the same
        /// as this instance; otherwise, false.</returns>
        public override bool Equals(object? obj) =>
            obj is FilterUIHintAttribute otherAttribute && _implementation.Equals(otherAttribute._implementation);
    }
}
