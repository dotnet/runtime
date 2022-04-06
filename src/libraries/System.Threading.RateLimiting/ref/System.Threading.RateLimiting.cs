// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading.RateLimiting
{
    public sealed partial class ConcurrencyLimiter : System.Threading.RateLimiting.RateLimiter
    {
        public ConcurrencyLimiter(System.Threading.RateLimiting.ConcurrencyLimiterOptions options) { }
        protected override System.Threading.RateLimiting.RateLimitLease AcquireCore(int permitCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(int permitCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class ConcurrencyLimiterOptions
    {
        public ConcurrencyLimiterOptions(int permitLimit, System.Threading.RateLimiting.QueueProcessingOrder queueProcessingOrder, int queueLimit) { }
        public int PermitLimit { get { throw null; } }
        public int QueueLimit { get { throw null; } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } }
    }
    public static partial class MetadataName
    {
        public static System.Threading.RateLimiting.MetadataName<string> ReasonPhrase { get { throw null; } }
        public static System.Threading.RateLimiting.MetadataName<System.TimeSpan> RetryAfter { get { throw null; } }
        public static System.Threading.RateLimiting.MetadataName<T> Create<T>(string name) { throw null; }
    }
    public sealed partial class MetadataName<T> : System.IEquatable<System.Threading.RateLimiting.MetadataName<T>>
    {
        public MetadataName(string name) { }
        public string Name { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Threading.RateLimiting.MetadataName<T>? other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Threading.RateLimiting.MetadataName<T> left, System.Threading.RateLimiting.MetadataName<T> right) { throw null; }
        public static bool operator !=(System.Threading.RateLimiting.MetadataName<T> left, System.Threading.RateLimiting.MetadataName<T> right) { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class PartitionedRateLimiter<TResource> : System.IAsyncDisposable, System.IDisposable
    {
        protected PartitionedRateLimiter() { }
        public System.Threading.RateLimiting.RateLimitLease Acquire(TResource resourceID, int permitCount = 1) { throw null; }
        protected abstract System.Threading.RateLimiting.RateLimitLease AcquireCore(TResource resourceID, int permitCount);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public abstract int GetAvailablePermits(TResource resourceID);
        public System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsync(TResource resourceID, int permitCount = 1, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected abstract System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(TResource resourceID, int permitCount, System.Threading.CancellationToken cancellationToken);
    }
    public enum QueueProcessingOrder
    {
        OldestFirst = 0,
        NewestFirst = 1,
    }
    public abstract partial class RateLimiter : System.IAsyncDisposable, System.IDisposable
    {
        protected RateLimiter() { }
        public System.Threading.RateLimiting.RateLimitLease Acquire(int permitCount = 1) { throw null; }
        protected abstract System.Threading.RateLimiting.RateLimitLease AcquireCore(int permitCount);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public abstract int GetAvailablePermits();
        public System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsync(int permitCount = 1, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected abstract System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(int permitCount, System.Threading.CancellationToken cancellationToken);
    }
    public abstract partial class RateLimitLease : System.IDisposable
    {
        protected RateLimitLease() { }
        public abstract bool IsAcquired { get; }
        public abstract System.Collections.Generic.IEnumerable<string> MetadataNames { get; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object?>> GetAllMetadata() { throw null; }
        public abstract bool TryGetMetadata(string metadataName, out object? metadata);
        public bool TryGetMetadata<T>(System.Threading.RateLimiting.MetadataName<T> metadataName, [System.Diagnostics.CodeAnalysis.MaybeNullAttribute] out T metadata) { throw null; }
    }
    public sealed partial class TokenBucketRateLimiter : System.Threading.RateLimiting.RateLimiter
    {
        public TokenBucketRateLimiter(System.Threading.RateLimiting.TokenBucketRateLimiterOptions options) { }
        protected override System.Threading.RateLimiting.RateLimitLease AcquireCore(int tokenCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public bool TryReplenish() { throw null; }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(int tokenCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class TokenBucketRateLimiterOptions
    {
        public TokenBucketRateLimiterOptions(int tokenLimit, System.Threading.RateLimiting.QueueProcessingOrder queueProcessingOrder, int queueLimit, System.TimeSpan replenishmentPeriod, int tokensPerPeriod, bool autoReplenishment = true) { }
        public bool AutoReplenishment { get { throw null; } }
        public int QueueLimit { get { throw null; } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } }
        public System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        public int TokenLimit { get { throw null; } }
        public int TokensPerPeriod { get { throw null; } }
    }
}
