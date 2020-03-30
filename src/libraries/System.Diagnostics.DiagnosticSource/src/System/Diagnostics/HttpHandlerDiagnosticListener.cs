// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

// This HttpHandlerDiagnosticListener class is applicable only for .NET 4.5, and not for .NET core.

namespace System.Diagnostics
{
    /// <summary>
    /// A HttpHandlerDiagnosticListener is a DiagnosticListener for .NET 4.5 and above where
    /// HttpClient doesn't have a DiagnosticListener built in. This class is not used for .NET Core
    /// because HttpClient in .NET Core already emits DiagnosticSource events. This class compensates for
    /// that in .NET 4.5 and above. HttpHandlerDiagnosticListener has no public constructor. To use this,
    /// the application just needs to call <see cref="DiagnosticListener.AllListeners" /> and
    /// <see cref="DiagnosticListener.AllListenerObservable.Subscribe(IObserver{DiagnosticListener})"/>,
    /// then in the <see cref="IObserver{DiagnosticListener}.OnNext(DiagnosticListener)"/> method,
    /// when it sees the System.Net.Http.Desktop source, subscribe to it. This will trigger the
    /// initialization of this DiagnosticListener.
    /// </summary>
    internal sealed class HttpHandlerDiagnosticListener : DiagnosticListener
    {
        /// <summary>
        /// Overriding base class implementation just to give us a chance to initialize.
        /// </summary>
        public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled)
        {
            IDisposable result = base.Subscribe(observer, isEnabled);
            Initialize();
            return result;
        }

