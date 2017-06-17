// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a stubb class

namespace System.ServiceModel.Diagnostics
{
    internal class ExceptionUtility
    {
        internal Exception ThrowHelperError(ArgumentOutOfRangeException argumentOutOfRangeException)
        {
            return new NotImplementedException();
        }

        internal Exception ThrowHelperError(Exception exception)
        {
            return new NotImplementedException();
        }

        internal Exception ThrowHelperArgumentNull(string v)
        {
            return new NotImplementedException();
        }

        internal Exception ThrowHelperArgument(string v1, string v2)
        {
            return new NotImplementedException();
        }

        internal Exception ThrowHelperWarning(InvalidOperationException invalidOperationException)
        {
            return new NotImplementedException();
        }

        internal Exception ThrowHelperArgument(string v)
        {
            return new NotImplementedException();
        }
    }
}