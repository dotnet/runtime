// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Threading;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class AggregateCatalogTest
    {
        [Fact]
        public void Constructor1_ShouldNotThrow()
        {
            new AggregateCatalog();
        }

        [Fact]
        public void Constructor1_ShouldSetCatalogsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog();

            Assert.Empty(catalog.Catalogs);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void Constructor1_ShouldSetPartsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog();

            Assert.Empty(catalog.Parts);
        }

        [Fact]
        public void Constructor3_NullAsCatalogsArgument_ShouldSetCatalogsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog((IEnumerable<ComposablePartCatalog>)null);

            Assert.Empty(catalog.Catalogs);
        }

        [Fact]
        public void Constructor3_EmptyIEnumerableAsCatalogsArgument_ShouldSetCatalogsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog(Enumerable.Empty<ComposablePartCatalog>());

            Assert.Empty(catalog.Catalogs);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void Constructor3_NullAsCatalogsArgument_ShouldSetPartsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog((IEnumerable<ComposablePartCatalog>)null);

            Assert.Empty(catalog.Parts);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void Constructor3_EmptyIEnumerableAsCatalogsArgument_ShouldSetPartsPropertyToEmpty()
        {
            var catalog = new AggregateCatalog(Enumerable.Empty<ComposablePartCatalog>());

            Assert.Empty(catalog.Parts);
        }

        [Fact]
        [ActiveIssue(25498)]
        public void Constructor3_ArrayWithNullAsCatalogsArgument_ShouldThrowArgument()
        {
            var catalogs = new ComposablePartCatalog[] { null };

            AssertExtensions.Throws<ArgumentException>("catalogs", () =>
            {
                new AggregateCatalog(catalogs);
            });
        }

        [Fact]
        public void Catalogs_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateAggregateCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var catalogs = catalog.Catalogs;
            });
        }

        [Fact]
        public void Parts_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateAggregateCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var parts = catalog.Parts;
            });
        }

        [Fact]
        public void GetExports_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateAggregateCatalog();
            catalog.Dispose();
            var definition = ImportDefinitionFactory.Create();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                catalog.GetExports(definition);
            });
        }

        [Fact]
        [ActiveIssue(25498)]
        public void GetExports_NullAsConstraintArgument_ShouldThrowArgumentNull()
        {
            var catalog = CreateAggregateCatalog();

            AssertExtensions.Throws<ArgumentNullException>("definition", () =>
            {
                catalog.GetExports((ImportDefinition)null);
            });
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            using (var catalog = CreateAggregateCatalog())
            {
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var catalog = CreateAggregateCatalog();
            catalog.Dispose();
            catalog.Dispose();
            catalog.Dispose();
        }

        [Fact]
        public void EnumeratePartsProperty_ShouldSucceed()
        {
            using (var catalog = new AggregateCatalog(
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff))))
            {
                Assert.True(catalog.Catalogs.Count() == 6);
                Assert.True(catalog.Parts.Count() == 6);
            }
        }

        [Fact]
        public void MutableCatalogNotifications()
        {
            int step = 0;
            int changedStep = 0;
            var catalog = new AggregateCatalog();

            var typePartCatalog = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog1 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog2 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog3 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog4 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog5 = new TypeCatalog(typeof(SharedPartStuff));

            // Smoke test on inner collection
            catalog.Catalogs.Add(typePartCatalog);
            catalog.Catalogs.Remove(typePartCatalog);
            catalog.Catalogs.Clear();
            Assert.True(catalog.Catalogs.Count == 0);

            // Add notifications
            catalog.Changed += delegate (object source, ComposablePartCatalogChangeEventArgs args)
            {
                // Local code
                ++step;
                ++step;
                changedStep = step;
            };

            //Add something then verify counters
            catalog.Catalogs.Add(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 1);
            Assert.True(changedStep == 2);

            // Reset counters
            step = changedStep = 0;

            // Remove something then verify counters
            catalog.Catalogs.Remove(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 0);
            Assert.True(changedStep == 2);

            //Now Add it back
            catalog.Catalogs.Add(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 1);

            step = changedStep = 0;
            // Now clear the collection and verify counters
            catalog.Catalogs.Clear();
            Assert.True(catalog.Catalogs.Count == 0);
            Assert.True(changedStep == 2);

            // Now remove a non existent item and verify counters
            step = changedStep = 0;
            bool removed = catalog.Catalogs.Remove(typePartCatalog);
            Assert.True(removed == false);
            Assert.True(changedStep == 0);

            // Add a bunch
            step = changedStep = 0;
            catalog.Catalogs.Add(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 1);
            Assert.True(changedStep == 2);

            catalog.Catalogs.Add(typePartCatalog1);
            Assert.True(catalog.Catalogs.Count == 2);
            Assert.True(changedStep == 4);

            catalog.Catalogs.Add(typePartCatalog2);
            catalog.Catalogs.Add(typePartCatalog3);
            catalog.Catalogs.Add(typePartCatalog4);
            catalog.Catalogs.Add(typePartCatalog5);
            Assert.True(catalog.Catalogs.Count == 6);
            Assert.True(changedStep == 12);

            removed = catalog.Catalogs.Remove(typePartCatalog3);
            Assert.True(catalog.Catalogs.Count == 5);
            Assert.True(removed == true);
            Assert.True(changedStep == 14);
            removed = catalog.Catalogs.Remove(typePartCatalog2);
            removed = catalog.Catalogs.Remove(typePartCatalog1);
            removed = catalog.Catalogs.Remove(typePartCatalog4);
            removed = catalog.Catalogs.Remove(typePartCatalog);
            removed = catalog.Catalogs.Remove(typePartCatalog5);
            Assert.True(catalog.Catalogs.Count == 0);
            Assert.True(removed == true);
            Assert.True(changedStep == 24);

            // Add and then clear a lot
            step = changedStep = 0;
            catalog.Catalogs.Add(typePartCatalog);
            catalog.Catalogs.Add(typePartCatalog1);
            catalog.Catalogs.Add(typePartCatalog2);
            catalog.Catalogs.Add(typePartCatalog3);
            catalog.Catalogs.Add(typePartCatalog4);
            catalog.Catalogs.Add(typePartCatalog5);
            Assert.True(catalog.Catalogs.Count == 6);
            Assert.True(changedStep == 12);

            catalog.Catalogs.Clear();
            Assert.True(catalog.Catalogs.Count == 0);

            step = changedStep = 0;
            int step2 = 100;
            int changedStep2 = 0;

            catalog.Changed += delegate (object source, ComposablePartCatalogChangeEventArgs args)
            {
                // Local code
                --step2;
                --step2;
                changedStep2 = step2;
            };

            catalog.Catalogs.Add(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 1);
            Assert.True(changedStep == 2);
            Assert.True(changedStep2 == 98);

            catalog.Catalogs.Add(typePartCatalog1);
            Assert.True(catalog.Catalogs.Count == 2);
            Assert.True(changedStep == 4);
            Assert.True(changedStep2 == 96);

            catalog.Catalogs.Remove(typePartCatalog);
            Assert.True(catalog.Catalogs.Count == 1);
            Assert.True(changedStep == 6);
            Assert.True(changedStep2 == 94);

            catalog.Catalogs.Clear();
            Assert.True(catalog.Catalogs.Count == 0);
            Assert.True(changedStep == 8);
            Assert.True(changedStep2 == 92);

        }

        [Fact]
        public void DisposeAggregatingCatalog()
        {
            int changedNotification = 0;

            var typePartCatalog1 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog2 = new TypeCatalog(typeof(SharedPartStuff));
            var typePartCatalog3 = new TypeCatalog(typeof(SharedPartStuff));

            var assemblyPartCatalog1 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);
            var assemblyPartCatalog2 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);
            var assemblyPartCatalog3 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);

            var dirPartCatalog1 = new DirectoryCatalog(Path.GetTempPath());
            var dirPartCatalog2 = new DirectoryCatalog(Path.GetTempPath());
            var dirPartCatalog3 = new DirectoryCatalog(Path.GetTempPath());

            using (var catalog = new AggregateCatalog())
            {
                catalog.Catalogs.Add(typePartCatalog1);
                catalog.Catalogs.Add(typePartCatalog2);
                catalog.Catalogs.Add(typePartCatalog3);

                catalog.Catalogs.Add(assemblyPartCatalog1);
                catalog.Catalogs.Add(assemblyPartCatalog2);
                catalog.Catalogs.Add(assemblyPartCatalog3);

                catalog.Catalogs.Add(dirPartCatalog1);
                catalog.Catalogs.Add(dirPartCatalog2);
                catalog.Catalogs.Add(dirPartCatalog3);

                // Add notifications
                catalog.Changed += delegate (object source, ComposablePartCatalogChangeEventArgs args)
                {
                    // Local code
                    ++changedNotification;
                };

            }

            Assert.True(changedNotification == 0);

            //Ensure that the other catalogs are 
            ExceptionAssert.ThrowsDisposed(typePartCatalog1, () =>
            {
                var iEnum = typePartCatalog1.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(typePartCatalog2, () =>
            {
                var iEnum = typePartCatalog2.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(typePartCatalog3, () =>
            {
                var iEnum = typePartCatalog3.Parts.GetEnumerator();
            });

            //Ensure that the other catalogs are 
            ExceptionAssert.ThrowsDisposed(assemblyPartCatalog1, () =>
            {
                var iEnum = assemblyPartCatalog1.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(assemblyPartCatalog2, () =>
            {
                var iEnum = assemblyPartCatalog2.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(assemblyPartCatalog3, () =>
            {
                var iEnum = assemblyPartCatalog3.Parts.GetEnumerator();
            });

            //Ensure that the other catalogs are 
            ExceptionAssert.ThrowsDisposed(dirPartCatalog1, () =>
            {
                var iEnum = dirPartCatalog1.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(dirPartCatalog2, () =>
            {
                var iEnum = dirPartCatalog2.Parts.GetEnumerator();
            });

            ExceptionAssert.ThrowsDisposed(dirPartCatalog3, () =>
            {
                var iEnum = dirPartCatalog3.Parts.GetEnumerator();
            });
        }

        [Fact]
        [ActiveIssue(514749)]
        public void MutableMultithreadedEnumerations()
        {
            var catalog = new AggregateCatalog();

            ThreadStart func = delegate ()
            {
                var typePart = new TypeCatalog(typeof(SharedPartStuff));
                var typePart1 = new TypeCatalog(typeof(SharedPartStuff));
                var typePart2 = new TypeCatalog(typeof(SharedPartStuff));
                var typePart3 = new TypeCatalog(typeof(SharedPartStuff));
                var typePart4 = new TypeCatalog(typeof(SharedPartStuff));
                var typePart5 = new TypeCatalog(typeof(SharedPartStuff));

                for (int i = 0; i < 100; i++)
                {
                    catalog.Catalogs.Add(typePart);
                    catalog.Catalogs.Add(typePart1);
                    catalog.Catalogs.Add(typePart2);
                    catalog.Catalogs.Add(typePart3);
                    catalog.Catalogs.Add(typePart4);
                    catalog.Catalogs.Add(typePart5);

                    Assert.True(catalog.Catalogs.Count >= 6);

                    for (int k = 0; k < 5; k++)
                    {
                        int j;
                        // Ensure that iterating the returned queryable is okay even though there are many threads mutationg it
                        // We are really just looking to ensure that ollection changed exceptions are not thrown
                        j = 0;
                        var iq = catalog.Parts.GetEnumerator();
                        while (iq.MoveNext())
                        {
                            ++j;
                        }

                        Assert.True(j >= 6);

                        // Ensure that iterating the returned enumerator is okay even though there are many threads mutationg it
                        // We are really just looking to ensure that collection changed exceptions are not thrown
                        j = 0;
                        var ie = catalog.Catalogs.GetEnumerator();
                        while (ie.MoveNext())
                        {
                            ++j;
                        }
                        Assert.True(j >= 6);
                    }

                    catalog.Catalogs.Remove(typePart);
                    catalog.Catalogs.Remove(typePart1);
                    catalog.Catalogs.Remove(typePart2);
                    catalog.Catalogs.Remove(typePart3);
                    catalog.Catalogs.Remove(typePart4);
                    catalog.Catalogs.Remove(typePart5);
                }
            };

            Thread[] threads = new Thread[100];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(func);
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            Assert.True(catalog.Catalogs.Count == 0);
        }

        public void CreateMainAndOtherChildren(
                    out AggregateCatalog[] mainChildren,
                    out AggregateCatalog[] otherChildren,
                    out TypeCatalog[] componentCatalogs)
        {
            componentCatalogs = new TypeCatalog[]
            {
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff)),
                new TypeCatalog(typeof(SharedPartStuff))
            };

            // Create our child catalogs
            mainChildren = new AggregateCatalog[5];
            for (int i = 0; i < mainChildren.Length; i++)
            {
                mainChildren[i] = new AggregateCatalog(componentCatalogs);
            }

            otherChildren = new AggregateCatalog[5];
            for (int i = 0; i < otherChildren.Length; i++)
            {
                otherChildren[i] = new AggregateCatalog(componentCatalogs);
            }
        }

        [Fact]
        [ActiveIssue(812029)]
        public void AggregatingCatalogAddAndRemoveChildren()
        {
            int changedCount = 0;
            int typesChanged = 0;
            EventHandler<ComposablePartCatalogChangeEventArgs> onChanged = delegate (object sender, ComposablePartCatalogChangeEventArgs e)
            {
                ++changedCount;
                typesChanged += e.AddedDefinitions.Concat(e.RemovedDefinitions).Count();
            };

            // Create our child catalogs
            AggregateCatalog[] mainChildren;
            AggregateCatalog[] otherChildren;
            TypeCatalog[] componentCatalogs;

            CreateMainAndOtherChildren(out mainChildren, out otherChildren, out componentCatalogs);

            var parent = new AggregateCatalog(mainChildren);
            parent.Changed += onChanged;

            for (int i = 0; i < otherChildren.Length; i++)
            {
                parent.Catalogs.Add(otherChildren[i]);
            }

            Assert.Equal(otherChildren.Length, changedCount);
            Assert.Equal(otherChildren.Length * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            parent.Catalogs.Remove(otherChildren[0]);
            parent.Catalogs.Remove(otherChildren[1]);

            Assert.Equal(2, changedCount);
            Assert.Equal(2 * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            parent.Catalogs.Add(otherChildren[0]);
            parent.Catalogs.Add(otherChildren[1]);

            Assert.Equal(2, changedCount);
            Assert.Equal(2 * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            parent.Catalogs.Clear();
            Assert.Equal(1, changedCount);
            Assert.Equal((mainChildren.Length + otherChildren.Length) * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            // These have already been removed and so I should be able remove components from them without recieving notifications
            otherChildren[0].Catalogs.Remove(componentCatalogs[0]);
            otherChildren[1].Catalogs.Remove(componentCatalogs[1]);
            Assert.Equal(0, changedCount);
            Assert.Equal(0, typesChanged);

            // These have already been Cleared and so I should be able remove components from them without recieving notifications
            otherChildren[3].Catalogs.Remove(componentCatalogs[0]);
            otherChildren[4].Catalogs.Remove(componentCatalogs[1]);
            Assert.Equal(0, changedCount);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void AggregatingCatalogAddAndRemoveNestedChildren()
        {
            int changedCount = 0;
            int typesChanged = 0;

            EventHandler<ComposablePartCatalogChangeEventArgs> onChanged = delegate (object sender, ComposablePartCatalogChangeEventArgs e)
            {
                ++changedCount;
                typesChanged += e.AddedDefinitions.Concat(e.RemovedDefinitions).Count();
            };

            // Create our child catalogs
            AggregateCatalog[] mainChildren;
            AggregateCatalog[] otherChildren;
            TypeCatalog[] componentCatalogs;
            CreateMainAndOtherChildren(out mainChildren, out otherChildren, out componentCatalogs);

            var parent = new AggregateCatalog(mainChildren);
            parent.Changed += onChanged;

            for (int i = 0; i < otherChildren.Length; i++)
            {
                parent.Catalogs.Add(otherChildren[i]);
            }

            Assert.Equal(otherChildren.Length, changedCount);
            Assert.Equal(otherChildren.Length * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            otherChildren[0].Catalogs.Remove(componentCatalogs[0]);
            otherChildren[1].Catalogs.Remove(componentCatalogs[1]);

            Assert.Equal(2, changedCount);
            Assert.Equal(2, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            otherChildren[0].Catalogs.Add(componentCatalogs[0]);
            otherChildren[1].Catalogs.Add(componentCatalogs[1]);

            Assert.Equal(2, changedCount);
            Assert.Equal(2, typesChanged);

            changedCount = 0;
            typesChanged = 0;
            otherChildren[1].Catalogs.Clear();
            Assert.Equal(1, changedCount);
            Assert.Equal(componentCatalogs.Length, typesChanged);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void AggregatingDisposedAndNotifications()
        {
            int changedCount = 0;
            int typesChanged = 0;

            EventHandler<ComposablePartCatalogChangeEventArgs> onChanged = delegate (object sender, ComposablePartCatalogChangeEventArgs e)
            {
                ++changedCount;
                typesChanged += e.AddedDefinitions.Concat(e.RemovedDefinitions).Count();
            };

            // Create our child catalogs
            AggregateCatalog[] mainChildren;
            AggregateCatalog[] otherChildren;
            TypeCatalog[] componentCatalogs;
            CreateMainAndOtherChildren(out mainChildren, out otherChildren, out componentCatalogs);

            var parent = new AggregateCatalog(mainChildren);
            parent.Changed += onChanged;

            for (int i = 0; i < otherChildren.Length; i++)
            {
                parent.Catalogs.Add(otherChildren[i]);
            }

            Assert.Equal(otherChildren.Length, changedCount);
            Assert.Equal(otherChildren.Length * 3, typesChanged);

            changedCount = 0;
            typesChanged = 0;

            parent.Dispose();

            Assert.Equal(0, changedCount);
            Assert.Equal(0, typesChanged);

            //Ensure that the children are also disposed
            ExceptionAssert.ThrowsDisposed(otherChildren[0], () =>
            {
                otherChildren[0].Catalogs.Remove(componentCatalogs[0]);
            });

            //Ensure that the children are also disposed
            ExceptionAssert.ThrowsDisposed(otherChildren[4], () =>
            {
                otherChildren[4].Catalogs.Remove(componentCatalogs[0]);
            });

            Assert.Equal(0, changedCount);
            Assert.Equal(0, typesChanged);
        }

        [Fact]
        public void AggregatingCatalogParmsConstructorAggregateAggregateCatalogs()
        {
            var aggCatalog1 = new AggregateCatalog();
            var aggCatalog2 = new AggregateCatalog();
            var aggCatalog3 = new AggregateCatalog();

            // Construct with one catalog parameter
            var catalog = new AggregateCatalog(aggCatalog1);
            Assert.True(catalog.Catalogs.Count == 1);

            // Construct with two catalog parameters
            catalog = new AggregateCatalog(aggCatalog1, aggCatalog2);
            Assert.True(catalog.Catalogs.Count == 2);

            // Construct with three catalog parameters
            catalog = new AggregateCatalog(aggCatalog1, aggCatalog2, aggCatalog3);
            Assert.True(catalog.Catalogs.Count == 3);
        }

        [Fact]
        public void AggregatingCatalogParmsConstructorAggregateAssemblyCatalogs()
        {
            var assemblyCatalog1 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);
            var assemblyCatalog2 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);
            var assemblyCatalog3 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);

            // Construct with one catalog parameter
            var catalog = new AggregateCatalog(assemblyCatalog1);
            Assert.True(catalog.Catalogs.Count == 1);

            // Construct with two catalog parameters
            catalog = new AggregateCatalog(assemblyCatalog1, assemblyCatalog2);
            Assert.True(catalog.Catalogs.Count == 2);

            // Construct with three catalog parameters
            catalog = new AggregateCatalog(assemblyCatalog1, assemblyCatalog2, assemblyCatalog3);
            Assert.True(catalog.Catalogs.Count == 3);
        }

        [Fact]
        public void AggregatingCatalogParmsConstructorMixedCatalogs()
        {
            var typePartCatalog1 = new TypeCatalog(typeof(SharedPartStuff));
            var assemblyCatalog2 = new AssemblyCatalog(typeof(SharedPartStuff).Assembly);
            var typePartCatalog3 = new TypeCatalog(typeof(SharedPartStuff));

            // Construct with three catalog parameters
            var catalog = new AggregateCatalog(typePartCatalog1, assemblyCatalog2, typePartCatalog3);
            Assert.True(catalog.Catalogs.Count == 3);
        }

        [Fact]
        public void AggregatingCatalogRaisesChangesForCatalogsPassedToConstructor()
        {
            var subCatalog = CreateAggregateCatalog();
            var testCatalog = new AggregateCatalog(subCatalog);

            bool changedCalled = false;
            testCatalog.Changed += delegate
            {
                changedCalled = true;
            };

            subCatalog.Catalogs.Add(new TypeCatalog(typeof(SharedPartStuff)));

            Assert.True(changedCalled);
        }

        private AggregateCatalog CreateAggregateCatalog()
        {
            return new AggregateCatalog();
        }

        [Fact]
        [ActiveIssue(812029)]
        public void CatalogEvents_AggregateAddRemove()
        {
            var catalog = new AggregateCatalog();
            AggregateTests(catalog, catalog);
        }

        [Fact]
        [ActiveIssue(812029)]
        public void CatalogEvents_DeepAggregateAddRemove()
        {
            var deepCatalog = new AggregateCatalog();
            var midCatalog = new AggregateCatalog(new ComposablePartCatalog[] { deepCatalog });
            var topCatalog = new AggregateCatalog(new ComposablePartCatalog[] { midCatalog });
            AggregateTests(topCatalog, deepCatalog);
        }

        private void AggregateTests(AggregateCatalog watchedCatalog, AggregateCatalog modifiedCatalog)
        {
            var fooCatalog = new TypeCatalog(new Type[] { typeof(FooExporter) });
            var barCatalog = new TypeCatalog(new Type[] { typeof(BarExporter) });
            var bothCatalog = new TypeCatalog(new Type[] { typeof(FooExporter), typeof(BarExporter) });

            var catalogListener = new CatalogListener(watchedCatalog, modifiedCatalog);

            catalogListener.VerifyAdd(fooCatalog, typeof(FooExporter));
            catalogListener.VerifyAdd(barCatalog, typeof(BarExporter));
            catalogListener.VerifyRemove(fooCatalog, typeof(FooExporter));
            catalogListener.VerifyRemove(barCatalog, typeof(BarExporter));

            catalogListener.VerifyAdd(bothCatalog, typeof(FooExporter), typeof(BarExporter));
            catalogListener.VerifyClear(typeof(FooExporter), typeof(BarExporter));

            catalogListener.VerifyAdd(bothCatalog, typeof(FooExporter), typeof(BarExporter));
            catalogListener.VerifyRemove(bothCatalog, typeof(FooExporter), typeof(BarExporter));
        }

        public interface IFoo { }
        public interface IBar { }

        [Export(typeof(IFoo))]
        public class FooExporter : IFoo
        {
        }

        [Export(typeof(IBar))]
        public class BarExporter : IBar
        {
        }

        public class CatalogListener
        {
            private AggregateCatalog _watchedCatalog;
            private AggregateCatalog _modifiedCatalog;
            private string[] _expectedAdds;
            private string[] _expectedRemoves;
            private int _changedEventCount;
            private int _changingEventCount;

            public CatalogListener(AggregateCatalog watchCatalog, AggregateCatalog modifiedCatalog)
            {
                watchCatalog.Changing += OnChanging;
                watchCatalog.Changed += OnChanged;
                this._watchedCatalog = watchCatalog;
                this._modifiedCatalog = modifiedCatalog;
            }

            public void VerifyAdd(ComposablePartCatalog catalogToAdd, params Type[] expectedTypesAdded)
            {
                this._expectedAdds = GetDisplayNames(expectedTypesAdded);

                this._modifiedCatalog.Catalogs.Add(catalogToAdd);

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void VerifyRemove(ComposablePartCatalog catalogToRemove, params Type[] expectedTypesRemoved)
            {
                this._expectedAdds = null;
                this._expectedRemoves = GetDisplayNames(expectedTypesRemoved);

                this._modifiedCatalog.Catalogs.Remove(catalogToRemove);

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void VerifyClear(params Type[] expectedTypesRemoved)
            {
                this._expectedAdds = null;
                this._expectedRemoves = GetDisplayNames(expectedTypesRemoved);

                this._modifiedCatalog.Catalogs.Clear();

                Assert.True(this._changingEventCount == 1);
                Assert.True(this._changedEventCount == 1);

                ResetState();
            }

            public void OnChanging(object sender, ComposablePartCatalogChangeEventArgs args)
            {
                Assert.True(this._expectedAdds != null || this._expectedRemoves != null);

                if (this._expectedAdds == null)
                {
                    Assert.Empty(args.AddedDefinitions);
                }
                else
                {
                    EqualityExtensions.CheckSequenceEquals(this._expectedAdds, GetDisplayNames(args.AddedDefinitions));
                }

                if (this._expectedRemoves == null)
                {
                    Assert.Empty(args.RemovedDefinitions);
                }
                else
                {
                    EqualityExtensions.CheckSequenceEquals(this._expectedRemoves, GetDisplayNames(args.RemovedDefinitions));
                }

                Assert.False(ContainsChanges(), "The catalog should NOT contain the changes yet");

                this._changingEventCount++;
            }

            public void OnChanged(object sender, ComposablePartCatalogChangeEventArgs args)
            {
                Assert.True(this._expectedAdds != null || this._expectedRemoves != null);

                if (this._expectedAdds == null)
                {
                    Assert.Empty(args.AddedDefinitions);
                }
                else
                {
                    EqualityExtensions.CheckSequenceEquals(this._expectedAdds, GetDisplayNames(args.AddedDefinitions));
                }

                if (this._expectedRemoves == null)
                {
                    Assert.Empty(args.RemovedDefinitions);
                }
                else
                {
                    EqualityExtensions.CheckSequenceEquals(this._expectedRemoves, GetDisplayNames(args.RemovedDefinitions));
                }

                Assert.Null(args.AtomicComposition);
                Assert.True(ContainsChanges());

                this._changedEventCount++;
            }

            private bool ContainsChanges()
            {
                var allParts = GetDisplayNames(this._watchedCatalog.Parts);

                if (this._expectedAdds != null)
                {
                    foreach (var add in this._expectedAdds)
                    {
                        if (!allParts.Contains(add))
                        {
                            return false;
                        }
                    }
                }

                if (this._expectedRemoves != null)
                {
                    foreach (var remove in this._expectedRemoves)
                    {
                        if (allParts.Contains(remove))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            private void ResetState()
            {
                this._expectedAdds = null;
                this._expectedRemoves = null;
                this._changedEventCount = 0;
                this._changingEventCount = 0;
            }

            private static string[] GetDisplayNames(IEnumerable<ComposablePartDefinition> definitions)
            {
                return definitions.OfType<ICompositionElement>().Select(p => p.DisplayName).ToArray();
            }

            private static string[] GetDisplayNames(IEnumerable<Type> types)
            {
                return GetDisplayNames(types.Select(t => AttributedModelServices.CreatePartDefinition(t, null)));
            }
        }

        [Export]
        [PartCreationPolicy(CreationPolicy.Shared)]
        public class SharedPartStuff
        {
            Guid id = Guid.NewGuid();

            public override string ToString()
            {
                return id.ToString();
            }
        }
    }
}
