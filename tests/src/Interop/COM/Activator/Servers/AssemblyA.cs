// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class ClassFromA : IGetTypeFromC
{
    private readonly ClassFromC _fromC;
    public ClassFromA()
    {
        this._fromC = new ClassFromC();
    }
    
    public object GetTypeFromC()
    {
        return this._fromC.GetType();
    }
}