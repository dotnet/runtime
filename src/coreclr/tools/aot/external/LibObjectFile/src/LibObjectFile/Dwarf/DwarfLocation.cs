// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public struct DwarfLocation
    {
        public DwarfLocation(int value)
        {
            AsValue = new DwarfInteger() { I64 = value };
            AsObject = null;
        }

        public DwarfLocation(DwarfExpression expression)
        {
            AsValue = default;
            AsObject = expression;
        }

        public DwarfLocation(DwarfLocationList locationList)
        {
            AsValue = default;
            AsObject = locationList;
        }

        public DwarfInteger AsValue;

        public object AsObject;

        public DwarfExpression AsExpression => AsObject as DwarfExpression;

        public DwarfLocationList AsLocationList => AsObject as DwarfLocationList;

        public DwarfDIE AsReference => AsObject as DwarfDIE;

        public override string ToString()
        {
            if (AsExpression != null) return $"Location Expression: {AsExpression}";
            if (AsLocationList != null) return $"Location List: {AsLocationList}";
            if (AsReference != null) return $"Location Reference: {AsReference}";
            return $"Location Constant: {AsValue}";
        }

        public static implicit operator DwarfLocation(DwarfExpression value)
        {
            return new DwarfLocation(value);
        }

        public static implicit operator DwarfLocation(DwarfLocationList value)
        {
            return new DwarfLocation(value);
        }
    }
}