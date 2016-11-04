// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Dynamic
{
    internal static partial class UpdateDelegates
    {
#if FEATURE_COMPILE
        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute0<TRet>(CallSite site)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, TRet>>)site;
            Func<CallSite, TRet>[] applicable;
            Func<CallSite, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = Array.Empty<object>();

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch0<TRet>(CallSite site)
        {
            site._match = false;
            return default(TRet);
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute1<T0, TRet>(CallSite site, T0 arg0)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, TRet>>)site;
            Func<CallSite, T0, TRet>[] applicable;
            Func<CallSite, T0, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch1<T0, TRet>(CallSite site, T0 arg0)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute2<T0, T1, TRet>(CallSite site, T0 arg0, T1 arg1)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, TRet>>)site;
            Func<CallSite, T0, T1, TRet>[] applicable;
            Func<CallSite, T0, T1, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch2<T0, T1, TRet>(CallSite site, T0 arg0, T1 arg1)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute3<T0, T1, T2, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, TRet>>)site;
            Func<CallSite, T0, T1, T2, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch3<T0, T1, T2, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute4<T0, T1, T2, T3, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch4<T0, T1, T2, T3, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute5<T0, T1, T2, T3, T4, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch5<T0, T1, T2, T3, T4, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute6<T0, T1, T2, T3, T4, T5, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, T5, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, T5, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch6<T0, T1, T2, T3, T4, T5, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute7<T0, T1, T2, T3, T4, T5, T6, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch7<T0, T1, T2, T3, T4, T5, T6, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute8<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch8<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch9<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static TRet UpdateAndExecute10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>>)site;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>[] applicable;
            Func<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet> rule, originalRule = @this.Target;
            TRet result;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return result;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    result = rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return result;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static TRet NoMatch10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, TRet>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            site._match = false;
            return default(TRet);
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid1<T0>(CallSite site, T0 arg0)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0>>)site;
            Action<CallSite, T0>[] applicable;
            Action<CallSite, T0> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid1<T0>(CallSite site, T0 arg0)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid2<T0, T1>(CallSite site, T0 arg0, T1 arg1)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1>>)site;
            Action<CallSite, T0, T1>[] applicable;
            Action<CallSite, T0, T1> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid2<T0, T1>(CallSite site, T0 arg0, T1 arg1)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid3<T0, T1, T2>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2>>)site;
            Action<CallSite, T0, T1, T2>[] applicable;
            Action<CallSite, T0, T1, T2> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid3<T0, T1, T2>(CallSite site, T0 arg0, T1 arg1, T2 arg2)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid4<T0, T1, T2, T3>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3>>)site;
            Action<CallSite, T0, T1, T2, T3>[] applicable;
            Action<CallSite, T0, T1, T2, T3> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid4<T0, T1, T2, T3>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid5<T0, T1, T2, T3, T4>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4>>)site;
            Action<CallSite, T0, T1, T2, T3, T4>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid5<T0, T1, T2, T3, T4>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid6<T0, T1, T2, T3, T4, T5>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5>>)site;
            Action<CallSite, T0, T1, T2, T3, T4, T5>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4, T5> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4, arg5);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid6<T0, T1, T2, T3, T4, T5>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid7<T0, T1, T2, T3, T4, T5, T6>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6>>)site;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid7<T0, T1, T2, T3, T4, T5, T6>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid8<T0, T1, T2, T3, T4, T5, T6, T7>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>>)site;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid8<T0, T1, T2, T3, T4, T5, T6, T7>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>>)site;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            site._match = false;
            return;
        }



        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        internal static void UpdateAndExecuteVoid10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            //
            // Declare the locals here upfront. It actually saves JIT stack space.
            //
            var @this = (CallSite<Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>)site;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>[] applicable;
            Action<CallSite, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> rule, originalRule = @this.Target;


            //
            // Create matchmaker and its site. We'll need them regardless.
            //
            site = CallSiteOps.CreateMatchmaker(@this);

            //
            // Level 1 cache lookup
            //
            if ((applicable = CallSiteOps.GetRules(@this)) != null)
            {
                for (int i = 0; i < applicable.Length; i++)
                {
                    rule = applicable[i];

                    //
                    // Execute the rule
                    //

                    // if we've already tried it skip it...
                    if ((object)rule != (object)originalRule)
                    {
                        @this.Target = rule;
                        rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);

                        if (CallSiteOps.GetMatch(site))
                        {
                            CallSiteOps.UpdateRules(@this, i);
                            return;
                        }        

                        // Rule didn't match, try the next one
                        CallSiteOps.ClearMatch(site);            
                    }
                }
            }

            //
            // Level 2 cache lookup
            //

            //
            // Any applicable rules in level 2 cache?
            //

            var cache = CallSiteOps.GetRuleCache(@this);

            applicable = cache.GetRules();
            for (int i = 0; i < applicable.Length; i++)
            {
                rule = applicable[i];

                //
                // Execute the rule
                //
                @this.Target = rule;

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // Rule worked. Add it to level 1 cache
                        //
                        CallSiteOps.AddRule(@this, rule);
                        // and then move it to the front of the L2 cache
                        CallSiteOps.MoveRule(cache, rule, i);
                    }
                }

                // Rule didn't match, try the next one
                CallSiteOps.ClearMatch(site);
            }

            //
            // Miss on Level 0, 1 and 2 caches. Create new rule
            //

            rule = null;
            var args = new object[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };

            for (; ; )
            {
                @this.Target = originalRule;
                rule = @this.Target = @this.Binder.BindCore(@this, args);

                //
                // Execute the rule on the matchmaker site
                //

                try
                {
                    rule(site, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
                    if (CallSiteOps.GetMatch(site))
                    {
                        return;
                    }
                }
                finally
                {
                    if (CallSiteOps.GetMatch(site))
                    {
                        //
                        // The rule worked. Add it to level 1 cache.
                        //
                        CallSiteOps.AddRule(@this, rule);
                    }
                }

                // Rule we got back didn't work, try another one
                CallSiteOps.ClearMatch(site);
            }
        }

        [Obsolete("pregenerated CallSite<T>.Update delegate", true)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
        internal static void NoMatchVoid10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(CallSite site, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            site._match = false;
            return;
        }
#endif
    }
}
