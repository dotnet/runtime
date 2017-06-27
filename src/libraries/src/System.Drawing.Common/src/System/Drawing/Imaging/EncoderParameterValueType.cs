// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies a EncoderParameter data type.
    /// </summary>    
    public enum EncoderParameterValueType
    {
        /// <summary>
        /// The data is an 8-bit unsigned value.
        /// </summary>
        ValueTypeByte = 1,
        /// <summary>
        /// The data is an 8-bit ASCII value.
        /// </summary>
        ValueTypeAscii = 2, // 8-bit byte containing one 7-bit ASCII code. NULL terminated.
        /// <summary>
        /// The data is a 16-bit unsigned value.
        /// </summary>
        ValueTypeShort = 3,
        /// <summary>
        /// The data is a 32-bit unsigned value.
        /// </summary>
        ValueTypeLong = 4,
        /// <summary>
        /// The data is two long integers, specifying the numerator and the denominator of a rational number, respectively.
        /// </summary>
        ValueTypeRational = 5,   // Two Longs. The first Long is the numerator, the second Long expresses the denomintor.

        /// <summary>
        /// Two longs which specify a range of integer values.
        /// The first Long specifies the lower end and the second one specifies the higher end.
        /// All values are inclusive at both ends.
        /// </summary>
        ValueTypeLongRange = 6,
        /// <summary>
        /// An 8-bit undefined value that can take any value depending on field definition.
        /// </summary>
        ValueTypeUndefined = 7,
        /// <summary>
        /// Two Rationals. The first Rational specifies the lower end and the second specifies the higher end.
        /// All values are inclusive at both ends
        /// </summary>
        ValueTypeRationalRange = 8
    }
}
