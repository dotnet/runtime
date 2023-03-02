// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Immutable.Tests
{
    public class GenericParameterHelper
    {
        public GenericParameterHelper()
        {
            this.Data = new Random().Next();
        }

        public GenericParameterHelper(int data)
        {
            this.Data = data;
        }

        public int Data { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is GenericParameterHelper other)
            {
                return this.Data == other.Data;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Data;
        }
    }
}
