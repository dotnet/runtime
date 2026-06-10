// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET11_0_OR_GREATER

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Async validation implementation for <see cref="DataAnnotationValidateOptions{TOptions}"/>.
    /// </summary>
    public partial class DataAnnotationValidateOptions<TOptions>
    {
        /// <summary>
        /// Asynchronously validates a specific named options instance (or all when <paramref name="name"/> is null).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        /// <remarks>
        /// The <paramref name="cancellationToken"/> is propagated from
        /// <c>Host.StartAsync(CancellationToken)</c>. By default, no startup timeout
        /// is applied. Configure <see cref="T:Microsoft.Extensions.Hosting.HostOptions.StartupTimeout"/> or pass
        /// a <see cref="CancellationToken"/> with a timeout to <c>Host.StartAsync</c>
        /// to bound I/O-bound async validators:
        /// <code>
        /// builder.Services.Configure&lt;HostOptions&gt;(opts =&gt;
        ///     opts.StartupTimeout = TimeSpan.FromSeconds(30));
        /// </code>
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Suppressing the warnings on this method since the constructor of the type is annotated as RequiresUnreferencedCode.")]
        public async Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default)
        {
            // Null name is used to configure all named options.
            if (Name is not null && Name != name)
            {
                // Ignored if not validating this instance.
                return ValidateOptionsResult.Skip;
            }

            // Ensure options are provided to validate against
            ArgumentNullException.ThrowIfNull(options);

            var validationResults = new List<ValidationResult>();
            HashSet<object>? visited = null;
            List<string>? errors = null;

            (bool success, errors) = await TryValidateOptionsAsync(options, options.GetType().Name, validationResults, errors, visited, cancellationToken).ConfigureAwait(false);

            if (success)
            {
                return ValidateOptionsResult.Success;
            }

            Debug.Assert(errors is not null && errors.Count > 0);

            return ValidateOptionsResult.Fail(errors);
        }

        /// <remarks>
        /// Async counterpart of <see cref="TryValidateOptions"/>. Uses a tuple return
        /// <c>(bool success, List&lt;string&gt;? errors)</c> instead of <c>ref</c> parameters
        /// because <c>async</c> methods cannot have <c>ref</c>/<c>out</c> parameters.
        ///
        /// <c>visited</c> does not need to be returned — it is created <em>before</em> each
        /// recursive call, so the callee always receives a non-null, shared instance.
        /// <c>errors</c> must be returned because it may be lazily created
        /// (<c>errors ??= new List&lt;string&gt;()</c>) inside the callee.
        /// </remarks>
        [RequiresUnreferencedCode("This method on this type will walk through all properties of the passed in options object, and its type cannot be " +
            "statically analyzed so its members may be trimmed.")]
        private static async Task<(bool success, List<string>? errors)> TryValidateOptionsAsync(
            object options,
            string qualifiedName,
            List<ValidationResult> results,
            List<string>? errors,
            HashSet<object>? visited,
            CancellationToken cancellationToken)
        {
            Debug.Assert(options is not null);

            if (visited is not null && visited.Contains(options))
            {
                return (true, errors);
            }

            results.Clear();

            bool res = await Validator.TryValidateObjectAsync(options, new ValidationContext(options), results, validateAllProperties: true, cancellationToken).ConfigureAwait(false);
            if (!res)
            {
                errors ??= new List<string>();

                foreach (ValidationResult result in results)
                {
                    errors.Add($"DataAnnotation validation failed for '{qualifiedName}' members: '{string.Join(",", result.MemberNames)}' with the error: '{result.ErrorMessage}'.");
                }
            }

            foreach (PropertyInfo propertyInfo in options.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                // Indexers are properties which take parameters. Ignore them.
                if (propertyInfo.GetMethod is null || propertyInfo.GetMethod.GetParameters().Length > 0)
                {
                    continue;
                }

                object? value = propertyInfo.GetValue(options);

                if (value is null)
                {
                    continue;
                }

                if (propertyInfo.GetCustomAttribute<ValidateObjectMembersAttribute>() is not null)
                {
                    visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
                    visited.Add(options);

                    bool innerRes;
                    (innerRes, errors) = await TryValidateOptionsAsync(value, $"{qualifiedName}.{propertyInfo.Name}", results, errors, visited, cancellationToken).ConfigureAwait(false);
                    res = innerRes && res;
                }
                else if (value is IEnumerable enumerable &&
                         propertyInfo.GetCustomAttribute<ValidateEnumeratedItemsAttribute>() is not null)
                {
                    visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
                    visited.Add(options);

                    int index = 0;
                    foreach (object item in enumerable)
                    {
                        bool innerRes;
                        (innerRes, errors) = await TryValidateOptionsAsync(item, $"{qualifiedName}.{propertyInfo.Name}[{index++}]", results, errors, visited, cancellationToken).ConfigureAwait(false);
                        res = innerRes && res;
                    }
                }
            }

            return (res, errors);
        }
    }
}

#endif
