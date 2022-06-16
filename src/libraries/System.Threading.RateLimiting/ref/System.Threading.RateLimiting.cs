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
        public override System.TimeSpan? IdleDuration { get { throw null; } }
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
    public sealed partial class FixedWindowRateLimiter : System.Threading.RateLimiting.ReplenishingRateLimiter
    {
        public FixedWindowRateLimiter(System.Threading.RateLimiting.FixedWindowRateLimiterOptions options) { }
        public override System.TimeSpan? IdleDuration { get { throw null; } }
        public override bool IsAutoReplenishing { get { throw null; } }
        public override System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        protected override System.Threading.RateLimiting.RateLimitLease AcquireCore(int requestCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(int requestCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class FixedWindowRateLimiterOptions
    {
        public FixedWindowRateLimiterOptions(int permitLimit, System.Threading.RateLimiting.QueueProcessingOrder queueProcessingOrder, int queueLimit, System.TimeSpan window, bool autoReplenishment = true) { }
        public bool AutoReplenishment { get { throw null; } }
        public int PermitLimit { get { throw null; } }
        public int QueueLimit { get { throw null; } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } }
        public System.TimeSpan Window { get { throw null; } }
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
    public static partial class PartitionedRateLimiter
    {
        public static System.Threading.RateLimiting.PartitionedRateLimiter<TResource> Create<TResource, TPartitionKey>(System.Func<TResource, System.Threading.RateLimiting.RateLimitPartition<TPartitionKey>> partitioner, System.Collections.Generic.IEqualityComparer<TPartitionKey>? equalityComparer = null) where TPartitionKey : notnull { throw null; }
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
        public abstract System.TimeSpan? IdleDuration { get; }
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
    public static partial class RateLimitPartition
    {
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> CreateConcurrencyLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.ConcurrencyLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> CreateFixedWindowLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.FixedWindowRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> CreateNoLimiter<TKey>(TKey partitionKey) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> CreateSlidingWindowLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.SlidingWindowRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> CreateTokenBucketLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.TokenBucketRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> Create<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.RateLimiter> factory) { throw null; }
    }
    public partial struct RateLimitPartition<TKey>
    {
        private readonly TKey _PartitionKey_k__BackingField;
        private object _dummy;
        private int _dummyPrimitive;
        public RateLimitPartition(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.RateLimiter> factory) { throw null; }
        public readonly TKey PartitionKey { get { throw null; } }
    }
    public abstract partial class ReplenishingRateLimiter : System.Threading.RateLimiting.RateLimiter
    {
        protected ReplenishingRateLimiter() { }
        public abstract bool IsAutoReplenishing { get; }
        public abstract System.TimeSpan ReplenishmentPeriod { get; }
        public abstract bool TryReplenish();
    }
    public sealed partial class SlidingWindowRateLimiter : System.Threading.RateLimiting.ReplenishingRateLimiter
    {
        public SlidingWindowRateLimiter(System.Threading.RateLimiting.SlidingWindowRateLimiterOptions options) { }
        public override System.TimeSpan? IdleDuration { get { throw null; } }
        public override bool IsAutoReplenishing { get { throw null; } }
        public override System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        protected override System.Threading.RateLimiting.RateLimitLease AcquireCore(int requestCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> WaitAsyncCore(int requestCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class SlidingWindowRateLimiterOptions
    {
        public SlidingWindowRateLimiterOptions(int permitLimit, System.Threading.RateLimiting.QueueProcessingOrder queueProcessingOrder, int queueLimit, System.TimeSpan window, int segmentsPerWindow, bool autoReplenishment = true) { }
        public bool AutoReplenishment { get { throw null; } }
        public int PermitLimit { get { throw null; } }
        public int QueueLimit { get { throw null; } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } }
        public int SegmentsPerWindow { get { throw null; } }
        public System.TimeSpan Window { get { throw null; } }
    }
    public sealed partial class TokenBucketRateLimiter : System.Threading.RateLimiting.ReplenishingRateLimiter
    {
        public TokenBucketRateLimiter(System.Threading.RateLimiting.TokenBucketRateLimiterOptions options) { }
        public override System.TimeSpan? IdleDuration { get { throw null; } }
        public override bool IsAutoReplenishing { get { throw null; } }
        public override System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        protected override System.Threading.RateLimiting.RateLimitLease AcquireCore(int tokenCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
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
