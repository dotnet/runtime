// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    public interface IComObjectWrapper<T> { }

    public class ComWrappers { }

    public class ComObject : IDynamicInterfaceCastable, IComObjectWrapper<ComObject>
    {
        public bool IsInterfaceImplemented(RuntimeTypeHandle th, bool b) => true;
        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle th) => th;
        // Implement support for casting through IUnknown.
        // No thread-affinity aware support.
        // No IDispatch support.
        // No aggregation support.
    }

    public abstract class GeneratedComWrappersBase<TComObject> : ComWrappers
    {
    }

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
