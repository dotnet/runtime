// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    public interface IComObjectWrapper<T> { }

    [AttributeUsage(AttributeTargets.Interface)]
    public class GeneratedComInterfaceAttribute : Attribute
    {
        public GeneratedComInterfaceAttribute(Type comWrappersType)
            => (ComWrappersType) = (comWrappersType);

        public GeneratedComInterfaceAttribute(Type comWrappersType, bool generateManagedObjectWrapper, bool generateComObjectWrapper)
            => (ComWrappersType, GenerateManagedObjectWrapper, GenerateComObjectWrapper)
             = (comWrappersType, generateManagedObjectWrapper, generateComObjectWrapper);

        public Type ComWrappersType { get; }

        public bool GenerateManagedObjectWrapper { get; } = true;

        public bool GenerateComObjectWrapper { get; } = true;

        public bool ExportInterfaceDefinition { get; }
    }
}
