// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using Internal.TypeSystem;

namespace ILVerify
{
    /// <summary>
    /// Describes a problem that a <see cref="Verifier"/> found.
    /// </summary>
    public class VerificationResult
    {
        /// <summary>
        /// Gets the error that the verifier found, or <see cref="VerifierError.None"/> if the result reports a failure
        /// that has no error code. In that case, <see cref="Message"/> describes the failure.
        /// </summary>
        public VerifierError Code { get; internal set; }
        /// <summary>
        /// Gets the identifier of the type system error recorded for this result, or <see langword="null"/> if
        /// none was recorded. Method verification records it when such an error stops the verification of a method;
        /// type verification does not record it.
        /// </summary>
        public ExceptionStringID? ExceptionID { get; internal set; }
        /// <summary>
        /// Gets the type definition that the result was reported for, if the result comes from verifying the type's
        /// metadata; otherwise, a nil handle.
        /// </summary>
        public TypeDefinitionHandle Type { get; internal set; }
        /// <summary>
        /// Gets the method definition that the result was reported for, if the result comes from verifying the method's
        /// IL; otherwise, a nil handle.
        /// </summary>
        public MethodDefinitionHandle Method { get; internal set; }
        internal EntityHandle MetadataHandle { get; set; }
        /// <summary>
        /// Gets a description of the problem.
        /// </summary>
        /// <remarks>
        /// For a result from IL verification, the message does not include the details in <see cref="ErrorArguments"/>,
        /// such as the IL offset. For a result from type verification, the message can contain format items whose
        /// values are in <see cref="Args"/>.
        /// </remarks>
        public string Message { get; internal set; }
        /// <summary>
        /// Gets the values of the format items in <see cref="Message"/> for a result from type verification;
        /// otherwise, <see langword="null"/>.
        /// </summary>
        public object[] Args { get; internal set; }
        /// <summary>
        /// Gets or sets named values that give details of the problem.
        /// </summary>
        /// <remarks>
        /// When IL verification records named details for a problem, they include <c>Offset</c>, an
        /// <see cref="int"/> that holds the IL offset the problem was found at. They can also include <c>Found</c>,
        /// a string that describes what the verifier found, and optionally <c>Expected</c>, a string that describes
        /// what it expected. Alternatively, they can include <c>Token</c>, an <see cref="int"/> that holds the
        /// metadata token that could not be resolved. A result created from an exception during method verification
        /// has no named details. The array is empty when no named details were recorded.
        /// </remarks>
        public ErrorArgument[] ErrorArguments { get; set; } = Array.Empty<ErrorArgument>();

        /// <summary>
        /// Gets the value of the named error argument.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="name">The name of the argument, such as <c>Offset</c>.</param>
        /// <returns>
        /// The value of the first argument in <see cref="ErrorArguments"/> named <paramref name="name"/>, or the
        /// default value of <typeparamref name="T"/> if there is no such argument.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// The value of the argument is not <see langword="null"/> and cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// The value of the argument is <see langword="null"/> and <typeparamref name="T"/> is a
        /// non-nullable value type.
        /// </exception>
        public T GetArgumentValue<T>(string name)
        {
            TryGetArgumentValue<T>(name, out T argumentValue);
            return argumentValue;
        }

        /// <summary>
        /// Gets the value of the named error argument, if the result has one.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="name">The name of the argument, such as <c>Found</c>.</param>
        /// <param name="argumentValue">
        /// When this method returns, the value of the first argument in <see cref="ErrorArguments"/> named
        /// <paramref name="name"/>, or the default value of <typeparamref name="T"/> if there is no such argument.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <see cref="ErrorArguments"/> contains an argument named <paramref name="name"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// The value of the argument is not <see langword="null"/> and cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="NullReferenceException">
        /// The value of the argument is <see langword="null"/> and <typeparamref name="T"/> is a
        /// non-nullable value type.
        /// </exception>
        public bool TryGetArgumentValue<T>(string name,out T argumentValue)
        {
            argumentValue = default;

            foreach (ErrorArgument argument in ErrorArguments)
            {
                if (argument.Name == name)
                {
                    argumentValue = (T)argument.Value;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Represents a named value that gives a detail of a <see cref="VerificationResult"/>.
    /// </summary>
    public class ErrorArgument
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorArgument"/> class whose <see cref="Name"/> and
        /// <see cref="Value"/> are <see langword="null"/>.
        /// </summary>
        public ErrorArgument() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorArgument"/> class with the specified name and value.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="value">The value of the argument.</param>
        public ErrorArgument(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the argument.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the value of the argument.
        /// </summary>
        public object Value { get; }
    }
}
