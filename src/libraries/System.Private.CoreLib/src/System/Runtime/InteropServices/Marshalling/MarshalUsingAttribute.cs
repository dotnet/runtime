// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Provides type or size information to a custom marshaller.
    /// </summary>
    /// <remarks>
    /// This attribute is recognized by the runtime-provided source generators for source-generated interop scenarios.
    /// It's not used by the interop marshalling system at run time.
    /// </remarks>
    /// <seealso cref="LibraryImportAttribute" />
    /// <seealso cref="CustomMarshallerAttribute" />
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public sealed class MarshalUsingAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarshalUsingAttribute" /> class that provides only size information.
        /// </summary>
        public MarshalUsingAttribute()
        {
            CountElementName = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarshalUsingAttribute" /> class that provides a native marshalling type and optionally size information.
        /// </summary>
        /// <param name="nativeType">The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" />.</param>
        public MarshalUsingAttribute(Type nativeType)
            : this()
        {
            NativeType = nativeType;
        }

        /// <summary>
        /// Gets the marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" />.
        /// </summary>
        public Type? NativeType { get; }

        /// <summary>
        /// Gets or sets the name of the parameter that will provide the size of the collection when marshalling from unmanaged to managed, or <see cref="ReturnsCountValue" /> if the return value provides the size.
        /// </summary>
        /// <remarks>
        /// This property cannot be provided when <see cref="ConstantElementCount" /> is set.
        /// </remarks>
        public string CountElementName { get; set; }

        /// <summary>
        /// Gets or sets the size of the collection when marshalling from unmanaged to managed, if the collection is constant size.
        /// </summary>
        /// <remarks>
        /// This property cannot be provided when <see cref="CountElementName" /> is set.
        /// </remarks>
        public int ConstantElementCount { get; set; }

        /// <summary>
        /// Gets or sets the indirection depth this marshalling info is provided for.
        /// </summary>
        /// <remarks>
        /// This value corresponds to how many pointer indirections would be required to get to the corresponding value from the native representation.
        /// For example, if this attribute is on a parameter of type <see cref="int" />[][], then an <see cref="ElementIndirectionDepth"/> of 0 means that the marshalling info applies to the managed type of <see cref="int" />[][],
        /// an <see cref="ElementIndirectionDepth"/> of 1 applies to the managed type of <see cref="int" />[], and an <see cref="ElementIndirectionDepth"/> of 2 applies to the managed type of <see cref="int" />.
        /// Only one <see cref="MarshalUsingAttribute" /> with a given <see cref="ElementIndirectionDepth" /> can be provided on a given parameter or return value.
        /// </remarks>
        public int ElementIndirectionDepth { get; set; }

        /// <summary>
        /// Represents the name of the return value for <see cref="CountElementName" />.
        /// </summary>
        public const string ReturnsCountValue = "return-value";
    }
}
