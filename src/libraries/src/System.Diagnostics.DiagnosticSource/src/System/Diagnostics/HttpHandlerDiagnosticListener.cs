// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

// This HttpHandlerDiagnosticListener class is applicable only for .NET 4.6, and not for .NET core.

namespace System.Diagnostics
{
    /// <summary>
    /// A HttpHandlerDiagnosticListener is a DiagnosticListener for .NET 4.6 and above where
    /// HttpClient doesn't have a DiagnosticListener built in. This class is not used for .NET Core
    /// because HttpClient in .NET Core already emits DiagnosticSource events. This class compensates for
    /// that in .NET 4.6 and above. HttpHandlerDiagnosticListener has no public constructor. To use this, 
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
                if (!this.initialized)
                {
                    try
                    {
                        // This flag makes sure we only do this once. Even if we failed to initialize in an
                        // earlier time, we should not retry because this initialization is not cheap and
                        // the likelihood it will succeed the second time is very small.
                        this.initialized = true;

                        PrepareReflectionObjects();
                        PerformInjection();
                    }
                    catch (Exception ex)
                    {
                        // If anything went wrong, just no-op. Write an event so at least we can find out.
                        this.Write(InitializationFailed, new { Exception = ex });
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
                    WeakReference weakRef = value as WeakReference;
                    if (weakRef != null && weakRef.IsAlive)
                    {
                        ServicePoint servicePoint = weakRef.Target as ServicePoint;
                        if (servicePoint != null)
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
            private ArrayList _list;

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
                HttpWebRequest request = value as HttpWebRequest;
                if (request != null)
                {
                    s_instance.RaiseRequestEvent(request);
                }

                return base.Add(value);
            }

            public override void RemoveAt(int index)
            {
                HttpWebRequest request = base[index] as HttpWebRequest;
                if (request != null)
                {
                    HttpWebResponse response = s_httpResponseAccessor(request);
                    if (response != null)
                    {
                        s_instance.RaiseResponseEvent(request, response);
                    }
                }

                base.RemoveAt(index);
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
            if (request.Headers.Get(RequestIdHeaderName) != null)
            {
                // this request was instrumented by previous RaiseRequestEvent
                return;
            }

            if (this.IsEnabled(ActivityName, request))
            {
                var activity = new Activity(ActivityName);

                // Only send start event to users who subscribed for it, but start activity anyway
                if (this.IsEnabled(RequestStartName))
                {
                    this.StartActivity(activity, new { Request = request });
                }
                else
                {
                    activity.Start();
                }

                request.Headers.Add(RequestIdHeaderName, activity.Id);
                // we expect baggage to be empty or contain a few items
                using (IEnumerator<KeyValuePair<string, string>> e = activity.Baggage.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        StringBuilder baggage = new StringBuilder();
                        do
                        {
                            KeyValuePair<string, string> item = e.Current;
                            baggage.Append(item.Key).Append('=').Append(item.Value).Append(',');
                        }
                        while (e.MoveNext());
                        baggage.Remove(baggage.Length - 1, 1);
                        request.Headers.Add(CorrelationContextHeaderName, baggage.ToString());
                    }
                }

                // There is no gurantee that Activity.Current will flow to the Response, so let's stop it here
                activity.Stop();
            }
        }

        private void RaiseResponseEvent(HttpWebRequest request, HttpWebResponse response)
        {
            // Response event could be received several times for the same request in case it was redirected
            // IsLastResponse checks if response is the last one (no more redirects will happen)
            // based on response StatusCode and number or redirects done so far
            if (request.Headers[RequestIdHeaderName] != null && IsLastResponse(request, response))
            {
                // only send Stop if request was instrumented
                this.Write(RequestStopName, new { Request = request, Response = response });
            }
        }

        private bool IsLastResponse(HttpWebRequest request, HttpWebResponse response)
        {
            if (request.AllowAutoRedirect)
            {
                if (response.StatusCode == HttpStatusCode.Ambiguous       ||  // 300
                    response.StatusCode == HttpStatusCode.Moved           ||  // 301
                    response.StatusCode == HttpStatusCode.Redirect        ||  // 302
                    response.StatusCode == HttpStatusCode.RedirectMethod  ||  // 303
                    response.StatusCode == HttpStatusCode.RedirectKeepVerb)   // 307
                {
                    return s_autoRedirectsAccessor(request) >= request.MaximumAutomaticRedirections;
                }
            }

            return true;
        }

        private static void PrepareReflectionObjects()
        {
            // At any point, if the operation failed, it should just throw. The caller should catch all exceptions and swallow.

            // First step: Get all the reflection objects we will ever need.
            Assembly systemNetHttpAssembly = typeof(ServicePoint).Assembly;
            s_connectionGroupListField = typeof(ServicePoint).GetField("m_ConnectionGroupList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionGroupType = systemNetHttpAssembly?.GetType("System.Net.ConnectionGroup");
            s_connectionListField = s_connectionGroupType?.GetField("m_ConnectionList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionType = systemNetHttpAssembly?.GetType("System.Net.Connection");
            s_writeListField = s_connectionType?.GetField("m_WriteList", BindingFlags.Instance | BindingFlags.NonPublic);

            // Second step: Generate an accessor for HttpWebRequest._HttpResponse
            FieldInfo responseField = typeof(HttpWebRequest).GetField("_HttpResponse", BindingFlags.NonPublic | BindingFlags.Instance);
            if (responseField != null)
            {
                string methodName = responseField.ReflectedType.FullName + ".get_" + responseField.Name;
                DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(HttpWebResponse), new Type[] { typeof(HttpWebRequest) }, true);
                ILGenerator generator = getterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, responseField);
                generator.Emit(OpCodes.Ret);
                s_httpResponseAccessor = (Func<HttpWebRequest, HttpWebResponse>)getterMethod.CreateDelegate(typeof(Func<HttpWebRequest, HttpWebResponse>));
            }

            // Third step: Generate an accessor for HttpWebRequest._AutoRedirects
            FieldInfo redirectsField = typeof(HttpWebRequest).GetField("_AutoRedirects", BindingFlags.NonPublic | BindingFlags.Instance);
            if (redirectsField != null)
            {
                string methodName = redirectsField.ReflectedType.FullName + ".get_" + redirectsField.Name;
                DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(int), new Type[] { typeof(HttpWebRequest) }, true);
                ILGenerator generator = getterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, redirectsField);
                generator.Emit(OpCodes.Ret);
                s_autoRedirectsAccessor = (Func<HttpWebRequest, int>)getterMethod.CreateDelegate(typeof(Func<HttpWebRequest, int>));
            }

