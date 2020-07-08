// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#pragma warning disable 169

namespace ValueTypeShapeCharacteristics
{
    struct SimpleHfaFloatStruct
    {
        static int irrelevantField;
        float field;
    }

    struct SimpleHfaFloatStructWithManyFields
    {
        float field1;
        float field2;
        float field3;
        float field4;
    }

    struct SimpleHfaDoubleStruct
    {
        double field;
        static int irrelevantField;
    }

    struct CompositeHfaFloatStruct
    {
        SimpleHfaFloatStruct field1;
        float field2;
        SimpleHfaFloatStruct field3;
    }

    struct CompositeHfaDoubleStruct
    {
        SimpleHfaDoubleStruct field1;
        SimpleHfaDoubleStruct field2;
        SimpleHfaDoubleStruct field3;
        SimpleHfaDoubleStruct field4;
    }

    struct NonHAEmptyStruct
    {
    }

    struct NonHAStruct
    {
        float field1;
        int field2;
    }

    struct NonHAMixedStruct
    {
        float field1;
        double field2;
    }

    struct NonHACompositeStruct
    {
        SimpleHfaDoubleStruct field1;
        SimpleHfaFloatStruct field2;
    }

    struct NonHAStructWithManyFields
    {
        float field1;
        float field2;
        float field3;
        float field4;
        float field5;
    }
}
