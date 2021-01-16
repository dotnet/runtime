// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private const string CustomHandlerTypeEnvVar = "XA_HTTP_CLIENT_HANDLER_TYPE";

        private static Type s_customHandlerType;

        private static HttpMessageHandler CreateDefaultHandler()
        {
            if (s_customHandlerType != null)
            {
                return (HttpMessageHandler)Activator.CreateInstance(s_customHandlerType);
            }

            string envVar = Environment.GetEnvironmentVariable(CustomHandlerTypeEnvVar)?.Trim();
            if (string.IsNullOrEmpty(envVar))
            {
                return new HttpClientHandler();
            }

            Type handlerType = Type.GetType(envVar, false);
            if (handlerType == null && !envVar.Contains(", "))
            {
                // Look for custom handlers in Mono.Android by default if assembly name is not specified.
                handlerType = Type.GetType(envVar + ", Mono.Android", false);
            }
            if (handlerType != null && Activator.CreateInstance(handlerType) is HttpMessageHandler handler)
            {
                // Create instance or fallback to default one if the type is invalid (current XA behavior).
                s_customHandlerType = handlerType;
                return handler;
            }
            return new HttpClientHandler();
        }
    }
}