            // Double checking to make sure we have all the pieces initialized
            if (s_connectionGroupListField == null ||
                s_connectionGroupType == null ||
                s_connectionListField == null ||
                s_connectionType == null ||
                s_writeListField == null ||
                s_httpResponseAccessor == null ||
                s_autoRedirectsAccessor == null)
            {
                // If anything went wrong here, just return false. There is nothing we can do.
                throw new InvalidOperationException("Unable to initialize all required reflection objects");
            }
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

#endregion

        internal static HttpHandlerDiagnosticListener s_instance = new HttpHandlerDiagnosticListener();

#region private fields
        private const string DiagnosticListenerName = "System.Net.Http.Desktop";
        private const string ActivityName = "System.Net.Http.Desktop.HttpRequestOut";
        private const string RequestStartName = "System.Net.Http.Desktop.HttpRequestOut.Start";
        private const string RequestStopName = "System.Net.Http.Desktop.HttpRequestOut.Stop";
        private const string InitializationFailed = "System.Net.Http.InitializationFailed";
        private const string RequestIdHeaderName = "Request-Id";
        private const string CorrelationContextHeaderName = "Correlation-Context";

        // Fields for controlling initialization of the HttpHandlerDiagnosticListener singleton
        private bool initialized = false;

        // Fields for reflection
        private static FieldInfo s_connectionGroupListField;
        private static Type s_connectionGroupType;
        private static FieldInfo s_connectionListField;
        private static Type s_connectionType;
        private static FieldInfo s_writeListField;
        private static Func<HttpWebRequest, HttpWebResponse> s_httpResponseAccessor;
        private static Func<HttpWebRequest, int> s_autoRedirectsAccessor;

#endregion
    }
}
