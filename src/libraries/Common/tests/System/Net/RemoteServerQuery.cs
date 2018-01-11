// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Test.Common
{
    internal class RemoteServerException : Exception
    {
        private const string Description = "Likely external issue with remote server";

        public RemoteServerException()
            : this(null, null) { }

        public RemoteServerException(string server, Exception inner)
            : base(GetMessage(server), inner) { }

        private static string GetMessage(string server)
        {
            if (server == null)
                return Description;

            return $"{Description} : {server}";
        }
    }

    internal static class RemoteServerQuery
    {
        internal static async Task<TResult> Run<TResult>(Func<Task<TResult>> testCode, Func<Exception, bool> remoteExceptionWrapper, string serverName)
        {
            try
            {
                return await testCode();
            }
            catch (Exception actualException)
            {
                if (remoteExceptionWrapper(actualException))
                {
                    throw new RemoteServerException(serverName, actualException);
                }

                throw;
            }
        }

        internal static async Task Run(Func<Task> testCode, Func<Exception, bool> remoteExceptionWrapper, string serverName)
        {
            try
            {
                await testCode();
            }
            catch (Exception actualException)
            {
                if (remoteExceptionWrapper(actualException))
                {
                    throw new RemoteServerException(serverName, actualException);
                }

                throw;
            }
        }
    }
}
