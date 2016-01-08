namespace System.Reflection.Metadata.Decoding
{
    [Flags]
    public enum SignatureDecoderOptions
    {
        /// <summary>
        /// Disable all options (default when no options are passed).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Causes the decoder to pass <see cref="SignatureTypeHandleCode.Class"/> or <see cref="SignatureTypeHandleCode.ValueType"/>
        /// to the <see cref="ITypeProvider{TType}"/> instead of <see cref="SignatureTypeHandleCode.Unresolved"/>.
        /// </summary>
        /// <remarks>
        /// There is additional overhead for this case when dealing with .winmd files to handle projection.
        /// Most scenarios will end up resolving valuetype vs. class from the actual definitions and do not
        /// need to know which was used in the signature. As such, it is not enabled by default.
        /// </remarks>
        DifferentiateClassAndValueTypes = 0x1,
    }
}
