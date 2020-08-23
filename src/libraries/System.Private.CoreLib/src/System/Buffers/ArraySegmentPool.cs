namespace System.Buffers
{
    /// <summary>
    /// Implements a dangerous, thread safe, auto resizable Pool that uses an array of objects to store <see cref="ArraySegment{T}"/>.
    /// The best fit is when one thread <see cref="DangerousRent"/>, same or others <see cref="Return"/> back short-lived <see cref="ArraySegment{T}"/>.
    /// (!) WARNING: If the <see cref="ArraySegment{T}"/> is not returned, a memory leak will occur.
    /// (!) WARNING: Do not use <see cref="ArraySegmentPool{T}"/> for long-lived objects, otherwise the memory consumption will be enormous.
    /// (!) WARNING: Slice <see cref="ArraySegment{T}"/> to zero not permitted.
    /// (!) WARNING: User of <see cref="ArraySegmentPool{T}"/> must strictly understand how the structure differs from the class.
    /// (!) WARNING: If the user has made many copies of the <see cref="ArraySegment{T}"/>, then only one copy needs to be returned to the <see cref="ArraySegmentPool{T}"/>. After returning, you should not use the leftover copies, as this will corrupt the data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ArraySegmentPool<T>
    {
        private volatile _pool _currentPool;
        private readonly bool _autoResize;
        private readonly int _maxArraySegmentLength;
        private readonly int _maxCapacity;
        private readonly object _poolLock = new object();
        /// <summary>
        /// Gets the number of rented <see cref="ArraySegment{T}"/> after last resize.
        /// </summary>
        public int Count
        {
            get
            {
                return _currentPool.Count;
            }
        }
        /// <summary>
        /// Gets the total number of <see cref="ArraySegment{T}"/> after last resize.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _currentPool.ArrayLayout.Length;
            }
        }
        /// <summary>
        /// Gets the maximum capacity of <see cref="ArraySegmentPool{T}"/>.
        /// </summary>
        public int MaxCapacity
        {
            get
            {
                return _maxCapacity;
            }
        }
        /// <summary>
        /// Gets a value that indicates whether the <see cref="ArraySegmentPool{T}"/> is resizeble.
        /// </summary>
        /// <returns></returns>
        public bool IsResizeble
        {
            get
            {
                return _autoResize;
            }
        }
        //TODO: maybe visible to ?
#if UT
        /// <summary>
        /// (Debug) Shows how many unsuccessful attempts were made before the <see cref="ArraySegment{T}"/> was taken.
        /// </summary>
        private long _failsCount;
        public long FailsCount
        {
            get
            {
                return _failsCount;
            }
        }
        /// <summary>
        /// (Debug)
        /// </summary>        
        public int LastRentedSegment
        {
            get
            {
                return _currentPool.LastRentedSegment;
            }
        }
        /// <summary>
        /// (Debug)
        /// </summary>        
        public int[] UnderlyingLayoutArray
        {
            get
            {
                return _currentPool.ArrayLayout;
            }
        }        
