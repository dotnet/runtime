// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            throw new AggregateException();
        }
        catch (Exception e)
        {
            // When the Trimmer receives the feature switch to use resource keys then exception
            // messages shouldn't return the exception message resource, but instead the resource
            // key. This test is passing in the feature switch so we make sure that the resources
            // got trimmed correctly.
            if (e.Message == "AggregateException_ctor_DefaultMessage")
            {
                return 100;
            }
            else
            {
                return -1;
            }
        }
    }
}
