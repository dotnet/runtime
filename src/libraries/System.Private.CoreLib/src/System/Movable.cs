// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// The "using" keyword guarantees exception-safety in a specific context by disposing the
    /// associated resources even if an exception is thrown in the block. However, RAII is not always
    /// appreciated when using resources.
    ///
    /// <example>
    /// <code>
    /// // "Resource" is a class that implements the interface "IDisposable"
    /// Resource resource = new Resource();
    ///
    /// // "Configure" is a method to configure the "resource"; "currentContext" represents a capture of runtime context
    /// Configure(resource, currentContext);
    ///
    /// return resource;
    /// </code>
    /// </example>
    ///
    /// The code will work fine if every function call returns normally. However, when an exception is
    /// thrown during configuration (the second line), the "resource" leaks. We do not have this issue in
    /// C++ because of move semantics built on top of value semantics.
    ///
    /// With the proposed facilities, the code above could be refactored into:
    ///
    /// <example>
    /// <code>
    /// using (Movable&lt;Resource&gt; resource = new Resource())
    /// {
    ///     Configure(resource.Value, currentContext);<br />
    ///     return resource.Move();
    /// }
    /// </code>
    /// </example>
    ///
    /// The mechanism is similar to the "move semantics" in C++ to ensure exception-safety but has more
    /// strict preconditions to avoid abuse.
    /// </summary>
    /// <typeparam name="TResource">The underlying type of the associated resource.</typeparam>
    public struct Movable<TResource> : IDisposable where TResource : class, IDisposable
    {
        private TResource? _resource;

        /// <summary>
        /// Initializes a new instance of the <c>Movable&lt;TResource&gt;</c> structure with the specific resource.
        /// This method throws <c>ArgumentNullException</c> if <c>resource</c> is <c>null</c>.
        /// </summary>
        /// <param name="resource">The resource held by the created instance.</param>
        public Movable(TResource resource)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        /// <summary>
        /// Gets a value indicating whether this is associated with a valid resource.
        /// </summary>
        public bool HasValue => _resource != null;

        /// <summary>
        /// Get the resource associated with <c>this</c>.
        /// This method throws <c>InvalidOperationException</c> if this is not associated with a valid resource.
        /// </summary>
        public TResource Value => GetResourceWithValidation("The resource has been moved before, and could no longer be accessed.");

        /// <summary>
        /// Initializes a new instance of the <c>Movable&lt;TResource&gt;</c> structure with the specific resource.
        /// This method throws <c>ArgumentNullException</c> if <c>resource</c> is <c>null</c>.
        /// </summary>
        /// <param name="resource">The resource held by the created instance.</param>
        public static implicit operator Movable<TResource>(TResource resource) => new Movable<TResource>(resource);

        /// <summary>
        /// Get the resource associated with <c>this</c>.
        /// This method throws <c>InvalidOperationException</c> if movable is not associated with a valid resource.
        /// </summary>
        /// <param name="movable">The movable resource</param>
        public static explicit operator TResource(Movable<TResource> movable) => movable.Value;

        /// <summary>
        /// Move the resource out of <c>this</c>, and <c>this</c> will no longer associate with a valid resource during its lifetime.
        /// </summary>
        /// <returns>The resource associated with <c>this</c>.</returns>
        public TResource Move()
        {
            TResource result = GetResourceWithValidation("The resource has been moved before. Please pay attention to resource leak if there is any unmanaged one in the context.");
            _resource = null;
            return result;
        }

        /// <summary>
        /// If <c>this</c> is associated with an underlying resource, call <c>Dispose()</c> on it. <c>this</c> will no longer associate with a
        /// valid resource during its lifetime.
        /// </summary>
        public void Dispose()
        {
            if (_resource != null)
            {
                _resource.Dispose();
                _resource = null;
            }
        }

        /// <summary>
        /// Returns the text representation of the value of the current object.
        /// </summary>
        /// <returns>The text representation of the value of the current object</returns>
        public override string ToString() => $"{GetType().Name}[{_resource}]";

        private TResource GetResourceWithValidation(string errorMessage) => _resource ?? throw new InvalidOperationException(errorMessage);
    }
}
