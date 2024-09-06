// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Claunia.PropertyList
{
    /// <summary>
    ///     A PropertyListFormatException is thrown by the various property list format parsers when an error in the
    ///     format of the given property list is encountered.
    /// </summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class PropertyListFormatException : PropertyListException
    {
        /// <summary>Creates a new exception with the given message.</summary>
        /// <param name="message">A message containing information about the nature of the exception.</param>
        public PropertyListFormatException(string message) : base(message) {}
    }
}
