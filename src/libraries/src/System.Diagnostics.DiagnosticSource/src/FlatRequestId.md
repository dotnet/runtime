# Flat Request-Ids
This document provide guidance for implementations of [HTTP Correlation Protocol](HttpCorrelationProtocol.md) without [Hierarchical Request-Id](HierarchicalRequestId.md) support or interoperability with services that do not support it. 

We strongly recommend every implementation to support [Hierarchical Request-Id](HierarchicalRequestId.md) wherever possible. If implementation do not support it, it still MUST ensure essential requirements are met:
* `Request-Id` uniquely identifies every HTTP request involved in operation processing and MUST be generated for every incoming and outgoing request
* `Correlation-Context` has `Id` property serving as single unique identifier of the whole operation and implementation MUST generate one if it is missing.

It is important to log `Request-Id` received from the upstream service along with the incoming request. It ensures that parent-child relationships between requests are retained and the whole tree of the requests could be restored. 
Therefore implementations MUST provide access to the 'parent' Request-Id for logging system.

[Root Request Id](HierarchicalRequestId.md#root-request-id-generation) requirements and generation considerations must be used for flat Request-Id

## Correlation Id (Trace Id)
Many applications and tracing systems use single correlation/trace id to identify whole operation through all services and client applications.
In case of heterogeneous environment (where some services generate hierarchical Request-Ids and others generate flat Ids) having single identifier, common for all requests, helps to make telemetry query simple and efficient.

If implementation generates flat Request-Id, it MUST ensure `Id` is present in `Correlation-Context` or [generate](#correlation-id-generation) new one and add to the `Correlation-Context`.

### Correlation Id generation
If implementation needs to add `Id` property to `Correlation-Context`:
* SHOULD use root node of the Request-Id received from upstream service if it has hierarchical structure.
* MUST follow [Root Request Id Generation](HierarchicalRequestId.md#root-request-id-generation) rules otherwise

## Non-hierarchical Request-Id example
1. A: service-a receives request 
  * scans through its headers does not find Request-Id.
  * generates a new one: `abc`
  * adds extra property to CorrelationContext `Id=123`
  * logs event that operation was started along with `Request-Id: abc`, `Correlation-Context: Id=123`
2. A: service-a makes request to service-b:
  * generates new `Request-Id: def`
  * logs that outgoing request is about to be sent with all the available context: `Request-Id: def`, `Correlation-Context: Id=123`
  * sends request to service-b
3. B: service-b receives request
  * scans through its headers and finds `Request-Id: ghi`, `Correlation-Context: Id=123`
  * logs event that operation was started along with all available context: `Request-Id: ghi`, `Correlation-Context: Id=123`
  * processes request and responds to service-a
4. A: service-a receives response from service-b
  * logs response with context: `Request-Id: def`, `Correlation-Context: Id=123`
  * Processes request and responds to caller

As a result log records may look like:

| Message  |  Component Name | Context |
| ---------| --------------- | ------- |
| user starts request to service-a | user |  |
| incoming request | service-a | `Request-Id=abc; Parent-Request-Id=; Id=123` |
| request to service-b | service-a | `Request-Id=def; Parent-Request-Id=abc, Id=123` |
| incoming request | service-b | `Request-Id=ghi; Parent-Request-Id=def; Id=123` |
| response | service-b |`Request-Id=ghi; Parent-Request-Id=def; Id=123` |
| response from service-b | service-a | `Request-Id=def; Parent-Request-Id=abc; Id=123` |
| response | service-a |`Request-Id=abc; Parent-Request-Id=; Id=123` |
| response from service-a | user |  |

#### Remarks
* Logs for operation may be queried by `Id=123` match, logs for particular request may be queried by exact Request-Id match
* Note that since hierarchical request Id was not used, Id must be logged with every trace. Parent-Request-Id must be logged to restore parent-child relationships between incoming/outgoing requests.

## Mixed hierarchical and non-hierarchical scenario
In heterogeneous environment, some services may support hierarchical Request-Id generation and others may not.

Requirements listed [Request-Id](HttpCorrelationProtocol.md#request-id) help to ensure all telemetry for the operation still is accessible:
- if implementation supports hierarchical Request-Id, it MUST propagate `Correlation-Context` and **MAY** add `Id` if missing
- if implementation does NOT support hierarchical Request-Id, it MUST propagate `Correlation-Context` and **MUST** add `Id` if missing

Let's imagine service-a supports hierarchical Request-Id and service-b does not:

1. A: service-a receives request 
  * scans through its headers and does not find `Request-Id`.
  * generates a new one: `|Guid.`
  * logs event that operation was started along with `Request-Id: |Guid.`
2. A: service-a makes request to service-b:
  * generates new `Request-Id: |Guid.1_`
  * logs that outgoing request is about to be sent
  * sends request to service-b
3. B: service-b receives request
  * scans through its headers and finds `Request-Id: |Guid.1_`
  * generates a new Request-Id: `def`   
  * does not see `Correlation-Context`. It parses parent Request-Id, extracts root node: `Guid` and adds `Id` property to `CorrelationContext : Id=abc`
  * logs event that operation was started
  * processes request and responds to service-a
4. A: service-a receives response from service-b
  * logs response with context: `Request-Id: |Guid.1_`
  * Processes request and responds to caller

As a result log records may look like:

| Message  |  Component Name | Context |
| ---------| --------------- | ------- |
| incoming request | service-a | `Request-Id=|Guid.` |
| request to service-b | service-a | `Request-Id=|Guid.1_` |
| incoming request | service-b | `Request-Id=def; Parent-Request-Id=|Guid.1_; Id=Guid` |
| response | service-b |`Request-Id=def; Parent-Request-Id=|Guid.1_; Id=Guid` |
| response from service-b | service-a | `Request-Id=|Guid.1_; Parent-Request-Id=|abc.bcec871c; Id=Guid` |
| response | service-a |`Request-Id=|Guid.` |

#### Remarks
* Note, that even if service-b does not **generate** hierarchical Request-Id, it still could benefit from hierarchical structure, by assigning `Correlation-Context: Id` to the root node of Request-Id
* Retrieving all log records then could be done by query like `Id == Guid || RequestId.startsWith('|Guid')`
* If the first service to process request does not support hierarchical ids, then it sets `Correlation-Context: Id` immediately and it's propagated further and still may be used to query all logs.
