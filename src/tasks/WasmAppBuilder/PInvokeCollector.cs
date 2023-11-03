// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks;

#pragma warning disable CA1067
#pragma warning disable CS0649
internal sealed class PInvoke : IEquatable<PInvoke>
#pragma warning restore CA1067
{
    public PInvoke(string entryPoint, string module, MethodInfo method)
    {
        EntryPoint = entryPoint;
        Module = module;
        Method = method;
    }

    public string EntryPoint;
    public string Module;
    public MethodInfo Method;
    public bool Skip;

    public bool Equals(PInvoke? other)
        => other != null &&
            string.Equals(EntryPoint, other.EntryPoint, StringComparison.Ordinal) &&
            string.Equals(Module, other.Module, StringComparison.Ordinal) &&
            string.Equals(Method.ToString(), other.Method.ToString(), StringComparison.Ordinal);

    public override string ToString() => $"{{ EntryPoint: {EntryPoint}, Module: {Module}, Method: {Method}, Skip: {Skip} }}";
}
#pragma warning restore CS0649

internal sealed class PInvokeComparer : IEqualityComparer<PInvoke>
{
    public bool Equals(PInvoke? x, PInvoke? y)
    {
        if (x == null && y == null)
            return true;
        if (x == null || y == null)
            return false;

        return x.Equals(y);
    }

    public int GetHashCode(PInvoke pinvoke)
        => $"{pinvoke.EntryPoint}{pinvoke.Module}{pinvoke.Method}".GetHashCode();
}


internal sealed class PInvokeCollector {
    private readonly Dictionary<Assembly, bool> _assemblyDisableRuntimeMarshallingAttributeCache = new();
    private TaskLoggingHelper Log { get; init; }

    public PInvokeCollector(TaskLoggingHelper log)
    {
        Log = log;
    }

    public void CollectPInvokes(List<PInvoke> pinvokes, List<PInvokeCallback> callbacks, HashSet<string> signatures, Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            try
            {
                CollectPInvokesForMethod(method);
                if (DoesMethodHaveCallbacks(method))
                    callbacks.Add(new PInvokeCallback(method));
            }
            catch (Exception ex) when (ex is not LogAsErrorException)
            {
                Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0,
                        $"Could not get pinvoke, or callbacks for method '{type.FullName}::{method.Name}' because '{ex.Message}'");
            }
        }

        if (HasAttribute(type, "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"))
        {
            var method = type.GetMethod("Invoke");

            if (method != null)
            {
                string? signature = SignatureMapper.MethodToSignature(method!);
                if (signature == null)
                    throw new NotSupportedException($"Unsupported parameter type in method '{type.FullName}.{method.Name}'");

                if (signatures.Add(signature))
                    Log.LogMessage(MessageImportance.Low, $"Adding pinvoke signature {signature} for method '{type.FullName}.{method.Name}'");
            }
        }

        void CollectPInvokesForMethod(MethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            {
                var dllimport = method.CustomAttributes.First(attr => attr.AttributeType.Name == "DllImportAttribute");
                var module = (string)dllimport.ConstructorArguments[0].Value!;
                var entrypoint = (string)dllimport.NamedArguments.First(arg => arg.MemberName == "EntryPoint").TypedValue.Value!;
                pinvokes.Add(new PInvoke(entrypoint, module, method));

                string? signature = SignatureMapper.MethodToSignature(method);
                if (signature == null)
                {
                    throw new NotSupportedException($"Unsupported parameter type in method '{type.FullName}.{method.Name}'");
                }

                if (signatures.Add(signature))
                    Log.LogMessage(MessageImportance.Low, $"Adding pinvoke signature {signature} for method '{type.FullName}.{method.Name}'");
            }
        }

        bool DoesMethodHaveCallbacks(MethodInfo method)
        {
            if (!MethodHasCallbackAttributes(method))
                return false;

            if (TryIsMethodGetParametersUnsupported(method, out string? reason))
            {
                Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0,
                        $"Skipping callback '{method.DeclaringType!.FullName}::{method.Name}' because '{reason}'.");
                return false;
            }

            if (method.DeclaringType != null && HasAssemblyDisableRuntimeMarshallingAttribute(method.DeclaringType.Assembly))
                return true;

            // No DisableRuntimeMarshalling attribute, so check if the params/ret-type are
            // blittable
            bool isVoid = method.ReturnType.FullName == "System.Void";
            if (!isVoid && !IsBlittable(method.ReturnType))
                Error($"The return type '{method.ReturnType.FullName}' of pinvoke callback method '{method}' needs to be blittable.");

            foreach (var p in method.GetParameters())
            {
                if (!IsBlittable(p.ParameterType))
                    Error("Parameter types of pinvoke callback method '" + method + "' needs to be blittable.");
            }

            return true;
        }

        static bool MethodHasCallbackAttributes(MethodInfo method)
        {
            foreach (CustomAttributeData cattr in CustomAttributeData.GetCustomAttributes(method))
            {
                try
                {
                    if (cattr.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute" ||
                        cattr.AttributeType.Name == "MonoPInvokeCallbackAttribute")
                    {
                        return true;
                    }
                }
                catch
                {
                    // Assembly not found, ignore
                }
            }

            return false;
        }
    }

    public static bool IsBlittable(Type type)
    {
        if (type.IsPrimitive || type.IsByRef || type.IsPointer || type.IsEnum)
            return true;
        else
            return false;
    }

    private static void Error(string msg) => throw new LogAsErrorException(msg);

    private static bool HasAttribute(MemberInfo element, params string[] attributeNames)
    {
        foreach (CustomAttributeData cattr in CustomAttributeData.GetCustomAttributes(element))
        {
            try
            {
                for (int i = 0; i < attributeNames.Length; ++i)
                {
                    if (cattr.AttributeType.FullName == attributeNames [i] ||
                        cattr.AttributeType.Name == attributeNames[i])
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Assembly not found, ignore
            }
        }
        return false;
    }

    private static bool TryIsMethodGetParametersUnsupported(MethodInfo method, [NotNullWhen(true)] out string? reason)
    {
        try
        {
            method.GetParameters();
        }
        catch (NotSupportedException nse)
        {
            reason = nse.Message;
            return true;
        }
        catch
        {
            // not concerned with other exceptions
        }

        reason = null;
        return false;
    }

    private bool HasAssemblyDisableRuntimeMarshallingAttribute(Assembly assembly)
    {
        if (!_assemblyDisableRuntimeMarshallingAttributeCache.TryGetValue(assembly, out var value))
        {
            _assemblyDisableRuntimeMarshallingAttributeCache[assembly] = value = assembly
                .GetCustomAttributesData()
                .Any(d => d.AttributeType.Name == "DisableRuntimeMarshallingAttribute");
        }

       value = assembly.GetCustomAttributesData().Any(d => d.AttributeType.Name == "DisableRuntimeMarshallingAttribute");

        return value;
    }
}

#pragma warning disable CS0649
internal sealed class PInvokeCallback
{
    public PInvokeCallback(MethodInfo method)
    {
        Method = method;
    }

    public MethodInfo Method;
    public string? EntryName;
}
#pragma warning restore CS0649
