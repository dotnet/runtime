extern alias System_Diagnostics_Tools;

using System.Diagnostics.CodeAnalysis;
using SuppressMessageAttribute = System_Diagnostics_Tools::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Diagnostics.Tracing.EventCounter.#Enqueue(System.Single)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Diagnostics.Tracing.EventCounter.#GetEventCounterPayload():System.Diagnostics.Tracing.EventCounterPayload")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Diagnostics.Tracing.EventCounterGroup.#OnTimer(System.Object)")]
