// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptedXmlEqualityComparer: IEqualityComparer<EncryptedXml>
    {
        /// <summary>
        /// Are the two <see cref="EncryptedXml"/> objects equal?
        /// </summary>
        /// <param name="x">
        /// The first <see cref="EncryptedXml"/> object to compare.
        /// </param>
        /// <param name="y">
        /// The second <see cref="EncryptedXml"/> object to compare.
        /// </param>
        /// <returns>
        /// True if they are equal, false otherwise.
        /// </returns>
        public bool Equals(EncryptedXml x, EncryptedXml y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if ((x == null && y != null)
                     || (x != null && y == null))
            {
                return false;
            }
            else
            {
                // TODO
                return false;
            }
        }

        /// <summary>
        /// Get the hash code.
        /// </summary>
        /// <param name="obj">
        /// The object to get the hash code of.
        /// </param>
        /// <returns>
        /// The hash code.
        /// </returns>
        public int GetHashCode(EncryptedXml obj)
        {
            // Use the actual implementation since this is unused.
            return obj.GetHashCode();
        }
    }
}
