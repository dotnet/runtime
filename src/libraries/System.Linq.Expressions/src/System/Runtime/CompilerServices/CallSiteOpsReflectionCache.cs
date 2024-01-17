// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Runtime.CompilerServices;
internal static class CallSiteOpsReflectionCache<T> where T : class
{
    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo CreateMatchmaker = ((Delegate)CallSiteOps.CreateMatchmaker<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo UpdateRules = ((Delegate)CallSiteOps.UpdateRules<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo GetRules = ((Delegate)CallSiteOps.GetRules<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo GetRuleCache = ((Delegate)CallSiteOps.GetRuleCache<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo GetCachedRules = ((Delegate)CallSiteOps.GetCachedRules<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo AddRule = ((Delegate)CallSiteOps.AddRule<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    public static readonly MethodInfo MoveRule = ((Delegate)CallSiteOps.MoveRule<T>).GetMethodInfo();

    [Obsolete("CallSiteOps has been deprecated and is not supported.", error: false), EditorBrowsable(EditorBrowsableState.Never)]
    [UnconditionalSuppressMessage("DynamicCode", "IL3050",
        Justification = "CallSiteOps is obsolete and CallSite has RUC. Propagating warnings through fields isn't worth it.")]
    public static readonly MethodInfo Bind = ((Delegate)CallSiteOps.Bind<T>).GetMethodInfo();
}
