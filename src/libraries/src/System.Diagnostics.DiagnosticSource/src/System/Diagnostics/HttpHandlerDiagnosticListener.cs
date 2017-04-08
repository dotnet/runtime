// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;

// This HttpHandlerDiagnosticListener class is applicable only for .NET 4.5/4.6, and not for .NET core.
// If you are making these changes, please test your changes manually via custom test applications.

namespace System.Diagnostics
{
    /// <summary>
    /// A HttpHandlerDiagnosticListener is a DiagnosticListener for .NET 4.5/4.6 where HttpClient
    /// doesn't have a DiagnosticListener built-in. This class is not used for .NET Core because
    /// HttpClient in .NET Core already emits DiagnosticSource events. This class compensates for
    /// that in .NET 4.5/4.6. HttpHandlerDiagnosticListener has no public constructor, but it has a
    /// singleton style Initialize method to control its creation.
    /// 
    /// To use this, the application just needs to call HttpHandlerDiagnosticListener.Initialize(), and this
    /// will register itself with the DiagnosticListener's all listeners collection.
    /// </summary>
    internal class HttpHandlerDiagnosticListener : DiagnosticListener
    {
        /// <summary>
        /// Overriding base class implementation just to give us a chance to initialize.  
        /// </summary>
        public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled)
        {
            Initialize();
            return base.Subscribe(observer, isEnabled);
        }

        /// <summary>
        /// Overriding base class implementation just to give us a chance to initialize.  
        /// </summary>
        public override IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Func<string, object, object, bool> isEnabled)
        {
            Initialize();
            return base.Subscribe(observer, isEnabled);
        }

        /// <summary>
        /// Initializes all the reflection objects it will ever need. Reflection is costly, but it's better to take
        /// this one time performance hit than to get it multiple times later, or do it lazily and have to worry about
        /// threading issues. If Initialize has been called before, it will not doing anything.
        /// </summary>
        private void Initialize()
        {
            lock (this.initializationLock)
            {
                if (!this.initialized)
                {
                    // This flag makes sure we only do this once. Even if we failed to initialize in an
                    // earlier time, we should not retry because this initialization is not cheap and
                    // the likelihood it will succeed the second time is very small.
                    this.initialized = true;

                    try
                    {
                        PrepareReflectionObjects();
                        PerformInjection();
                    }
                    catch (Exception)
                    {
                        // If anything went wrong, just no-op
                    }
                }
            }
        }

