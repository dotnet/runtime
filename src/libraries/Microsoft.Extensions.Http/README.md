# Microsoft.Extensions.Http

`Microsoft.Extensions.Http` provides an implementation of an HttpClient factory as a pattern for configuring and retrieving named HttpClients in a composable way. The HttpClient factory provides extensibility to plug in DelegatingHandlers that address cross-cutting concerns such as service location, load balancing, and reliability. The default HttpClient factory provides built-in diagnostics and logging and manages the lifetimes of connections in a performant way.

Commonly Used Types:
- System.Net.Http.IHttpClientFactory

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) is deployed as out-of-band (OOB) too and can be referenced into projects directly.