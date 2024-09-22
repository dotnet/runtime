// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.System.Net.Http
{
    public static class HttpClientExtensions
    {

        public static Task AddHeader(this HttpClient httpclinet, Dictionary<string, string> dictionary)
        {
            if (dictionary == null)
                return;
            foreach (var item in dictionary)
            {
                if (!client.DefaultRequestHeaders.Contains(header.Key))
                {
                    httpclinet.DefaultRequestHeaders.Add(item.Key, item.Value);
                }
            }
            return Task.CompletedTask;


        }
    }
}
