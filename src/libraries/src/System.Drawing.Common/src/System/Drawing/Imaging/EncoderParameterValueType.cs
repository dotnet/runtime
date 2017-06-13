// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * EncoderParameter Value Type
     */
    /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType"]/*' />
    /// <devdoc>
    ///    Specifies a EncoderParameter data type.
    /// </devdoc>    
    public enum EncoderParameterValueType
    {
        /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeByte"]/*' />
        /// <devdoc>
        ///    The data is an 8-bit unsigned value.
        /// </devdoc>
        ValueTypeByte = 1,   // 8-bit unsigned int
        /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeAscii"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The data is an 8-bit ASCII value.
        ///    </para>
        /// </devdoc>
        ValueTypeAscii = 2,   // 8-bit byte containing one 7-bit ASCII
                              // code. NULL terminated.
                              /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeShort"]/*' />
                              /// <devdoc>
                              ///    <para>
                              ///       The data is a 16-bit unsigned value.
                              ///    </para>
                              /// </devdoc>
        ValueTypeShort = 3,   // 16-bit unsigned int
        /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeLong"]/*' />
        /// <devdoc>
        ///    The data is a 32-bit unsigned value.
        /// </devdoc>
        ValueTypeLong = 4,   // 32-bit unsigned int
        /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeRational"]/*' />
        /// <devdoc>
        ///    The data is two long integers, specifying
        ///    the numerator and the denominator of a rational number, respectively.
        /// </devdoc>
        ValueTypeRational = 5,   // Two Longs. The first Long is the
                                 // numerator, the second Long expresses the
                                 // denomintor.
                                 /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeLongRange"]/*' />
                                 /// <devdoc>
                                 ///    Two values that specify a range of numbers.
                                 /// </devdoc>
        ValueTypeLongRange = 6,   // Two longs which specify a range of
                                  // integer values. The first Long specifies
                                  // the lower end and the second one
                                  // specifies the higher end. All values 
                                  // are inclusive at both ends
                                  /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeUndefined"]/*' />
                                  /// <devdoc>
                                  ///    An 8-bit undefined value.
                                  /// </devdoc>
        ValueTypeUndefined = 7,   // 8-bit byte that can take any value
                                  // depending on field definition
                                  /// <include file='doc\EncoderParameterValueType.uex' path='docs/doc[@for="EncoderParameterValueType.ValueTypeRationalRange"]/*' />
                                  /// <devdoc>
                                  ///    A range of rational numbers.
                                  /// </devdoc>
        ValueTypeRationalRange = 8    // Two Rationals. The first Rational
                                      // specifies the lower end and the second
                                      // specifies the higher end. All values 
                                      // are inclusive at both ends
    }
}


