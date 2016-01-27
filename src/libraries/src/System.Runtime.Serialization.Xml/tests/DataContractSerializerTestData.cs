// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


// Invalid because there is no parameterless ctor
public class Invalid_Class_No_Parameterless_Ctor
{
    public Invalid_Class_No_Parameterless_Ctor(string id)
    {
        ID = id;
    }
    public string ID { get; set; }
}