#region private helper classes

        /// <summary>
        /// Helper class used for ServicePointManager.s_ServicePointTable. The goal here is to
        /// intercept each new ServicePoint object being added to ServicePointManager.s_ServicePointTable
        /// and replace its ConnectionGroupList hashtable field.
        /// </summary>
        private class ServicePointHashtable : Hashtable
        {
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
                            ConnectionGroupHashtable newTable = new ConnectionGroupHashtable();
                            if (originalTable != null)
                            {
                                foreach (DictionaryEntry entry in originalTable)
                                {
                                    newTable[entry.Key] = entry.Value;
                                }
                            }
                            s_connectionGroupListField.SetValue(servicePoint, newTable);
                        }
                    }

                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// Helper class used for ServicePoint.m_ConnectiopGroupList. The goal here is to
        /// intercept each new ConnectionGroup object being added to ServicePoint.m_ConnectionGroupList
        /// and replace its m_ConnectionList arraylist field.
        /// </summary>
        private class ConnectionGroupHashtable : Hashtable
        {
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
                        ConnectionArrayList newArrayList = new ConnectionArrayList();
                        if (originalArrayList != null)
                        {
                            foreach (object entry in originalArrayList)
                            {
                                newArrayList.Add(entry);
                            }
                        }
                        s_connectionListField.SetValue(value, newArrayList);
                    }

                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// Helper class used for ConnectionGroup.m_ConnectionList. The goal here is to
        /// intercept each new Connection object being added to ConnectionGroup.m_ConnectionList
        /// and replace its m_WriteList arraylist field.
        /// </summary>
        private class ConnectionArrayList : ArrayList
        {
            public override int Add(object value)
            {
                if (s_connectionType.IsInstanceOfType(value))
                {
                    // Replace the HttpWebRequest arraylist inside this Connection object,
                    // which allows us to intercept each new HttpWebRequest object added under
                    // this Connection.
                    ArrayList originalArrayList = s_writeListField.GetValue(value) as ArrayList;
                    HttpWebRequestArrayList newArrayList = new HttpWebRequestArrayList();
                    if (originalArrayList != null)
                    {
                        foreach (object entry in originalArrayList)
                        {
                            newArrayList.Add(entry);
                        }
                    }
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
        private class HttpWebRequestArrayList : ArrayList
        {
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
        /// Private constructor. Make sure every caller has to create this object via the Initialize
        /// method.
        /// </summary>
        private HttpHandlerDiagnosticListener() : base(DiagnosticListenerName)
        {
        }

        private void RaiseRequestEvent(HttpWebRequest request)
        {
            Guid loggingRequestId = Guid.Empty;

            // If System.Net.Http.Request is on, raise the event
            if (this.IsEnabled(RequestWriteName))
            {
                long timestamp = Stopwatch.GetTimestamp();
                loggingRequestId = Guid.NewGuid();
                this.Write(RequestWriteName,
                    new
                    {
                        Request = request,
                        //LoggingRequestId = loggingRequestId,
                        Timestamp = timestamp
                    }
                );
            }
        }

        private void RaiseResponseEvent(HttpWebRequest request, HttpWebResponse response)
        {
            Guid loggingRequestId = Guid.Empty;

            // TODO: Figure what's the right way to detect error conditions here.
            if (response.StatusCode >= (HttpStatusCode)400)
            {
                if (this.IsEnabled(ExceptionEventName))
                {
                    //this.Write(ExceptionEventName, new { Exception = ex, Request = request });
                    this.Write(ExceptionEventName, new { Response = response });
                }
            }

            if (this.IsEnabled(ResponseWriteName))
            {
                long timestamp = Stopwatch.GetTimestamp();
                this.Write(ResponseWriteName,
                    new
                    {
                        Request = request,
                        Response = response,
                        //LoggingRequestId = loggingRequestId,
                        TimeStamp = timestamp
                        //RequestTaskStatus = responseTask.Status
                    }
                );
            }
        }

        private static void PrepareReflectionObjects()
        {
            // At any point, if the operation failed, it should just throw, including NullReferenceException.
            // The caller should catch all exceptions and swallow.

            // First step: Get all the reflection objects we will ever need.
            Assembly systemNetHttpAssembly = typeof(ServicePoint).Assembly;
            s_connectionGroupListField = typeof(ServicePoint).GetField("m_ConnectionGroupList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionGroupType = systemNetHttpAssembly?.GetType("System.Net.ConnectionGroup");
            s_connectionListField = s_connectionGroupType?.GetField("m_ConnectionList", BindingFlags.Instance | BindingFlags.NonPublic);
            s_connectionType = systemNetHttpAssembly?.GetType("System.Net.Connection");
            s_writeListField = s_connectionType?.GetField("m_WriteList", BindingFlags.Instance | BindingFlags.NonPublic);

            // Second step: Generate an accessor for HttpWebRequest._HttpResponse
            FieldInfo field = typeof(HttpWebRequest).GetField("_HttpResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            string methodName = field?.ReflectedType.FullName + ".get_" + field.Name;
            if (!string.IsNullOrEmpty(methodName))
            {
                DynamicMethod getterMethod = new DynamicMethod(methodName, typeof(HttpWebResponse), new Type[] { typeof(HttpWebRequest) }, true);
                ILGenerator generator = getterMethod.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Ret);
                s_httpResponseAccessor = (Func<HttpWebRequest, HttpWebResponse>)getterMethod.CreateDelegate(typeof(Func<HttpWebRequest, HttpWebResponse>));
            }

            // Double checking to make sure we have all the pieces initialized
            if (s_connectionGroupListField == null ||
                s_connectionGroupType == null ||
                s_connectionListField == null ||
                s_connectionType == null ||
                s_writeListField == null ||
                s_httpResponseAccessor == null)
            {
                throw new InvalidOperationException("Unable to initialize all required reflection objects");
            }
        }

        private static void PerformInjection()
        {
            FieldInfo servicePointTableField = typeof(ServicePointManager).GetField("s_ServicePointTable", BindingFlags.Static | BindingFlags.NonPublic);
            if (servicePointTableField == null)
            {
                throw new InvalidOperationException("Unable to access the ServicePointTable field");
            }

            Hashtable originalTable = servicePointTableField.GetValue(null) as Hashtable;
            ServicePointHashtable newTable = new ServicePointHashtable();

            // Copy the existing entries over to the new table, and replace the field with our new table
            if (originalTable != null)
            {
                foreach (DictionaryEntry entry in originalTable)
                {
                    newTable[entry.Key] = entry.Value;
                }
            }

            servicePointTableField.SetValue(null, newTable);
        }

#endregion

        internal static HttpHandlerDiagnosticListener s_instance = new HttpHandlerDiagnosticListener();

#region private fields
        private const string DiagnosticListenerName = "System.Net.Http.Desktop.V4";
        private const string RequestWriteName = "System.Net.Http.Request";
        private const string ResponseWriteName = "System.Net.Http.Response";
        private const string ExceptionEventName = "System.Net.Http.Exception";

        // Fields for controlling initialization of the HttpHandlerDiagnosticListener singleton
        private object initializationLock = new object();
        private bool initialized = false;

        // Fields for reflection
        private static FieldInfo s_connectionGroupListField;
        private static Type s_connectionGroupType;
        private static FieldInfo s_connectionListField;
        private static Type s_connectionType;
        private static FieldInfo s_writeListField;
        private static Func<HttpWebRequest, HttpWebResponse> s_httpResponseAccessor;

#endregion
    }
}
