using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// IOptions wrapper that returns the options instance.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class, new()
    {
        /// <summary>
        /// Intializes the wrapper with the options instance to return.
        /// </summary>
        /// <param name="options">The options instance to return.</param>
        public OptionsWrapper(TOptions options)
        {
            Value = options;
        }

        /// <summary>
        /// The options instance.
        /// </summary>
        public TOptions Value { get; }

        /// <summary>
        /// This method is obsolete and will be removed in a future version.
        /// </summary>
        [Obsolete("This method is obsolete and will be removed in a future version.")]
        public void Add(string name, TOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is obsolete and will be removed in a future version.
        /// </summary>
        /// <param name="name">This parameter is ignored.</param>
        /// <returns>The <see cref="Value"/>.</returns>
        [Obsolete("This method is obsolete and will be removed in a future version.")]
        public TOptions Get(string name)
        {
            return Value;
        }

        /// <summary>
        /// This method is obsolete and will be removed in a future version.
        /// </summary>
        [Obsolete("This method is obsolete and will be removed in a future version.")]
        public bool Remove(string name)
        {
            throw new NotImplementedException();
        }
    }
}
