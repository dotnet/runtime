// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Attribute used to provide a custom marshaller type or size information for marshalling.
    /// </summary>
    /// <remarks>
    /// This attribute is recognized by the runtime-provided source generators for source-generated interop scenarios.
    /// It is not used by the interop marshalling system at runtime.
    /// <seealso cref="LibraryImportAttribute"/>
    /// <seealso cref="CustomTypeMarshallerAttribute" />
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
    public sealed class MarshalUsingAttribute : Attribute
    {
        /// <summary>
        /// Create a <see cref="MarshalUsingAttribute" /> that provides only size information.
        /// </summary>
        public MarshalUsingAttribute()
        {
            CountElementName = string.Empty;
        }

        /// <summary>
        /// Create a <see cref="MarshalUsingAttribute" /> that provides a native marshalling type and optionally size information.
        /// </summary>
        /// <param name="nativeType">The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomTypeMarshallerAttribute" /></param>
        public MarshalUsingAttribute(Type nativeType)
            : this()
        {
            NativeType = nativeType;
        }

        /// <summary>
        /// The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomTypeMarshallerAttribute" />
        /// </summary>
        public Type? NativeType { get; }

        /// <summary>
        /// The name of the parameter that will provide the size of the collection when marshalling from unmanaged to managed, or <see cref="ReturnsCountValue" /> if the return value provides the size.
        /// </summary>
        /// <remarks>
        /// Cannot be provided when <see cref="ConstantElementCount" /> is set.
        /// </remarks>
        public string CountElementName { get; set; }

        /// <summary>
        /// If a collection is constant size, the size of the collection when marshalling from unmanaged to managed.
        /// </summary>
        /// <remarks>
        /// Cannot be provided when <see cref="CountElementName" /> is set.
        /// </remarks>
        public int ConstantElementCount { get; set; }

        /// <summary>
        /// What indirection depth this marshalling info is provided for.
        /// </summary>
        /// <remarks>
        /// This value corresponds to how many pointer indirections would be required to get to the corresponding value from the native representation.
        /// For example, this attribute is on a parameter of type <see cref="int" />[][], then an <see cref="ElementIndirectionDepth"/> of 0 means that the marshalling info applies to the managed type of <see cref="int" />[][],
        /// an <see cref="ElementIndirectionDepth"/> of 1 applies to the managed type of <see cref="int" />[], and an <see cref="ElementIndirectionDepth"/> of 2 applies to the managed type of <see cref="int" />.
        /// Only one <see cref="MarshalUsingAttribute" /> with a given <see cref="ElementIndirectionDepth" /> can be provided on a given parameter or return value.
        /// </remarks>
        public int ElementIndirectionDepth { get; set; }

        /// <summary>
        /// A constant string that represents the name of the return value for <see cref="CountElementName" />.
        /// </summary>
        public const string ReturnsCountValue = "return-value";
    }
}
