// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        private readonly struct StackRow
        {
            internal readonly PropertyNameSet PropertyNames;
            internal readonly int SizeOrLength;
            internal readonly int NumberOfRows;

            internal StackRow(int sizeOrLength, int numberOfRows, PropertyNameSet propertyNames)
            {
                Debug.Assert(sizeOrLength >= 0);
                Debug.Assert(numberOfRows >= -1);

                SizeOrLength = sizeOrLength;
                NumberOfRows = numberOfRows;
                PropertyNames = propertyNames;
            }
        }
    }
}
