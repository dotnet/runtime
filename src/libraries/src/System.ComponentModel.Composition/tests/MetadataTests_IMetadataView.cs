// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    public interface ITrans_RecompositionTest_RelationshipView
    {
        string Relationship { get; }
    }

    public interface ITrans_StronglyTypedStructure
    {
        string String1 { get; }
        string String2 { get; }
        int[] Numbers { get; }
        CreationPolicy Policy { get; }
        Type Type { get; }
    }

    public interface ITrans_AddinMetadata
    {
        string Name { get; }
        string Version { get; }
        string Id { get; }
    }

    public interface ITrans_MetadataTests_CustomMetadata
    {
        bool PropertyName { get; }
    }

    public interface ITrans_MetadataTests_MetadataView
    {
        string Value
        {
            get;
        }
    }

    public interface ITrans_MetadataTests_MetadataView2 : ITrans_MetadataTests_MetadataView
    {
        new int Value
        {
            get;
        }
    }

    public interface ITrans_MetadataTests_MetadataView3 : ITrans_MetadataTests_MetadataView
    {
        string Value2
        {
            get;
        }
    }

    public interface ITrans_MetadataTests_MetadataViewWithPropertySetter
    {
        string Value
        {
            get;
            set;
        }
    }

    public interface ITrans_MetadataTests_MetadataViewWithMethod
    {
        string Value
        {
            get;
        }
        void Method();
    }

    public interface ITrans_MetadataTests_MetadataViewWithEvent
    {
        string Value
        {
            get;
        }
        event EventHandler TestEvent;
    }

    public interface ITrans_MetadataTests_MetadataViewWithIndexer
    {
        string Value
        {
            get;
        }
        string this[object o] { get; }
    }
}
