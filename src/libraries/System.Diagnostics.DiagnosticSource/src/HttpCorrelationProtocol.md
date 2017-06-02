# Overview
One of the common problems in microservices development is ability to trace request flow from client (application, browser) through all the services involved in processing.

Typical scenarios include:

1. Tracing error received by user
2. Performance analysis and optimization: whole stack of request needs to be analyzed to find where performance issues come from
3. A/B testing: metrics for requests with experimental features should be distinguished and compared to 'production' data.

These scenarios require every request to carry additional context and services to enrich their telemetry events with this context, so it would possible to correlate telemetry from all services involved in operation processing.

Tracing an operation involves an overhead on application performance and should always be considered as optional, so application may not trace anything, trace only particular operations or some percent of all operations. 
Tracing should be consistent: operation should be either fully traced, or not traced at all.

This document provides guidance on the context needed for telemetry correlation and describes its format in HTTP communication. The context is not specific to HTTP protocol, it represents set of identifiers that is needed or helpful for end-to-end tracing. Applications widely use distributed queues for asynchronous processing so operation may start (or continue) from a queue message; applications should propagate the context through the queues and restore (create) it when they start processing received task.

# HTTP Protocol proposal
| Header name           |  Format    | Description |
| ----------------------| ---------- | ---------- |
| Request-Id            | Required. String | Unique identifier for every HTTP request involved in operation processing |
| Correlation-Context   | Optional. Comma separated list of key-value pairs: Id=id, key1=value1, key2=value2 | Operation context which is propagated across all services involved in operation processing |

## Request-Id
`Request-Id` uniquely identifies every HTTP request involved in operation processing. 

Request-Id is generated on the caller side and passed to callee. 

Implementation of this protocol should expect to receive `Request-Id` in header of incoming request. 
Absence of Request-Id indicates that it is either the first instrumented service in the system or this request was not traced by upstream service and therefore does not have any context associated with it.
To start tracing the request, implementation MUST generate new `Request-Id` (see [Root Request Id Generation](#root-request-id-generation)) for the incoming request.

When Request-Id is provided by upstream service, there is no guarantee that it is unique within the entire system. 
Implementation SHOULD make it unique by adding small suffix to incoming Request-Id to represent internal activity and use it for outgoing requests, see more details in [Hierarchical Request-Id document](HierarchicalRequestId.md).

`Request-Id` is required field, i.e., every instrumented request MUST have it. If implementation does not find `Request-Id` in the incoming request headers, it should consider it as non-traced and MAY not look for `Correlation-Context`.

It is essential that 'incoming' and 'outgoing' Request-Ids are included in the telemetry events, so implementation of this protocol MUST provide read access to Request-Id for logging systems.

### Request-Id Format
`Request-Id` is a string up to 1024 bytes length. It contains only [Base64](https://en.wikipedia.org/wiki/Base64) and "-" (hyphen), "|" (vertical bar), "." (dot), and "_" (underscore) characters.

Vertical bar, dot and underscore are reserved characters that are used to mark and delimit hierarchical Request-Id, and must not be present in the nodes. Hyphen may be used in the nodes.

Implementations SHOULD support hierarchical structure for the Request-Id, described in [Hierarchical Request-Id document](HierarchicalRequestId.md).
See [Flat Request-Id](FlatRequestId.md) for non-hierarchical Request-Id requirements.

## Correlation-Context
First service MAY add state (key value pairs) that will automatically propagate to all other services including intermediary services (that support this protocol). A typical scenarios for the Correlation-Context include logging control and sampling or A/B testing (feature flags) so that the first service has a way to pass this kind of information down to all services (including intermediary). All services other than the first one SHOULD consider Correlation-Context as read-only.
It is important to keep the size of any property small because these get serialized into HTTP headers which have significant size restrictions; Correlation-Context parsing, storing and propagation involves performance overhead on all downstream services. 

Correlation-Context MUST NOT be used as generic data passing mechanism between services or within one service.

We anticipate that there will be common well-known Correlation-Context keys. If you wish to use this for you own custom (not well-known) context key, prefix it with "@".

`Correlation-Context` is optional, it may or may not be provided by upstream (instrumented) service.
If `Correlation-Context` is provided by upstream service, implementation MUST propagate it further to downstream services. It MUST NOT change or remove properties and SHOULD NOT add new properties.

Implementation MUST provide read access to `Correlation-Context` for logging systems and MUST support adding properties to Correlation-Context.

### Correlation-Context Format
`Correlation-Context` is represented as comma separated list of key value pairs, where each pair is represented in key=value format:

`Correlation-Context: key1=value1, key2=value2`

Keys and values MUST NOT contain "=" (equals) or "," (comma) characters. 

Overall Correlation-Context length MUST NOT exceed 1024 bytes, key and value length should stay well under the combined limit of 1024 bytes.

Note that uniqueness of the key within the Correlation-Context is not guaranteed. Context received from upstream service is read-only and implementation MUST NOT remove or aggregate duplicated keys. 

# HTTP Guidelines and Limitations
- [HTTP 1.1 RFC2616](https://tools.ietf.org/html/rfc2616)
- [HTTP Header encoding RFC5987](https://tools.ietf.org/html/rfc5987)
- De-facto overall HTTP headers size is limited to several kilobytes (depending on a web server)

# Industry standards
- [Google Dapper tracing system](http://static.googleusercontent.com/media/research.google.com/en//pubs/archive/36356.pdf)
- [Zipkin](http://zipkin.io/)
- [OpenTracing](http://opentracing.io/)

# See also
- [Hierarchical Request-Id](HierarchicalRequestId.md)
- [Flat Request-Id](FlatRequestId.md)
