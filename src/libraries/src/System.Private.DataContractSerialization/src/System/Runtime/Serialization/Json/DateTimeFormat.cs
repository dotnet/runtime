// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;


namespace System.Runtime.Serialization
{
    /// <summary>
    /// This class is used to customize the way DateTime is
    /// serialized or deserialized by <see cref="Json.DataContractJsonSerializer"/> 
    /// </summary>
    public class DateTimeFormat
    {
        private string _formatString;
        private IFormatProvider _formatProvider;
        private DateTimeStyles _dateTimeStyles;

        /// <summary>
        /// Initializes a new <see cref="DateTimeFormat"/> with the specified
        /// formatString and DateTimeFormatInfo.CurrentInfo as the
        /// formatProvider.
        /// </summary>
        /// <param name="formatString">Specifies the formatString to be used.</param>
        public DateTimeFormat(string formatString) : this(formatString, DateTimeFormatInfo.CurrentInfo)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DateTimeFormat"/> with the specified
        /// formatString and formatProvider.
        /// </summary>
        /// <param name="formatString">Specifies the formatString to be used.</param>
        /// <param name="formatProvider">Specifies the formatProvider to be used.</param>
        public DateTimeFormat(string formatString, IFormatProvider formatProvider)
        {
            if (formatString == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(formatString));
            }

            if (formatProvider == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(formatProvider));
            }

            _formatString = formatString;
            _formatProvider = formatProvider;
            _dateTimeStyles = DateTimeStyles.RoundtripKind;
        }

        /// <summary>
        /// Gets the FormatString set on this instance.
        /// </summary>
        public string FormatString
        {
            get
            {
                return _formatString;
            }
        }

        /// <summary>
        /// Gets the FormatProvider set on this instance.
        /// </summary>
        public IFormatProvider FormatProvider
        {
            get
            {
                return _formatProvider;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="DateTimeStyles"/> on this instance.  
        /// </summary>        
        public DateTimeStyles DateTimeStyles
        {
            get
            {
                return _dateTimeStyles;
            }

            set
            {
                _dateTimeStyles = value;
            }
        }
    }
}
