// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Attribute to provide a hint to the presentation layer about what control it should use
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class UIHintAttribute : Attribute
    {
        private readonly UIHintImplementation _implementation;

        /// <summary>
        ///     Constructor that accepts the name of the control, without specifying which presentation layer to use
        /// </summary>
        /// <param name="uiHint">The name of the UI control.</param>
        public UIHintAttribute(string uiHint)
            : this(uiHint, null, Array.Empty<object>())
        {
        }

        /// <summary>
        ///     Constructor that accepts both the name of the control as well as the presentation layer
        /// </summary>
        /// <param name="uiHint">The name of the control to use</param>
        /// <param name="presentationLayer">The name of the presentation layer that supports this control</param>
        public UIHintAttribute(string uiHint, string? presentationLayer)
            : this(uiHint, presentationLayer, Array.Empty<object>())
        {
        }

        /// <summary>
        ///     Full constructor that accepts the name of the control, presentation layer, and optional parameters
        ///     to use when constructing the control
        /// </summary>
        /// <param name="uiHint">The name of the control</param>
        /// <param name="presentationLayer">The presentation layer</param>
        /// <param name="controlParameters">The list of parameters for the control</param>
        public UIHintAttribute(string uiHint, string? presentationLayer, params object?[]? controlParameters)
        {
            _implementation = new UIHintImplementation(uiHint, presentationLayer, controlParameters);
        }

        /// <summary>
        ///     Gets the name of the control that is most appropriate for this associated property or field
        /// </summary>
        public string UIHint => _implementation.UIHint;

        /// <summary>
        ///     Gets the name of the presentation layer that supports the control type in <see cref="UIHint" />
        /// </summary>
        public string? PresentationLayer => _implementation.PresentationLayer;

        /// <summary>
        ///     Gets the name-value pairs used as parameters to the control's constructor
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public IDictionary<string, object?> ControlParameters => _implementation.ControlParameters;

        public override int GetHashCode() => _implementation.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is UIHintAttribute otherAttribute && _implementation.Equals(otherAttribute._implementation);

        internal sealed class UIHintImplementation
        {
            private readonly object?[]? _inputControlParameters;
            private IDictionary<string, object?>? _controlParameters;

            public UIHintImplementation(string uiHint, string? presentationLayer, params object?[]? controlParameters)
            {
                UIHint = uiHint;
                PresentationLayer = presentationLayer;
                if (controlParameters != null)
                {
                    _inputControlParameters = new object[controlParameters.Length];
                    Array.Copy(controlParameters, _inputControlParameters, controlParameters.Length);
                }
            }

            /// <summary>
            ///     Gets the name of the control that is most appropriate for this associated property or field
            /// </summary>
            public string UIHint { get; }

            /// <summary>
            ///     Gets the name of the presentation layer that supports the control type in <see cref="UIHint" />
            /// </summary>
            public string? PresentationLayer { get; }

            // Lazy load the dictionary. It's fine if this method executes multiple times in stress scenarios.
            // If the method throws (indicating that the input params are invalid) this property will throw
            // every time it's accessed.
            public IDictionary<string, object?> ControlParameters =>
                _controlParameters ?? (_controlParameters = BuildControlParametersDictionary());

            /// <summary>
            ///     Returns the hash code for this UIHintAttribute.
            /// </summary>
            /// <returns>A 32-bit signed integer hash code.</returns>
            public override int GetHashCode()
            {
                var a = UIHint ?? string.Empty;
                var b = PresentationLayer ?? string.Empty;

                return a.GetHashCode() ^ b.GetHashCode();
            }

            /// <summary>
            ///     Determines whether this instance of UIHintAttribute and a specified object,
            ///     which must also be a UIHintAttribute object, have the same value.
            /// </summary>
            /// <param name="obj">An System.Object.</param>
            /// <returns>true if obj is a UIHintAttribute and its value is the same as this instance; otherwise, false.</returns>
            public override bool Equals(object? obj)
            {
                var otherImplementation = obj as UIHintImplementation;

                if (otherImplementation is null ||
                    UIHint != otherImplementation.UIHint ||
                    PresentationLayer != otherImplementation.PresentationLayer)
                {
                    return false;
                }

                IDictionary<string, object?> leftParams;
                IDictionary<string, object?> rightParams;

                try
                {
                    leftParams = ControlParameters;
                    rightParams = otherImplementation.ControlParameters;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }

                Debug.Assert(leftParams != null, "leftParams shouldn't be null");
                Debug.Assert(rightParams != null, "rightParams shouldn't be null");
                if (leftParams.Count != rightParams.Count)
                {
                    return false;
                }
                return leftParams.OrderBy(p => p.Key).SequenceEqual(rightParams.OrderBy(p => p.Key));
            }


            /// <summary>
            ///     Validates the input control parameters and throws InvalidOperationException if they are not correct.
            /// </summary>
            /// <returns>
            ///     Dictionary of control parameters.
            /// </returns>
            private IDictionary<string, object?> BuildControlParametersDictionary()
            {
                IDictionary<string, object?> controlParameters = new Dictionary<string, object?>();

                object?[]? inputControlParameters = _inputControlParameters;

                if (inputControlParameters == null || inputControlParameters.Length == 0)
                {
                    return controlParameters;
                }
                if (inputControlParameters.Length % 2 != 0)
                {
                    throw new InvalidOperationException(SR.UIHintImplementation_NeedEvenNumberOfControlParameters);
                }

                for (int i = 0; i < inputControlParameters.Length; i += 2)
                {
                    object? key = inputControlParameters[i];
                    object? value = inputControlParameters[i + 1];
                    if (key == null)
                    {
                        throw new InvalidOperationException(SR.Format(SR.UIHintImplementation_ControlParameterKeyIsNull, i));
                    }

                    if (!(key is string keyString))
                    {
                        throw new InvalidOperationException(SR.Format(SR.UIHintImplementation_ControlParameterKeyIsNotAString,
                                                            i,
                                                            inputControlParameters[i]!.ToString()));
                    }

                    if (controlParameters.ContainsKey(keyString))
                    {
                        throw new InvalidOperationException(SR.Format(SR.UIHintImplementation_ControlParameterKeyOccursMoreThanOnce,
                                                            i,
                                                            keyString));
                    }

                    controlParameters[keyString] = value;
                }

                return controlParameters;
            }
        }
    }
}
