// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Represents the exception that is thrown when trying to access a printer using invalid printer settings.
    /// </summary>
    [Serializable]
    public partial class InvalidPrinterException : SystemException
    {
        private PrinterSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref='InvalidPrinterException'/> class.
        /// </summary>
        public InvalidPrinterException(PrinterSettings settings)
        : base(GenerateMessage(settings))
        {
            _settings = settings;
        }

        private static string GenerateMessage(PrinterSettings settings)
        {
            if (settings.IsDefaultPrinter)
            {
                return SR.Format(SR.InvalidPrinterException_NoDefaultPrinter);
            }
            else
            {
                try
                {
                    return SR.Format(SR.InvalidPrinterException_InvalidPrinter, settings.PrinterName);
                }
                catch (SecurityException)
                {
                    return SR.Format(SR.InvalidPrinterException_InvalidPrinter, SR.Format(SR.CantTellPrinterName));
                }
            }
        }
    }
}

