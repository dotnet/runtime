// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // StringEx
    //
    ////////////////////////////////////////////////////////////////
    internal class StringEx
    {
        static public string ToString(object value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        static public string Format(object value)
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

            //Otheriwse add the value
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
