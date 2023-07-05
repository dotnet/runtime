#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

namespace Grpc.Shared.TestAssets
{
    public static class ExceptionAssert
    {
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, params string[] possibleMessages)
            where TException : Exception
        {
            try
            {
                await action();
            }
            catch (TException ex)
            {
                if (possibleMessages == null || possibleMessages.Length == 0)
                {
                    return ex;
                }
                foreach (string possibleMessage in possibleMessages)
                {
                    if (Assert.Equals(possibleMessage, ex.Message))
                    {
                        return ex;
                    }
                }

                throw new Exception("Unexpected exception message." + Environment.NewLine + "Expected one of: " + string.Join(Environment.NewLine, possibleMessages) + Environment.NewLine + "Got: " + ex.Message + Environment.NewLine + Environment.NewLine + ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception of type {typeof(TException).Name} expected; got exception of type {ex.GetType().Name}.", ex);
            }

            throw new Exception($"Exception of type {typeof(TException).Name} expected. No exception thrown.");
        }
    }
}
