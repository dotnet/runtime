// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    public interface ITrans_MetadataViewWithDefaultedInt64
    {
        [DefaultValue(long.MaxValue)]
        long MyInt64 { get; }
    }

    public interface ITrans_MetadataViewWithTypeMismatchDefaultValue
    {
        [DefaultValue("Strings can't cast to numbers")]
        int MyInt { get; }
    }

    public interface ITrans_MetadataViewWithDefaultedInt
    {
        [DefaultValue(120)]
        int MyInt { get; }
    }

    public interface ITrans_MetadataViewWithDefaultedBool
    {
        [DefaultValue(false)]
        bool MyBool { get; }
    }

    public interface ITrans_MetadataViewWithDefaultedString
    {
        [DefaultValue("MyString")]
        string MyString { get; }
    }

    public interface ITrans_HasInt64
    {
        int Value { get; }
    }
}
