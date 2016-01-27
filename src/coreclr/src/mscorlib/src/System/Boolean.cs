// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The boolean class serves as a wrapper for the primitive
** type boolean.
**
** 
===========================================================*/
namespace System {
    
    using System;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    // The Boolean class provides the
    // object representation of the boolean primitive type.
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct Boolean : IComparable, IConvertible, IComparable<Boolean>,  IEquatable<Boolean>
    {
    
      //
      // Member Variables
      //
      private bool m_value;

      // The true value. 
      // 
      internal const int True = 1; 
      
      // The false value.
      // 
      internal const int False = 0; 
      
      
      //
      // Internal Constants are real consts for performance.
      //

      // The internal string representation of true.
      // 
      internal const String TrueLiteral  = "True";
      
      // The internal string representation of false.
      // 
      internal const String FalseLiteral = "False";
      

      //
      // Public Constants
      //
                 
      // The public string representation of true.
      // 
      public static readonly String TrueString  = TrueLiteral;
      
      // The public string representation of false.
      // 
      public static readonly String FalseString = FalseLiteral;

      //
      // Overriden Instance Methods
      //
      /*=================================GetHashCode==================================
      **Args:  None
      **Returns: 1 or 0 depending on whether this instance represents true or false.
      **Exceptions: None
      **Overriden From: Value
      ==============================================================================*/
      // Provides a hash code for this instance.
      public override int GetHashCode() {
          return (m_value)?True:False;
      }
    
      /*===================================ToString===================================
      **Args: None
      **Returns:  "True" or "False" depending on the state of the boolean.
      **Exceptions: None.
      ==============================================================================*/
      // Converts the boolean value of this instance to a String.
      public override String ToString() {
        if (false == m_value) {
          return FalseLiteral;
        }
        return TrueLiteral;
      }

      public String ToString(IFormatProvider provider) {
        if (false == m_value) {
          return FalseLiteral;
        }
        return TrueLiteral;
      }
    
      // Determines whether two Boolean objects are equal.
      public override bool Equals (Object obj) {
        //If it's not a boolean, we're definitely not equal
        if (!(obj is Boolean)) {
          return false;
        }
    
        return (m_value==((Boolean)obj).m_value);
      }

      [System.Runtime.Versioning.NonVersionable]
      public bool Equals(Boolean obj)
      {
          return m_value == obj;
      }

        // Compares this object to another object, returning an integer that
        // indicates the relationship. For booleans, false sorts before true.
        // null is considered to be less than any instance.
        // If object is not of type boolean, this method throws an ArgumentException.
        // 
        // Returns a value less than zero if this  object
        // 
        public int CompareTo(Object obj) {
            if (obj==null) {
                return 1;
            }
            if (!(obj is Boolean)) {
                throw new ArgumentException (Environment.GetResourceString("Arg_MustBeBoolean"));
            }
             
            if (m_value==((Boolean)obj).m_value) {
                return 0;
            } else if (m_value==false) {
                return -1;
            }
            return 1;
        }

        public int CompareTo(Boolean value) {
            if (m_value==value) {
                return 0;
            } else if (m_value==false) {
                return -1;
            }
            return 1;    
        }
    
        //
        // Static Methods
        // 
    
        // Determines whether a String represents true or false.
        // 
        public static Boolean Parse (String value) {
            if (value==null) throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            Boolean result = false;
            if (!TryParse(value, out result)) {
                throw new FormatException(Environment.GetResourceString("Format_BadBoolean"));            
            }
            else {
                return result;
            }
        }

        // Determines whether a String represents true or false.
        // 
        public static Boolean TryParse (String value, out Boolean result) {
            result = false;
            if (value==null) {
                return false;
            }
            // For perf reasons, let's first see if they're equal, then do the
            // trim to get rid of white space, and check again.
            if (TrueLiteral.Equals(value, StringComparison.OrdinalIgnoreCase)) {
                result = true;
                return true;
            }
            if (FalseLiteral.Equals(value,StringComparison.OrdinalIgnoreCase)) {
                result = false;
                return true;
            }

            // Special case: Trim whitespace as well as null characters.
            value = TrimWhiteSpaceAndNull(value);

            if (TrueLiteral.Equals(value, StringComparison.OrdinalIgnoreCase)) {
                result = true;
                return true;
            }
            
            if (FalseLiteral.Equals(value,StringComparison.OrdinalIgnoreCase)) {
                result = false;
                return true;
            }
            
            return false;
        }

        private static String TrimWhiteSpaceAndNull(String value) {
            int start = 0;
            int end   = value.Length-1;
            char nullChar = (char) 0x0000;

            while (start < value.Length) {
                if (!Char.IsWhiteSpace(value[start]) && value[start] != nullChar) {
                    break;
                }
                start++;
            }

            while (end >= start) {
                if (!Char.IsWhiteSpace(value[end]) && value[end] != nullChar) {
                    break;
                }
                end--;            
            }

            return value.Substring(start, end - start + 1);
        }       

        //
        // IConvertible implementation
        // 
        
        public TypeCode GetTypeCode() {
            return TypeCode.Boolean;
        }


        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            return m_value;
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Boolean", "Char"));
        }

        /// <internalonly/>
        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            return Convert.ToSByte(m_value);
        }

        /// <internalonly/>
        byte IConvertible.ToByte(IFormatProvider provider) {
            return Convert.ToByte(m_value);
        }

        /// <internalonly/>
        short IConvertible.ToInt16(IFormatProvider provider) {
            return Convert.ToInt16(m_value);
        }

        /// <internalonly/>
        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            return Convert.ToUInt16(m_value);
        }

        /// <internalonly/>
        int IConvertible.ToInt32(IFormatProvider provider) {
            return Convert.ToInt32(m_value);
        }

        /// <internalonly/>
        uint IConvertible.ToUInt32(IFormatProvider provider) {
            return Convert.ToUInt32(m_value);
        }

        /// <internalonly/>
        long IConvertible.ToInt64(IFormatProvider provider) {
            return Convert.ToInt64(m_value);
        }

        /// <internalonly/>
        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            return Convert.ToUInt64(m_value);
        }

        /// <internalonly/>
        float IConvertible.ToSingle(IFormatProvider provider) {
            return Convert.ToSingle(m_value);
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider) {
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Boolean", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
