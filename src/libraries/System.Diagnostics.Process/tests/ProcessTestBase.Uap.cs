// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessTestBase
    {
        protected const string NetfxRunnerName = "cmd";
        protected static readonly string RunnerName = $"{NetfxRunnerName}.exe";

        protected Process CreateProcessLong()
        {
            return CreateProcessForUap(RemotelyInvokable.LongWait);
        }

        protected Process CreateProcessPortable(Func<int> func)
        {
            return CreateProcessForUap(func);
        }

        protected Process CreateProcessPortable(Func<string, int> func, string arg)
        {
            return CreateProcessForUap(func, arg);
        }

        protected Process CreateProcessForUap(Func<int> func)
        {
            return CreateProcessForUap(func.Method, Array.Empty<string>());
        }

        protected Process CreateProcessForUap(Func<string, int> func, string arg)
        {
            return CreateProcessForUap(func.Method, new string[] { arg });
        }

        private MethodInfo GetMethodForUap(MethodInfo originalMethod)
        {
            string methodName = $"{originalMethod.Name}UapCmd";

            MethodInfo mi = typeof(RemotelyInvokable)
                .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (mi == null || mi.ReturnType != typeof(string) || mi.GetParameters().Length != originalMethod.GetParameters().Length)
            {
                throw new Exception($"Method {methodName} could not be found in class {nameof(RemotelyInvokable)}.");
            }

            return mi;
        }

        protected Process CreateProcessForUap(MethodInfo method, string[] args)
        {
            if (method.DeclaringType != typeof(RemotelyInvokable))
            {
                throw new Exception($"Method needs to be defined in {nameof(RemotelyInvokable)} class.");
            }

            if (method.Name == nameof(RemotelyInvokable.LongWait))
            {
                return CreateLongWaitingProcess();
            }

            MethodInfo uapMethod = GetMethodForUap(method);
            string cmdArgs = (string)uapMethod.Invoke(null, args);

            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = RunnerName,
                    Arguments = $"/C {PasteArguments.Paste(new string[] { cmdArgs }, false)}"
                }
            };

            AddProcessForDispose(p);
            return p;
        }

        private Process CreateLongWaitingProcess()
        {
            // timeout.exe does not work as expected on uap
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = RunnerName,
                    Arguments = "/K"
                }
            };

            AddProcessForDispose(p);
            return p;
        }
    }
}
