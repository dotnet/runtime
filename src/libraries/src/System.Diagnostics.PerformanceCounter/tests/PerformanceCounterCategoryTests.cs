// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)] // In appcontainer, cannot write to perf counters
    public static class PerformanceCounterCategoryTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CreatePerformanceCounterCategory_DefaultConstructor()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();
            Assert.Equal(".", pcc.MachineName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CreatePerformanceCounterCategory_NullTests()
        {
            Assert.Throws<ArgumentNullException>(() => new PerformanceCounterCategory(null, "."));
            Assert.Throws<ArgumentException>(() => new PerformanceCounterCategory(string.Empty, "."));
            Assert.Throws<ArgumentException>(() => new PerformanceCounterCategory("category", string.Empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_SetCategoryName_Valid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();
            pcc.CategoryName = "Processor";
            Assert.Equal("Processor", pcc.CategoryName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_SetCategoryName_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<ArgumentNullException>(() => pcc.CategoryName = null);
            Assert.Throws<ArgumentException>(() => pcc.CategoryName = string.Empty);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_SetMachineName_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<ArgumentException>(() => pcc.MachineName = string.Empty);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_SetMachineName_ValidCategoryNameNull()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            pcc.MachineName = "machineName";
            Assert.Equal("machineName", pcc.MachineName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_SetMachineName_ValidCategoryNameNotNull()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            pcc.CategoryName = "Processor";
            pcc.MachineName = "machineName";
            Assert.Equal("machineName", pcc.MachineName);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetCounterHelp_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<InvalidOperationException>(() => pcc.CategoryHelp);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CategoryType_MultiInstance()
        {
            var name = nameof(PerformanceCounterCategory_CategoryType_MultiInstance) + "_Counter";

            var category = Helpers.CreateCategory(name, PerformanceCounterCategoryType.MultiInstance);

            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory(category));

            Assert.Equal(PerformanceCounterCategoryType.MultiInstance, Helpers.RetryOnAllPlatforms(() => pcc.CategoryType));
            PerformanceCounterCategory.Delete(category);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CategoryType_SingleInstance()
        {
            var name = nameof(PerformanceCounterCategory_CategoryType_SingleInstance) + "_Counter";

            var category = Helpers.CreateCategory(name, PerformanceCounterCategoryType.SingleInstance);

            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory(category));

            Assert.Equal(PerformanceCounterCategoryType.SingleInstance, Helpers.RetryOnAllPlatforms(() => pcc.CategoryType));
            PerformanceCounterCategory.Delete(category);
        }

#pragma warning disable 0618 // obsolete warning
        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_Create_Obsolete()
        {
            var name = nameof(PerformanceCounterCategory_Create_Obsolete) + "_Counter";
            var category = name + "_Category";

            Helpers.DeleteCategory(category);

            PerformanceCounterCategory.Create(category, "category help", name, "counter help");

            Assert.True(PerformanceCounterCategory.Exists(category));
            PerformanceCounterCategory.Delete(category);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_Create_Obsolete_CCD()
        {
            var name = nameof(PerformanceCounterCategory_Create_Obsolete_CCD) + "_Counter";
            var category = name + "_Category";

            CounterCreationData ccd = new CounterCreationData(name, "counter help", PerformanceCounterType.NumberOfItems32);
            CounterCreationDataCollection ccdc = new CounterCreationDataCollection();
            ccdc.Add(ccd);

            Helpers.DeleteCategory(category);

            PerformanceCounterCategory.Create(category, "category help", ccdc);

            Assert.True(PerformanceCounterCategory.Exists(category));
            PerformanceCounterCategory.Delete(category);
        }
#pragma warning restore 0618

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_Create_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.Create(null, "Categoryhelp", PerformanceCounterCategoryType.SingleInstance, "counter name", "counter help"));
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.Create("category name", "Categoryhelp", PerformanceCounterCategoryType.SingleInstance, null, "counter help"));
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.Create("category name", "Category help", PerformanceCounterCategoryType.SingleInstance, null));
            Assert.Throws<InvalidOperationException>(() => PerformanceCounterCategory.Create("Processor", "Category help", PerformanceCounterCategoryType.MultiInstance, "Interrupts/sec", "counter help"));

            string maxCounter = new string('a', 32769);

            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.Create("Category name", "Category help", PerformanceCounterCategoryType.SingleInstance, maxCounter, "counter help"));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.Create(maxCounter, "Category help", PerformanceCounterCategoryType.SingleInstance, "Counter name", "counter help"));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.Create("Category name", maxCounter, PerformanceCounterCategoryType.SingleInstance, "Counter name", "counter help"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetCategories()
        {
            PerformanceCounterCategory[] categories = PerformanceCounterCategory.GetCategories();

            Assert.True(categories.Length > 0);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetCategories_StaticInvalid()
        {
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.GetCategories(string.Empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CounterExists_InterruptsPerSec()
        {
            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory("Processor"));

            Assert.True(pcc.CounterExists("Interrupts/sec"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CounterExists_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<ArgumentNullException>(() => pcc.CounterExists(null));
            Assert.Throws<InvalidOperationException>(() => pcc.CounterExists("Interrupts/sec"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CounterExists_StaticInterruptsPerSec()
        {
            Assert.True(PerformanceCounterCategory.CounterExists("Interrupts/sec", "Processor"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_CounterExists_StaticInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.CounterExists(null, "Processor"));
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.CounterExists("Interrupts/sec", null));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.CounterExists("Interrupts/sec", string.Empty));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.CounterExists("Interrupts/sec", "Processor", string.Empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_DeleteCategory_Invalid()
        {
            Assert.Throws<InvalidOperationException>(() => PerformanceCounterCategory.Delete("Processor"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_DeleteCategory()
        {
            var name = nameof(PerformanceCounterCategory_DeleteCategory) + "_Counter";
            var category = Helpers.CreateCategory(name, PerformanceCounterCategoryType.SingleInstance);

            PerformanceCounterCategory.Delete(category);

            Assert.False(PerformanceCounterCategory.Exists(category));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_Exists_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.Exists(null, "."));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.Exists(string.Empty, "."));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.Exists("Processor", string.Empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetCounters()
        {
            var name = nameof(PerformanceCounterCategory_GetCounters) + "_Counter";
            var category = Helpers.CreateCategory(name, PerformanceCounterCategoryType.SingleInstance);

            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory(category));
            PerformanceCounter[] counters = pcc.GetCounters();

            Assert.True(counters.Length > 0);
            PerformanceCounterCategory.Delete(category);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetCounters_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<ArgumentNullException>(() => pcc.GetCounters(null));
            Assert.Throws<InvalidOperationException>(() => pcc.GetCounters(string.Empty));

            pcc.CategoryName = "Processor";

            Assert.Throws<InvalidOperationException>(() => pcc.GetCounters("Not An Instance"));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_GetInstanceNames_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<InvalidOperationException>(() => pcc.GetInstanceNames());
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_InstanceExists_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<ArgumentNullException>(() => pcc.InstanceExists(null));
            Assert.Throws<InvalidOperationException>(() => pcc.InstanceExists(""));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_InstanceExists_Static()
        {
            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory("Processor"));

            string[] instances = pcc.GetInstanceNames();
            Assert.True(instances.Length > 0);

            foreach (string instance in instances)
            {
                Assert.True(PerformanceCounterCategory.InstanceExists(instance, "Processor"));
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_InstanceExists_StaticInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.InstanceExists(null, "Processor", "."));
            Assert.Throws<ArgumentNullException>(() => PerformanceCounterCategory.InstanceExists("", null, "."));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.InstanceExists("", string.Empty, "."));
            Assert.Throws<ArgumentException>(() => PerformanceCounterCategory.InstanceExists("", "Processor", string.Empty));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_ReadCategory()
        {
            PerformanceCounterCategory pcc = Helpers.RetryOnAllPlatforms(() => new PerformanceCounterCategory("Processor"));

            InstanceDataCollectionCollection idColCol = pcc.ReadCategory();

            Assert.NotNull(idColCol);
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.IsElevatedAndCanWriteToPerfCounters))]
        public static void PerformanceCounterCategory_ReadCategory_Invalid()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory();

            Assert.Throws<InvalidOperationException>(() => pcc.ReadCategory());
        }
    }
}
