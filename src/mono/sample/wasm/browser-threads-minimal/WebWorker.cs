using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices.JavaScript
{
    // this is just temporary thin wrapper to expose future public API
    public partial class WebWorker
    {
        private static MethodInfo runAsyncMethod;
        private static MethodInfo runAsyncVoidMethod;
        private static MethodInfo runMethod;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.WebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "work in progress")]
        public static Task<T> RunAsync<T>(Func<Task<T>> body)
        {
            if(runAsyncMethod == null)
            {
                var webWorker = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.WebWorker");
                runAsyncMethod = webWorker.GetMethod("RunAsync", BindingFlags.Public|BindingFlags.Static);
            }
            return (Task<T>)runAsyncMethod.Invoke(null, new object[] { body });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.WebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "work in progress")]
        public static Task RunAsync(Func<Task> body)
        {
            if(runAsyncVoidMethod == null)
            {
                var webWorker = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.WebWorker");
                runAsyncVoidMethod = webWorker.GetMethod("RunAsyncVoid", BindingFlags.Public|BindingFlags.Static);
            }
            return (Task)runAsyncVoidMethod.Invoke(null, new object[] { body });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.WebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "work in progress")]
        public static Task RunAsync(Action body)
        {
            if(runMethod == null)
            {
                var webWorker = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.WebWorker");
                runMethod = webWorker.GetMethod("Run", BindingFlags.Public|BindingFlags.Static);
            }
            return (Task)runMethod.Invoke(null, new object[] { body });
        }
    }
}