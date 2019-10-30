// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // StringEx
    //
    ////////////////////////////////////////////////////////////////
    internal class StringEx
    {
        public static string ToString(object value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        public static string Format(object value)
        {
            if (value == null)
                return "(null)";
            if (value is string)
                return "\"" + value + "\"";

            return ToString(value);
        }
    }


    ////////////////////////////////////////////////////////////////
    // InsensitiveHashtable
    //
    ////////////////////////////////////////////////////////////////
    internal class InsensitiveDictionary : Dictionary<string, string>
    {
        //Case-insensitive
        public InsensitiveDictionary(int capacity)
            : base(capacity)
        {
        }

        //Helpers
        public void Update(string key, string value)
        {
            //If it already exist, update the value
            if (ContainsKey(key))
            {
                this[key] = value;
                return;
            }

            //Otherwise add the value
            Add(key, value);
        }

        public void Dump()
        {
            //Loop over all keys
            foreach (string key in Keys)
                Console.WriteLine(key + "=" + this[key] + ";");
        }
    }
}
