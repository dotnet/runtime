using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Sample
{
    // this is just temporary thin wrapper to expose future public API
    public partial class JSWebWorker
    {
        private static MethodInfo runAsyncMethod;
        private static MethodInfo runAsyncVoidMethod;

        public static Task RunAsync(Func<Task> body)
        {
            return RunAsync(body, CancellationToken.None);
        }

        public static Task<T> RunAsync<T>(Func<Task<T>> body)
        {
            return RunAsync(body, CancellationToken.None);
        }


        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:UnrecognizedReflectionPattern", Justification = "work in progress")]
        public static Task<T> RunAsync<T>(Func<Task<T>> body, CancellationToken cancellationToken)
        {
            if(runAsyncMethod == null)
            {
                var webWorker = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSWebWorker");
                runAsyncMethod = webWorker.GetMethod("RunAsyncGeneric", BindingFlags.NonPublic|BindingFlags.Static);
            }

            var genericRunAsyncMethod = runAsyncMethod.MakeGenericMethod(typeof(T));
            return (Task<T>)genericRunAsyncMethod.Invoke(null, new object[] { body, cancellationToken });
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern", Justification = "work in progress")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:UnrecognizedReflectionPattern", Justification = "work in progress")]
        public static Task RunAsync(Func<Task> body, CancellationToken cancellationToken)
        {
            if(runAsyncVoidMethod == null)
            {
                var webWorker = typeof(JSObject).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSWebWorker");
                runAsyncVoidMethod = webWorker.GetMethod("RunAsyncVoid", BindingFlags.NonPublic|BindingFlags.Static);
            }
            return (Task)runAsyncVoidMethod.Invoke(null, new object[] { body, cancellationToken });
        }
    }
}