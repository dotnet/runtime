// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class LogAsErrorException : System.Exception
{
    public LogAsErrorException(string message) : base(message)
    {
    }
}
