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
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(int permitCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(int permitCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
    }
    public sealed partial class ConcurrencyLimiterOptions
    {
        public ConcurrencyLimiterOptions() { }
        public int PermitLimit { get { throw null; } set { } }
        public int QueueLimit { get { throw null; } set { } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } set { } }
    }
    public sealed partial class FixedWindowRateLimiter : System.Threading.RateLimiting.ReplenishingRateLimiter
    {
        public FixedWindowRateLimiter(System.Threading.RateLimiting.FixedWindowRateLimiterOptions options) { }
        public override System.TimeSpan? IdleDuration { get { throw null; } }
        public override bool IsAutoReplenishing { get { throw null; } }
        public override System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(int requestCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(int requestCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
    }
    public sealed partial class FixedWindowRateLimiterOptions
    {
        public FixedWindowRateLimiterOptions() { }
        public bool AutoReplenishment { get { throw null; } set { } }
        public int PermitLimit { get { throw null; } set { } }
        public int QueueLimit { get { throw null; } set { } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } set { } }
        public System.TimeSpan Window { get { throw null; } set { } }
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
        public static System.Threading.RateLimiting.PartitionedRateLimiter<TResource> CreateChained<TResource>(params System.Threading.RateLimiting.PartitionedRateLimiter<TResource>[] limiters) { throw null; }
        public static System.Threading.RateLimiting.PartitionedRateLimiter<TResource> Create<TResource, TPartitionKey>(System.Func<TResource, System.Threading.RateLimiting.RateLimitPartition<TPartitionKey>> partitioner, System.Collections.Generic.IEqualityComparer<TPartitionKey>? equalityComparer = null) where TPartitionKey : notnull { throw null; }
    }
    public abstract partial class PartitionedRateLimiter<TResource> : System.IAsyncDisposable, System.IDisposable
    {
        protected PartitionedRateLimiter() { }
        public System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsync(TResource resource, int permitCount = 1, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected abstract System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(TResource resource, int permitCount, System.Threading.CancellationToken cancellationToken);
        public System.Threading.RateLimiting.RateLimitLease AttemptAcquire(TResource resource, int permitCount = 1) { throw null; }
        protected abstract System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(TResource resource, int permitCount);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public abstract int GetAvailablePermits(TResource resource);
        public System.Threading.RateLimiting.PartitionedRateLimiter<TOuter> TranslateKey<TOuter>(System.Func<TOuter, TResource> keyAdapter) { throw null; }
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
        public System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsync(int permitCount = 1, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected abstract System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(int permitCount, System.Threading.CancellationToken cancellationToken);
        public System.Threading.RateLimiting.RateLimitLease AttemptAcquire(int permitCount = 1) { throw null; }
        protected abstract System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(int permitCount);
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected virtual System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public abstract int GetAvailablePermits();
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
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> GetConcurrencyLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.ConcurrencyLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> GetFixedWindowLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.FixedWindowRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> GetNoLimiter<TKey>(TKey partitionKey) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> GetSlidingWindowLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.SlidingWindowRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> GetTokenBucketLimiter<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.TokenBucketRateLimiterOptions> factory) { throw null; }
        public static System.Threading.RateLimiting.RateLimitPartition<TKey> Get<TKey>(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.RateLimiter> factory) { throw null; }
    }
    public partial struct RateLimitPartition<TKey>
    {
        private readonly TKey _PartitionKey_k__BackingField;
        private object _dummy;
        private int _dummyPrimitive;
        public RateLimitPartition(TKey partitionKey, System.Func<TKey, System.Threading.RateLimiting.RateLimiter> factory) { throw null; }
        public readonly System.Func<TKey, System.Threading.RateLimiting.RateLimiter> Factory { get { throw null; } }
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
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(int requestCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(int requestCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
    }
    public sealed partial class SlidingWindowRateLimiterOptions
    {
        public SlidingWindowRateLimiterOptions() { }
        public bool AutoReplenishment { get { throw null; } set { } }
        public int PermitLimit { get { throw null; } set { } }
        public int QueueLimit { get { throw null; } set { } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } set { } }
        public int SegmentsPerWindow { get { throw null; } set { } }
        public System.TimeSpan Window { get { throw null; } set { } }
    }
    public sealed partial class TokenBucketRateLimiter : System.Threading.RateLimiting.ReplenishingRateLimiter
    {
        public TokenBucketRateLimiter(System.Threading.RateLimiting.TokenBucketRateLimiterOptions options) { }
        public override System.TimeSpan? IdleDuration { get { throw null; } }
        public override bool IsAutoReplenishing { get { throw null; } }
        public override System.TimeSpan ReplenishmentPeriod { get { throw null; } }
        protected override System.Threading.Tasks.ValueTask<System.Threading.RateLimiting.RateLimitLease> AcquireAsyncCore(int tokenCount, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        protected override System.Threading.RateLimiting.RateLimitLease AttemptAcquireCore(int tokenCount) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Threading.Tasks.ValueTask DisposeAsyncCore() { throw null; }
        public override int GetAvailablePermits() { throw null; }
        public override bool TryReplenish() { throw null; }
    }
    public sealed partial class TokenBucketRateLimiterOptions
    {
        public TokenBucketRateLimiterOptions() { }
        public bool AutoReplenishment { get { throw null; } set { } }
        public int QueueLimit { get { throw null; } set { } }
        public System.Threading.RateLimiting.QueueProcessingOrder QueueProcessingOrder { get { throw null; } set { } }
        public System.TimeSpan ReplenishmentPeriod { get { throw null; } set { } }
        public int TokenLimit { get { throw null; } set { } }
        public int TokensPerPeriod { get { throw null; } set { } }
    }
}