        /// <summary>
        /// Overriding base class implementation just to give us a chance to initialize.
        /// </summary>
        public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Func<string, object, object, bool> isEnabled)
        {
            IDisposable result = base.Subscribe(observer, isEnabled);
            Initialize();
            return result;
        }

        /// <summary>
        /// Overriding base class implementation just to give us a chance to initialize.
        /// </summary>
        public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer)
        {
            IDisposable result = base.Subscribe(observer);
            Initialize();
            return result;
        }

        /// <summary>
        /// Initializes all the reflection objects it will ever need. Reflection is costly, but it's better to take
        /// this one time performance hit than to get it multiple times later, or do it lazily and have to worry about
        /// threading issues. If Initialize has been called before, it will not doing anything.
        /// </summary>
        private void Initialize()
        {
            lock (this)
            {
                if (!_initialized)
                {
                    try
                    {
                        // This flag makes sure we only do this once. Even if we failed to initialize in an
                        // earlier time, we should not retry because this initialization is not cheap and
                        // the likelihood it will succeed the second time is very small.
                        _initialized = true;

                        PrepareReflectionObjects();
                        PerformInjection();
                    }
                    catch (Exception ex)
                    {
                        // If anything went wrong, just no-op. Write an event so at least we can find out.
                        Write(InitializationFailed, new { Exception = ex });
                    }
                }
            }
        }

        #region private helper classes

        private class HashtableWrapper : Hashtable, IEnumerable
        {
            protected Hashtable _table;
            public override int Count
            {
                get
                {
                    return this._table.Count;
                }
            }
            public override bool IsReadOnly
            {
                get
                {
                    return this._table.IsReadOnly;
                }
            }
            public override bool IsFixedSize
            {
                get
                {
                    return this._table.IsFixedSize;
                }
            }
            public override bool IsSynchronized
            {
                get
                {
                    return this._table.IsSynchronized;
                }
            }
            public override object this[object key]
            {
                get
                {
                    return this._table[key];
                }
                set
                {
                    this._table[key] = value;
                }
            }
            public override object SyncRoot
            {
                get
                {
                    return this._table.SyncRoot;
                }
            }
            public override ICollection Keys
            {
                get
                {
                    return this._table.Keys;
                }
            }
            public override ICollection Values
            {
                get
                {
                    return this._table.Values;
                }
            }
            internal HashtableWrapper(Hashtable table) : base()
            {
                this._table = table;
            }
            public override void Add(object key, object value)
            {
                this._table.Add(key, value);
            }
            public override void Clear()
            {
                this._table.Clear();
            }
            public override bool Contains(object key)
            {
                return this._table.Contains(key);
            }
            public override bool ContainsKey(object key)
            {
                return this._table.ContainsKey(key);
            }
            public override bool ContainsValue(object key)
            {
                return this._table.ContainsValue(key);
            }
            public override void CopyTo(Array array, int arrayIndex)
            {
                this._table.CopyTo(array, arrayIndex);
            }
            public override object Clone()
            {
                return new HashtableWrapper((Hashtable)this._table.Clone());
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this._table.GetEnumerator();
            }
            public override IDictionaryEnumerator GetEnumerator()
            {
                return this._table.GetEnumerator();
            }
            public override void Remove(object key)
            {
                this._table.Remove(key);
            }
        }

        /// <summary>
        /// Helper class used for ServicePointManager.s_ServicePointTable. The goal here is to
        /// intercept each new ServicePoint object being added to ServicePointManager.s_ServicePointTable
        /// and replace its ConnectionGroupList hashtable field.
        /// </summary>
        private sealed class ServicePointHashtable : HashtableWrapper
        {
            public ServicePointHashtable(Hashtable table) : base(table)
            {
            }

            public override object this[object key]
            {
                get
                {
                    return base[key];
                }
                set
                {
                    if (value is WeakReference weakRef && weakRef.IsAlive)
                    {
                        if (weakRef.Target is ServicePoint servicePoint)
                        {
                            // Replace the ConnectionGroup hashtable inside this ServicePoint object,
                            // which allows us to intercept each new ConnectionGroup object added under
                            // this ServicePoint.
                            Hashtable originalTable = s_connectionGroupListField.GetValue(servicePoint) as Hashtable;
                            ConnectionGroupHashtable newTable = new ConnectionGroupHashtable(originalTable ?? new Hashtable());

                            s_connectionGroupListField.SetValue(servicePoint, newTable);
                        }
                    }

                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// Helper class used for ServicePoint.m_ConnectionGroupList. The goal here is to
        /// intercept each new ConnectionGroup object being added to ServicePoint.m_ConnectionGroupList
        /// and replace its m_ConnectionList arraylist field.
        /// </summary>
        private sealed class ConnectionGroupHashtable : HashtableWrapper
        {
            public ConnectionGroupHashtable(Hashtable table) : base(table)
            {
            }

            public override object this[object key]
            {
                get
                {
                    return base[key];
                }
                set
                {
                    if (s_connectionGroupType.IsInstanceOfType(value))
                    {
                        // Replace the Connection arraylist inside this ConnectionGroup object,
                        // which allows us to intercept each new Connection object added under
                        // this ConnectionGroup.
                        ArrayList originalArrayList = s_connectionListField.GetValue(value) as ArrayList;
                        ConnectionArrayList newArrayList = new ConnectionArrayList(originalArrayList ?? new ArrayList());

                        s_connectionListField.SetValue(value, newArrayList);
                    }

                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// Helper class used to wrap the array list object. This class itself doesn't actually
        /// have the array elements, but rather access another array list that's given at
        /// construction time.
        /// </summary>
        private class ArrayListWrapper : ArrayList
        {
            private readonly ArrayList _list;

            public override int Capacity
            {
                get
                {
                    return this._list.Capacity;
                }
                set
                {
                    this._list.Capacity = value;
                }
            }
            public override int Count
            {
                get
                {
                    return this._list.Count;
                }
            }
            public override bool IsReadOnly
            {
                get
                {
                    return this._list.IsReadOnly;
                }
            }
            public override bool IsFixedSize
            {
                get
                {
                    return this._list.IsFixedSize;
                }
            }
            public override bool IsSynchronized
            {
                get
                {
                    return this._list.IsSynchronized;
                }
            }
            public override object this[int index]
            {
                get
                {
                    return this._list[index];
                }
                set
                {
                    this._list[index] = value;
                }
            }
            public override object SyncRoot
            {
                get
                {
                    return this._list.SyncRoot;
                }
            }
            internal ArrayListWrapper(ArrayList list) : base()
            {
                this._list = list;
            }
            public override int Add(object value)
            {
                return this._list.Add(value);
            }
            public override void AddRange(ICollection c)
            {
                this._list.AddRange(c);
            }
            public override int BinarySearch(object value)
            {
                return this._list.BinarySearch(value);
            }
            public override int BinarySearch(object value, IComparer comparer)
            {
                return this._list.BinarySearch(value, comparer);
            }
            public override int BinarySearch(int index, int count, object value, IComparer comparer)
            {
                return this._list.BinarySearch(index, count, value, comparer);
            }
            public override void Clear()
            {
                this._list.Clear();
            }
            public override object Clone()
            {
                return new ArrayListWrapper((ArrayList)this._list.Clone());
            }
            public override bool Contains(object item)
            {
                return this._list.Contains(item);
            }
            public override void CopyTo(Array array)
            {
                this._list.CopyTo(array);
            }
            public override void CopyTo(Array array, int index)
            {
                this._list.CopyTo(array, index);
            }
            public override void CopyTo(int index, Array array, int arrayIndex, int count)
            {
                this._list.CopyTo(index, array, arrayIndex, count);
            }
            public override IEnumerator GetEnumerator()
            {
                return this._list.GetEnumerator();
            }
            public override IEnumerator GetEnumerator(int index, int count)
            {
                return this._list.GetEnumerator(index, count);
            }
            public override int IndexOf(object value)
            {
                return this._list.IndexOf(value);
            }
            public override int IndexOf(object value, int startIndex)
            {
                return this._list.IndexOf(value, startIndex);
            }
            public override int IndexOf(object value, int startIndex, int count)
            {
                return this._list.IndexOf(value, startIndex, count);
            }
            public override void Insert(int index, object value)
            {
                this._list.Insert(index, value);
            }
            public override void InsertRange(int index, ICollection c)
            {
                this._list.InsertRange(index, c);
            }
            public override int LastIndexOf(object value)
            {
                return this._list.LastIndexOf(value);
            }
            public override int LastIndexOf(object value, int startIndex)
            {
                return this._list.LastIndexOf(value, startIndex);
            }
            public override int LastIndexOf(object value, int startIndex, int count)
            {
                return this._list.LastIndexOf(value, startIndex, count);
            }
            public override void Remove(object value)
            {
                this._list.Remove(value);
            }
            public override void RemoveAt(int index)
            {
                this._list.RemoveAt(index);
            }
            public override void RemoveRange(int index, int count)
            {
                this._list.RemoveRange(index, count);
            }
            public override void Reverse(int index, int count)
            {
                this._list.Reverse(index, count);
            }
            public override void SetRange(int index, ICollection c)
            {
                this._list.SetRange(index, c);
            }
            public override ArrayList GetRange(int index, int count)
            {
                return this._list.GetRange(index, count);
            }
            public override void Sort()
            {
                this._list.Sort();
            }
            public override void Sort(IComparer comparer)
            {
                this._list.Sort(comparer);
            }
            public override void Sort(int index, int count, IComparer comparer)
            {
                this._list.Sort(index, count, comparer);
            }
            public override object[] ToArray()
            {
                return this._list.ToArray();
            }
            public override Array ToArray(Type type)
            {
                return this._list.ToArray(type);
            }
            public override void TrimToSize()
            {
                this._list.TrimToSize();
            }
        }

        /// <summary>
        /// Helper class used for ConnectionGroup.m_ConnectionList. The goal here is to
        /// intercept each new Connection object being added to ConnectionGroup.m_ConnectionList
        /// and replace its m_WriteList arraylist field.
        /// </summary>
        private sealed class ConnectionArrayList : ArrayListWrapper
        {
            public ConnectionArrayList(ArrayList list) : base(list)
            {
            }

            public override int Add(object value)
            {
                if (s_connectionType.IsInstanceOfType(value))
                {
                    // Replace the HttpWebRequest arraylist inside this Connection object,
                    // which allows us to intercept each new HttpWebRequest object added under
                    // this Connection.
                    ArrayList originalArrayList = s_writeListField.GetValue(value) as ArrayList;
                    HttpWebRequestArrayList newArrayList = new HttpWebRequestArrayList(originalArrayList ?? new ArrayList());

                    s_writeListField.SetValue(value, newArrayList);
                }

                return base.Add(value);
            }
        }

        /// <summary>
        /// Helper class used for Connection.m_WriteList. The goal here is to
        /// intercept all new HttpWebRequest objects being added to Connection.m_WriteList
        /// and notify the listener about the HttpWebRequest that's about to send a request.
        /// It also intercepts all HttpWebRequest objects that are about to get removed from
        /// Connection.m_WriteList as they have completed the request.
        /// </summary>
        private sealed class HttpWebRequestArrayList : ArrayListWrapper
        {
            public HttpWebRequestArrayList(ArrayList list) : base(list)
            {
            }

            public override int Add(object value)
            {
                // Add before firing events so if some user code cancels/aborts the request it will be found in the outstanding list.
                int index = base.Add(value);

                if (value is HttpWebRequest request)
                {
                    s_instance.RaiseRequestEvent(request);
                }

                return index;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Private constructor. This class implements a singleton pattern and only this class is allowed to create an instance.
        /// </summary>
        private HttpHandlerDiagnosticListener() : base(DiagnosticListenerName)
        {
        }

        private void RaiseRequestEvent(HttpWebRequest request)
        {
            if (IsRequestInstrumented(request))
            {
                // This request was instrumented by previous RaiseRequestEvent, such is the case with redirect responses where the same request is sent again.
                return;
            }

            if (IsEnabled(ActivityName, request))
            {
                // We don't call StartActivity here because it will fire into user code before the headers are added.
                // In the case where user code cancels or aborts the request, this can lead to a Stop or Exception
                // event NOT firing because IsRequestInstrumented will return false without the headers.

                var activity = new Activity(ActivityName);
                activity.Start();

                object asyncContext = s_readAResultAccessor(request);

                // Step 1: Hook request._ReadAResult.m_AsyncState to store the state (request + callback + state + activity) we will need later.
                s_asyncStateModifier(asyncContext, new Tuple<HttpWebRequest, object, AsyncCallback, Activity>(request, s_asyncStateAccessor(asyncContext), s_asyncCallbackAccessor(asyncContext), activity));

                // Step 2: Hook request._ReadAResult.m_AsyncCallback so we can fire our events when the request is complete.
                s_asyncCallbackModifier(asyncContext, s_asyncCallback);

                InstrumentRequest(request, activity);

                // Only send start event to users who subscribed for it, but start activity anyway
                if (IsEnabled(RequestStartName))
                {
                    Write(activity.OperationName + ".Start", new { Request = request });
                }
            }
        }

        private void RaiseResponseEvent(HttpWebRequest request, HttpWebResponse response)
        {
            if (IsEnabled(RequestStopName))
            {
                Write(RequestStopName, new { Request = request, Response = response });
            }
        }

        private void RaiseExceptionEvent(HttpWebRequest request, Exception exception)
        {
            if (IsEnabled(RequestExceptionName))
            {
                Write(RequestExceptionName, new { Request = request, Exception = exception });
            }
        }

        private bool IsRequestInstrumented(HttpWebRequest request)
        {
            ActivityIdFormat Format = Activity.ForceDefaultIdFormat
                ? Activity.DefaultIdFormat
                : (Activity.Current?.IdFormat ?? Activity.DefaultIdFormat);

            return Format == ActivityIdFormat.W3C
                ? request.Headers.Get(TraceParentHeaderName) != null
                : request.Headers.Get(RequestIdHeaderName) != null;
        }

        private static void InstrumentRequest(HttpWebRequest request, Activity activity)
        {
            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                // do not inject header if it was injected already
                // perhaps tracing systems wants to override it
                if (request.Headers.Get(TraceParentHeaderName) == null)
                {
                    request.Headers.Add(TraceParentHeaderName, activity.Id);

                    string traceState = activity.TraceStateString;
                    if (traceState != null)
                    {
                        request.Headers.Add(TraceStateHeaderName, traceState);
                    }
                }
            }
            else if (request.Headers.Get(RequestIdHeaderName) == null)
            {
                // do not inject header if it was injected already
                // perhaps tracing systems wants to override it
                request.Headers.Add(RequestIdHeaderName, activity.Id);
            }

            if (request.Headers.Get(CorrelationContextHeaderName) == null)
            {
                // we expect baggage to be empty or contain a few items
                using (IEnumerator<KeyValuePair<string, string>> e = activity.Baggage.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        StringBuilder baggage = new StringBuilder();
                        do
                        {
                            KeyValuePair<string, string> item = e.Current;
                            baggage.Append(WebUtility.UrlEncode(item.Key)).Append('=').Append(WebUtility.UrlEncode(item.Value)).Append(',');
                        }
                        while (e.MoveNext());
                        baggage.Remove(baggage.Length - 1, 1);
                        request.Headers.Add(CorrelationContextHeaderName, baggage.ToString());
                    }
                }
            }
        }

        private static void AsyncCallback(IAsyncResult asyncResult)
        {
            // Retrieve the state we stuffed into m_AsyncState.
            Tuple<HttpWebRequest, object, AsyncCallback, Activity> state = (Tuple<HttpWebRequest, object, AsyncCallback, Activity>)s_asyncStateAccessor(asyncResult);

            AsyncCallback asyncCallback = state.Item3;

            try
            {
                // Access the result of the request.
                object result = s_resultAccessor(asyncResult);

                if (result is Exception ex)
                {
                    s_instance.RaiseExceptionEvent(state.Item1, ex);
                }
                else
                {
                    HttpWebResponse response = (HttpWebResponse)result;

                    if (asyncCallback == null && s_isContextAwareResultChecker(asyncResult))
                    {
                        // For async calls (where asyncResult is ContextAwareResult)...
                        // If no callback was set assume the user is manually calling BeginGetResponse & EndGetResponse
                        // in which case they could dispose the HttpWebResponse before our listeners have a chance to work with it.
                        // Disposed HttpWebResponse throws when accessing properties, so let's make a copy of the data to ensure that doesn't happen.

                        using HttpWebResponse responseCopy = s_httpWebResponseCtor(
                            new object[]
                            {
                                s_uriAccessor(response), s_verbAccessor(response), s_coreResponseDataAccessor(response), s_mediaTypeAccessor(response),
                                s_usesProxySemanticsAccessor(response), DecompressionMethods.None,
                                s_isWebSocketResponseAccessor(response), s_connectionGroupNameAccessor(response)
                            });

                        s_instance.RaiseResponseEvent(state.Item1, responseCopy);
                    }
                    else
                    {
                        s_instance.RaiseResponseEvent(state.Item1, response);
                    }
                }
            }
            catch
            {
            }

            // Activity.Current should be fine here because the AsyncCallback fires through ExecutionContext but it was easy enough to pass in and this will work even if context wasn't flowed, for some reason.
            state.Item4.Stop();

            // Restore the state in case anyone downstream is reliant on it.
            s_asyncStateModifier(asyncResult, state.Item2);

            // Fire the user's callback, if it was set. No try/catch so calling HttpWebRequest can abort on failure.
            asyncCallback?.Invoke(asyncResult);
        }

        private static void PrepareReflectionObjects()
        {
            // At any point, if the operation failed, it should just throw. The caller should catch all exceptions and swallow.

            Type servicePointType = typeof(ServicePoint);
            Assembly systemNetHttpAssembly = servicePointType.Assembly;
            s_connectionGroupListField = servicePointType.GetField("m_ConnectionGroupList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionGroupType = systemNetHttpAssembly?.GetType("System.Net.ConnectionGroup");
            s_connectionListField = s_connectionGroupType?.GetField("m_ConnectionList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionType = systemNetHttpAssembly?.GetType("System.Net.Connection");
            s_writeListField = s_connectionType?.GetField("m_WriteList", BindingFlags.Instance | BindingFlags.NonPublic);

            s_readAResultAccessor = CreateFieldGetter<object>(typeof(HttpWebRequest), "_ReadAResult", BindingFlags.NonPublic | BindingFlags.Instance);

            // Double checking to make sure we have all the pieces initialized
            if (s_connectionGroupListField == null ||
                s_connectionGroupType == null ||
                s_connectionListField == null ||
                s_connectionType == null ||
                s_writeListField == null ||
                s_readAResultAccessor == null ||
                !PrepareAsyncResultReflectionObjects(systemNetHttpAssembly) ||
                !PrepareHttpWebResponseReflectionObjects(systemNetHttpAssembly))
            {
                // If anything went wrong here, just return false. There is nothing we can do.
                throw new InvalidOperationException("Unable to initialize all required reflection objects");
            }
        }

        private static bool PrepareAsyncResultReflectionObjects(Assembly systemNetHttpAssembly)
        {
            Type lazyAsyncResultType = systemNetHttpAssembly?.GetType("System.Net.LazyAsyncResult");
            if (lazyAsyncResultType != null)
            {
                s_asyncCallbackAccessor = CreateFieldGetter<AsyncCallback>(lazyAsyncResultType, "m_AsyncCallback", BindingFlags.NonPublic | BindingFlags.Instance);
                s_asyncCallbackModifier = CreateFieldSetter<AsyncCallback>(lazyAsyncResultType, "m_AsyncCallback", BindingFlags.NonPublic | BindingFlags.Instance);
                s_asyncStateAccessor = CreateFieldGetter<object>(lazyAsyncResultType, "m_AsyncState", BindingFlags.NonPublic | BindingFlags.Instance);
                s_asyncStateModifier = CreateFieldSetter<object>(lazyAsyncResultType, "m_AsyncState", BindingFlags.NonPublic | BindingFlags.Instance);
                s_resultAccessor = CreateFieldGetter<object>(lazyAsyncResultType, "m_Result", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            Type contextAwareResultType = systemNetHttpAssembly?.GetType("System.Net.ContextAwareResult");
            if (contextAwareResultType != null)
            {
                s_isContextAwareResultChecker = CreateTypeChecker(contextAwareResultType);
            }

            return s_asyncCallbackAccessor != null
                && s_asyncCallbackModifier != null
                && s_asyncStateAccessor != null
                && s_asyncStateModifier != null
                && s_resultAccessor != null
                && s_isContextAwareResultChecker != null;
        }

        private static bool PrepareHttpWebResponseReflectionObjects(Assembly systemNetHttpAssembly)
        {
            Type knownHttpVerbType = systemNetHttpAssembly?.GetType("System.Net.KnownHttpVerb");
            Type coreResponseData = systemNetHttpAssembly?.GetType("System.Net.CoreResponseData");

            if (knownHttpVerbType != null && coreResponseData != null)
            {
                ConstructorInfo ctor = typeof(HttpWebResponse).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[]
                    {
                        typeof(Uri), knownHttpVerbType, coreResponseData, typeof(string),
                        typeof(bool), typeof(DecompressionMethods),
                        typeof(bool), typeof(string)
                    },
                    null);

                if (ctor != null)
                {
                    s_httpWebResponseCtor = CreateTypeInstance<HttpWebResponse>(ctor);
                }
            }

            s_uriAccessor = CreateFieldGetter<HttpWebResponse, Uri>("m_Uri", BindingFlags.NonPublic | BindingFlags.Instance);
            s_verbAccessor = CreateFieldGetter<HttpWebResponse, object>("m_Verb", BindingFlags.NonPublic | BindingFlags.Instance);
            s_mediaTypeAccessor = CreateFieldGetter<HttpWebResponse, string>("m_MediaType", BindingFlags.NonPublic | BindingFlags.Instance);
            s_usesProxySemanticsAccessor = CreateFieldGetter<HttpWebResponse, bool>("m_UsesProxySemantics", BindingFlags.NonPublic | BindingFlags.Instance);
            s_coreResponseDataAccessor = CreateFieldGetter<HttpWebResponse, object>("m_CoreResponseData", BindingFlags.NonPublic | BindingFlags.Instance);
            s_isWebSocketResponseAccessor = CreateFieldGetter<HttpWebResponse, bool>("m_IsWebSocketResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            s_connectionGroupNameAccessor = CreateFieldGetter<HttpWebResponse, string>("m_ConnectionGroupName", BindingFlags.NonPublic | BindingFlags.Instance);

            return s_httpWebResponseCtor != null
                && s_uriAccessor != null
                && s_verbAccessor != null
                && s_mediaTypeAccessor != null
                && s_usesProxySemanticsAccessor != null
                && s_coreResponseDataAccessor != null
                && s_isWebSocketResponseAccessor != null
                && s_connectionGroupNameAccessor != null;
        }

        private static void PerformInjection()
        {
            FieldInfo servicePointTableField = typeof(ServicePointManager).GetField("s_ServicePointTable", BindingFlags.Static | BindingFlags.NonPublic);
            if (servicePointTableField == null)
            {
                // If anything went wrong here, just return false. There is nothing we can do.
                throw new InvalidOperationException("Unable to access the ServicePointTable field");
            }

            Hashtable originalTable = servicePointTableField.GetValue(null) as Hashtable;
            ServicePointHashtable newTable = new ServicePointHashtable(originalTable ?? new Hashtable());

            servicePointTableField.SetValue(null, newTable);
        }

        private static Func<TClass, TField> CreateFieldGetter<TClass, TField>(string fieldName, BindingFlags flags) where TClass : class
        {
            FieldInfo field = typeof(TClass).GetField(fieldName, flags);
            if (field != null)
            {
                string methodName = field.ReflectedType.FullName + ".get_" + field.Name;
                DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(TField), new[] { typeof(TClass) }, true);
                ILGenerator generator = getterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Ret);
                return (Func<TClass, TField>)getterMethod.CreateDelegate(typeof(Func<TClass, TField>));
            }

            return null;
        }

        /// <summary>
        /// Creates getter for a field defined in private or internal type
        /// repesented with classType variable
        /// </summary>
        private static Func<object, TField> CreateFieldGetter<TField>(Type classType, string fieldName, BindingFlags flags)
        {
            FieldInfo field = classType.GetField(fieldName, flags);
            if (field != null)
            {
                string methodName = classType.FullName + ".get_" + field.Name;
                DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(TField), new[] { typeof(object) }, true);
                ILGenerator generator = getterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Castclass, classType);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Ret);

                return (Func<object, TField>)getterMethod.CreateDelegate(typeof(Func<object, TField>));
            }

            return null;
        }

        /// <summary>
        /// Creates setter for a field defined in private or internal type
        /// repesented with classType variable
        /// </summary>
        private static Action<object, TField> CreateFieldSetter<TField>(Type classType, string fieldName, BindingFlags flags)
        {
            FieldInfo field = classType.GetField(fieldName, flags);
            if (field != null)
            {
                string methodName = classType.FullName + ".set_" + field.Name;
                DynamicMethod setterMethod = new DynamicMethod(methodName, null, new[] { typeof(object), typeof(TField) }, true);
                ILGenerator generator = setterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Castclass, classType);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Stfld, field);
                generator.Emit(OpCodes.Ret);

                return (Action<object, TField>)setterMethod.CreateDelegate(typeof(Action<object, TField>));
            }

            return null;
        }

        /// <summary>
        /// Creates an "is" method for the private or internal type.
        /// </summary>
        private static Func<object, bool> CreateTypeChecker(Type classType)
        {
            string methodName = classType.FullName + ".typeCheck";
            DynamicMethod setterMethod = new DynamicMethod(methodName, typeof(bool), new[] { typeof(object) }, true);
            ILGenerator generator = setterMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Isinst, classType);
            generator.Emit(OpCodes.Ldnull);
            generator.Emit(OpCodes.Cgt_Un);
            generator.Emit(OpCodes.Ret);

            return (Func<object, bool>)setterMethod.CreateDelegate(typeof(Func<object, bool>));
        }

        /// <summary>
        /// Creates an instance of T using a private or internal ctor.
        /// </summary>
        private static Func<object[], T> CreateTypeInstance<T>(ConstructorInfo constructorInfo)
        {
            Type classType = typeof(T);
            string methodName = classType.FullName + ".ctor";
            DynamicMethod setterMethod = new DynamicMethod(methodName, classType, new Type[] { typeof(object[]) }, true);
            ILGenerator generator = setterMethod.GetILGenerator();

            ParameterInfo[] ctorParams = constructorInfo.GetParameters();
            for (int i = 0; i < ctorParams.Length; i++)
            {
                generator.Emit(OpCodes.Ldarg_0);
                switch (i)
                {
                    case 0: generator.Emit(OpCodes.Ldc_I4_0); break;
                    case 1: generator.Emit(OpCodes.Ldc_I4_1); break;
                    case 2: generator.Emit(OpCodes.Ldc_I4_2); break;
                    case 3: generator.Emit(OpCodes.Ldc_I4_3); break;
                    case 4: generator.Emit(OpCodes.Ldc_I4_4); break;
                    case 5: generator.Emit(OpCodes.Ldc_I4_5); break;
                    case 6: generator.Emit(OpCodes.Ldc_I4_6); break;
                    case 7: generator.Emit(OpCodes.Ldc_I4_7); break;
                    case 8: generator.Emit(OpCodes.Ldc_I4_8); break;
                    default: generator.Emit(OpCodes.Ldc_I4, i); break;
                }
                generator.Emit(OpCodes.Ldelem_Ref);
                Type paramType = ctorParams[i].ParameterType;
                generator.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
            }
            generator.Emit(OpCodes.Newobj, constructorInfo);
            generator.Emit(OpCodes.Ret);

            return (Func<object[], T>)setterMethod.CreateDelegate(typeof(Func<object[], T>));
        }

        #endregion

        internal static readonly HttpHandlerDiagnosticListener s_instance = new HttpHandlerDiagnosticListener();

        #region private fields
        private const string DiagnosticListenerName = "System.Net.Http.Desktop";
        private const string ActivityName = DiagnosticListenerName + ".HttpRequestOut";
        private const string RequestStartName = ActivityName + ".Start";
        private const string RequestStopName = ActivityName + ".Stop";
        private const string RequestExceptionName = ActivityName + ".Exception";
        private const string InitializationFailed = DiagnosticListenerName + ".InitializationFailed";
        private const string RequestIdHeaderName = "Request-Id";
        private const string CorrelationContextHeaderName = "Correlation-Context";
        private const string TraceParentHeaderName = "traceparent";
        private const string TraceStateHeaderName = "tracestate";

        private static readonly AsyncCallback s_asyncCallback = AsyncCallback;

        // Fields for controlling initialization of the HttpHandlerDiagnosticListener singleton
        private bool _initialized = false;

        // Fields for reflection
        private static FieldInfo s_connectionGroupListField;
        private static Type s_connectionGroupType;
        private static FieldInfo s_connectionListField;
        private static Type s_connectionType;
        private static FieldInfo s_writeListField;
        private static Func<object, object> s_readAResultAccessor;

        // LazyAsyncResult & ContextAwareResult
        private static Func<object, AsyncCallback> s_asyncCallbackAccessor;
        private static Action<object, AsyncCallback> s_asyncCallbackModifier;
        private static Func<object, object> s_asyncStateAccessor;
        private static Action<object, object> s_asyncStateModifier;
        private static Func<object, object> s_resultAccessor;
        private static Func<object, bool> s_isContextAwareResultChecker;

        // HttpWebResponse
        private static Func<object[], HttpWebResponse> s_httpWebResponseCtor;
        private static Func<HttpWebResponse, Uri> s_uriAccessor;
        private static Func<HttpWebResponse, object> s_verbAccessor;
        private static Func<HttpWebResponse, string> s_mediaTypeAccessor;
        private static Func<HttpWebResponse, bool> s_usesProxySemanticsAccessor;
        private static Func<HttpWebResponse, object> s_coreResponseDataAccessor;
        private static Func<HttpWebResponse, bool> s_isWebSocketResponseAccessor;
        private static Func<HttpWebResponse, string> s_connectionGroupNameAccessor;

        #endregion
    }
}
