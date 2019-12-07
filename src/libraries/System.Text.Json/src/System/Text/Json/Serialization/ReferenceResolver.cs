namespace System.Text.Json
{
    /// <summary>
    /// Provides a generic view of the mechanism used to handle reference on (De)Serialization. This is an abstract class (this is an internal type but will be potentially exposed in a future).
    /// </summary>
    internal abstract class ReferenceResolver
    {
        /// <summary>
        /// Adds the specified key as a reference of the specified value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddReference(string key, object value);

        /// <summary>
        /// Gets a reference identifier for the specified value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract string GetReference(object value);

        /// <summary>
        /// Resolves the reference for the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract object ResolveReference(string key);

        /// <summary>
        /// Returns a value that indicates whether the specified object is a reference.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract bool IsReferenced(object value);
    }
}
