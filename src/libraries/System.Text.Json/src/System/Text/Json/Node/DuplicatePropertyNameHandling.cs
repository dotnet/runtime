namespace System.Text.Json
{
    /// <summary>
    /// Specifies how duplicate property names are handled when added to JSON object.
    /// </summary>
    public enum DuplicatePropertyNameHandling
    {
        /// <summary>
        /// Replace the existing value when there is a duplicate property. The value of the last property in the JSON object will be used.
        /// </summary>
        Replace,
        /// <summary>
        /// Ignore the new value when there is a duplicate property. The value of the first property in the JSON object will be used.
        /// </summary>
        Ignore,
        /// <summary>
        /// Throw an <exception cref="ArgumentException"/> when a duplicate property is encountered.
        /// </summary>
        Error,
    }
}
