// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

class Program
{
    static int Main(string[] args)
    {
        IServiceCollection descriptors = new ServiceCollection();
        descriptors.AddHttpClient("test1")
            .AddTypedClient<TypedClientA>()
            .AddTypedClient<ITypedClientB, TypedClientB>();

        descriptors.AddHttpClient<TypedClientC>("test2");
        descriptors.AddHttpClient<ITypedClientD, TypedClientD>("test3");

        ServiceProvider provider = descriptors.BuildServiceProvider();

        TypedClientA clientA = provider.GetRequiredService<TypedClientA>();
        ITypedClientB clientB = provider.GetRequiredService<ITypedClientB>();
        TypedClientC clientC = provider.GetRequiredService<TypedClientC>();
        ITypedClientD clientD = provider.GetRequiredService<ITypedClientD>();

        if (clientA == null ||
            !(clientB is TypedClientB) ||
            clientC == null ||
            !(clientD is TypedClientD))
        {
            return -1;
        }

        return 100;
    }

    class TypedClientA
    {
        public TypedClientA(HttpClient httpClient) { }
    }

    interface ITypedClientB { }
    class TypedClientB : ITypedClientB
    {
        public TypedClientB(HttpClient httpClient) { }
    }

    class TypedClientC
    {
        public TypedClientC(HttpClient httpClient) { }
    }

    interface ITypedClientD { }
    class TypedClientD : ITypedClientD
    {
        public TypedClientD(HttpClient httpClient) { }
    }
}