#endif
        /// <summary>
        /// Main storage of the <see cref="ArraySegmentPool{T}"/>.
        /// </summary>
        private class _pool
        {
            volatile public T[] Array; // Stores segments.
            volatile public int[] ArrayLayout; // Stores information about rented segments.(Interlocked not support byte)
            volatile public int Count; // Stores number of used segments.
            volatile public int LastRentedSegment; // DangerousRent using that value to find next free segment.
        }
        /// <summary>
        /// Constructs a new <see cref="ArraySegmentPool{T}"/> with fixed capacity size.
        /// </summary>
        /// <param name="maxArraySegmentLength">Maximum length of the <see cref="ArraySegment{T}"/></param>
        /// <param name="fixedCapacity">Maximum count of <see cref="ArraySegment{T}"/></param>            
        public ArraySegmentPool(int maxArraySegmentLength, int fixedCapacity) : this(maxArraySegmentLength, fixedCapacity, fixedCapacity, false) { }
        /// <summary>
        /// Constructs a new auto resizeble <see cref="ArraySegmentPool{T}"/>.
        /// </summary>
        /// <param name="maxArraySegmentLength">Maximum length of the <see cref="ArraySegment{T}"/></param>
        /// <param name="initialCapacity">Initial <see cref="ArraySegment{T}"/> count</param>
        /// <param name="maxCapacity">Maximum count of <see cref="ArraySegment{T}"/></param>
        public ArraySegmentPool(int maxArraySegmentLength, int initialCapacity, int maxCapacity) : this(maxArraySegmentLength, initialCapacity, maxCapacity, true) { }
        private ArraySegmentPool(int maxArraySegmentLength, int initialCapacity, int maxCapacity, bool autoResize)
        {
            if (maxArraySegmentLength < 1 | initialCapacity < 1 | maxCapacity < 1)
                throw new ArgumentOutOfRangeException("Arguments must be greater than 1");
            if (initialCapacity > maxCapacity)
                throw new ArgumentOutOfRangeException("InitialCapacity > MaxCapacity");
            if ((long)maxArraySegmentLength * maxCapacity > int.MaxValue)
                throw new OverflowException("MaxCapacity");
            _maxArraySegmentLength = maxArraySegmentLength;
            _maxCapacity = maxCapacity;
            _autoResize = autoResize;
            _currentPool = new _pool() { ArrayLayout = new int[initialCapacity], Array = new T[_maxArraySegmentLength * initialCapacity] };
        }
        /// <summary>
        /// (!) Dangerous. Gets an <see cref="ArraySegment{T}"/> of the default length. <see cref="ArraySegment{T}"/> must be returned via <see cref="Return"/> on the same <see cref="ArraySegmentPool{T}"/> instance to avoid memory leaks.
        /// </summary>
        /// <returns><see cref="ArraySegment{T}"/></returns>
        public ArraySegment<T> DangerousRent()
        {
            return DangerousRent(_maxArraySegmentLength);
        }
        /// <summary>
        /// (!) Dangerous. Gets an <see cref="ArraySegment{T}"/> of the custom length. <see cref="ArraySegment{T}"/> must be returned via <see cref="Return"/> on the same <see cref="ArraySegmentPool{T}"/> instance to avoid memory leaks.
        /// </summary>    
        /// <param name="length">Lenght of the rented <see cref="ArraySegment{T}"/>. Lenght must be equal or smaller than <see cref="defaultLength"/></param>
        /// <returns><see cref="ArraySegment{T}"/></returns>
        public ArraySegment<T> DangerousRent(int length)
        {
            if (length < 0 | length > _maxArraySegmentLength)
                throw new ArgumentOutOfRangeException("Length must be positive and smaller or equal than default length");
            _pool pool = _currentPool;
            //Get new resized pool if free segment not finded.
            if (pool.Count >= pool.ArrayLayout.Length)
                pool = GetNewPool(pool);
            //Try find free segment and ocupy.
            int position = pool.LastRentedSegment + 1;
            int searchCount = 0;
            do
            {
                if (position > pool.ArrayLayout.GetUpperBound(0))
                    position = 0;
                if (Threading.Interlocked.CompareExchange(ref pool.ArrayLayout[position], 1, 0) == 0)
                {
                    Threading.Interlocked.Increment(ref pool.Count);
                    pool.LastRentedSegment = position;
                    return new ArraySegment<T>(pool.Array, position * _maxArraySegmentLength, _maxArraySegmentLength).Slice(0, length);
                }
                //TODO: maybe visible to ?
#if UT
                Threading.Interlocked.Increment(ref _failsCount);
#endif
                position += 1;
                searchCount += 1;
                //That check prevent state, where thread will loop forever.
                if (searchCount == pool.ArrayLayout.Length)
                {
                    pool = GetNewPool(pool);
                    position = 0;
                    searchCount = 0;
                }
            }
            while (true);
        }
        /// <summary>
        /// Returns to the <see cref="ArraySegmentPool{T}"/> an <see cref="ArraySegment{T}"/> that was previously obtained via <see cref="DangerousRent"/> on the same <see cref="ArraySegmentPool{T}"/> instance.
        /// </summary>
        /// <param name="arraySegment"></param>
        public void Return(ref ArraySegment<T> arraySegment)
        {
            _pool pool = _currentPool;
            if (arraySegment.Array == pool.Array)
            {
                //return segment.
                int position = arraySegment.Offset / _maxArraySegmentLength;
                if (arraySegment.IsSlicedToEnd == true)
                    position--;
                if (Threading.Interlocked.Exchange(ref pool.ArrayLayout[position], 0) == 0)
                    throw new Exception("ArraySegment was returned already");
                Threading.Interlocked.Decrement(ref pool.Count);
            }
            arraySegment = ArraySegment<T>.Empty;
        }
        /// <summary>
        /// Sets the capacity of <see cref="ArraySegmentPool{T}"/> to the size of the used <see cref="ArraySegment{T}"/>. This method can be used to minimize a pool's memory overhead once it is known that no new <see cref = "ArraySegment{T}" /> will be added to the <see cref= "ArraySegmentPool{T}"/>.
        /// </summary>
        public void TrimExcess()
        {
            lock (_poolLock)
            {
                if (_autoResize == false)
                    throw new AccessViolationException("Can't trim while auto resize false");
                int count = _currentPool.Count;
                int new_layout_length = count > 1 ? count : 1;
                int new_length = new_layout_length * _maxArraySegmentLength;
                _currentPool = new _pool() { ArrayLayout = new int[new_layout_length], Array = new T[new_length] };
            }
        }
        /// <summary>
        /// Resize the <see cref="ArraySegmentPool{T}"/> and update the instance reference.
        /// </summary>
        /// <param name="pool"></param>
        /// <returns>New pool</returns>
        private _pool GetNewPool(_pool pool)
        {
            lock (_poolLock)
            {
                if (_autoResize == false)
                    throw new OverflowException("ArraySegmentPool size out of max capacity");
                //check if other thread already create new resized pool.
                if (pool != _currentPool)
                    return _currentPool;
                //check limits.
                if (pool.ArrayLayout.Length == _maxCapacity)
                    throw new OverflowException("ArraySegmentPool size out of max capacity");
                //create new resized pool and refresh current ref.
                int newLayoutLength = pool.ArrayLayout.Length * 2L < _maxCapacity ? pool.ArrayLayout.Length * 2 : _maxCapacity;
                int newLength = _maxArraySegmentLength * newLayoutLength;
                _currentPool = new _pool() { ArrayLayout = new int[newLayoutLength], Array = new T[newLength] };
                //return new pool.
                return _currentPool;
            }
        }
    }
}
