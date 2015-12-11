#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Empty struct indicating no payload data.
    /// </summary>
    internal struct EmptyStruct
    {
    }
}
