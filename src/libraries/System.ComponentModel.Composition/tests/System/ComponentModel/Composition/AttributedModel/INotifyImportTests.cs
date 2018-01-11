// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition.AttributedModel
{
    public class INotifyImportTests
    {
        [Export(typeof(PartWithoutImports))]
        public class PartWithoutImports : IPartImportsSatisfiedNotification
        {
            public bool ImportsSatisfiedInvoked { get; private set; }
            public void OnImportsSatisfied()
            {
                this.ImportsSatisfiedInvoked = true;
            }
        }

        [Fact]
        public void ImportsSatisfiedOnComponentWithoutImports()
        {
            CompositionContainer container = ContainerFactory.CreateWithAttributedCatalog(typeof(PartWithoutImports));

            PartWithoutImports partWithoutImports = container.GetExportedValue<PartWithoutImports>();
            Assert.NotNull(partWithoutImports);

            Assert.True(partWithoutImports.ImportsSatisfiedInvoked);

        }

        [Fact]
        public void ImportCompletedTest()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            var entrypoint = new UpperCaseStringComponent();

            batch.AddParts(new LowerCaseString("abc"), entrypoint);
            container.Compose(batch);

            batch = new CompositionBatch();
            batch.AddParts(new object());
            container.Compose(batch);

            Assert.Equal(entrypoint.LowerCaseStrings.Count, 1);
            Assert.Equal(entrypoint.ImportCompletedCallCount, 1);
            Assert.Equal(entrypoint.UpperCaseStrings.Count, 1);
            Assert.Equal(entrypoint.LowerCaseStrings[0].Value.String, "abc");
            Assert.Equal(entrypoint.UpperCaseStrings[0], "ABC");
        }

        [Fact]
        public void ImportCompletedWithRecomposing()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            var entrypoint = new UpperCaseStringComponent();

            batch.AddParts(new LowerCaseString("abc"), entrypoint);
            container.Compose(batch);

            Assert.Equal(entrypoint.LowerCaseStrings.Count, 1);
            Assert.Equal(entrypoint.ImportCompletedCallCount, 1);
            Assert.Equal(entrypoint.UpperCaseStrings.Count, 1);
            Assert.Equal(entrypoint.LowerCaseStrings[0].Value.String, "abc");
            Assert.Equal(entrypoint.UpperCaseStrings[0], "ABC");

            // Add another component to verify recomposing
            batch = new CompositionBatch();
            batch.AddParts(new LowerCaseString("def"));
            container.Compose(batch);

            Assert.Equal(entrypoint.LowerCaseStrings.Count, 2);
            Assert.Equal(entrypoint.ImportCompletedCallCount, 2);
            Assert.Equal(entrypoint.UpperCaseStrings.Count, 2);
            Assert.Equal(entrypoint.LowerCaseStrings[1].Value.String, "def");
            Assert.Equal(entrypoint.UpperCaseStrings[1], "DEF");

            // Verify that adding a random component doesn't cause
            // the OnImportsSatisfied to be called again.
            batch = new CompositionBatch();
            batch.AddParts(new object());
            container.Compose(batch);

            Assert.Equal(entrypoint.LowerCaseStrings.Count, 2);
            Assert.Equal(entrypoint.ImportCompletedCallCount, 2);
            Assert.Equal(entrypoint.UpperCaseStrings.Count, 2);
        }

        [Fact]
        [ActiveIssue(700940)]
        public void ImportCompletedUsingSatisfyImportsOnce()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            var entrypoint = new UpperCaseStringComponent();
            var entrypointPart = AttributedModelServices.CreatePart(entrypoint);

            batch.AddParts(new LowerCaseString("abc"));
            container.Compose(batch);
            container.SatisfyImportsOnce(entrypointPart);

            Assert.Equal(1, entrypoint.LowerCaseStrings.Count);
            Assert.Equal(1, entrypoint.ImportCompletedCallCount);
            Assert.Equal(1, entrypoint.UpperCaseStrings.Count);
            Assert.Equal("abc", entrypoint.LowerCaseStrings[0].Value.String);
            Assert.Equal("ABC", entrypoint.UpperCaseStrings[0]);

            batch = new CompositionBatch();
            batch.AddParts(new object());
            container.Compose(batch);
            container.SatisfyImportsOnce(entrypointPart);

            Assert.Equal(1, entrypoint.LowerCaseStrings.Count);
            Assert.Equal(1, entrypoint.ImportCompletedCallCount);
            Assert.Equal(1, entrypoint.UpperCaseStrings.Count);
            Assert.Equal("abc", entrypoint.LowerCaseStrings[0].Value.String);
            Assert.Equal("ABC", entrypoint.UpperCaseStrings[0]);

            batch.AddParts(new LowerCaseString("def"));
            container.Compose(batch);
            container.SatisfyImportsOnce(entrypointPart);

            Assert.Equal(2, entrypoint.LowerCaseStrings.Count);
            Assert.Equal(2, entrypoint.ImportCompletedCallCount);
            Assert.Equal(2, entrypoint.UpperCaseStrings.Count);
            Assert.Equal("abc", entrypoint.LowerCaseStrings[0].Value.String);
            Assert.Equal("ABC", entrypoint.UpperCaseStrings[0]);
            Assert.Equal("def", entrypoint.LowerCaseStrings[1].Value.String);
            Assert.Equal("DEF", entrypoint.UpperCaseStrings[1]);
        }

        [Fact]
        [ActiveIssue(654513)]
        public void ImportCompletedCalledAfterAllImportsAreFullyComposed()
        {
            int importSatisfationCount = 0;
            var importer1 = new MyEventDrivenFullComposedNotifyImporter1();
            var importer2 = new MyEventDrivenFullComposedNotifyImporter2();

            Action<object, EventArgs> verificationAction = (object sender, EventArgs e) =>
            {
                Assert.True(importer1.AreAllImportsFullyComposed);
                Assert.True(importer2.AreAllImportsFullyComposed);
                ++importSatisfationCount;
            };

            importer1.ImportsSatisfied += new EventHandler(verificationAction);
            importer2.ImportsSatisfied += new EventHandler(verificationAction);

            // importer1 added first
            var batch = new CompositionBatch();
            batch.AddParts(importer1, importer2);

            var container = ContainerFactory.Create();
            container.ComposeExportedValue<ICompositionService>(container);
            container.Compose(batch);
            Assert.Equal(2, importSatisfationCount);

            // importer2 added first
            importSatisfationCount = 0;
            batch = new CompositionBatch();
            batch.AddParts(importer2, importer1);

            container = ContainerFactory.Create();
            container.ComposeExportedValue<ICompositionService>(container);
            container.Compose(batch);
            Assert.Equal(2, importSatisfationCount);
        }

        [Fact]
        public void ImportCompletedAddPartAndBindComponent()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();
            batch.AddParts(new CallbackImportNotify(delegate
            {
                batch = new CompositionBatch();
                batch.AddPart(new object());
                container.Compose(batch);
            }));

            container.Compose(batch);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCompletedChildNeedsParentContainer()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var parent = new CompositionContainer(cat);

            CompositionBatch parentBatch = new CompositionBatch();
            CompositionBatch childBatch = new CompositionBatch();
            CompositionBatch child2Batch = new CompositionBatch();

            parentBatch.AddExportedValue<ICompositionService>(parent);
            parent.Compose(parentBatch);
            var child = new CompositionContainer(parent);
            var child2 = new CompositionContainer(parent);

            var parentImporter = new MyNotifyImportImporter(parent);
            var childImporter = new MyNotifyImportImporter(child);
            var child2Importer = new MyNotifyImportImporter(child2);

            parentBatch = new CompositionBatch();
            parentBatch.AddPart(parentImporter);
            childBatch.AddPart(childImporter);
            child2Batch.AddPart(child2Importer);

            parent.Compose(parentBatch);
            child.Compose(childBatch);
            child2.Compose(child2Batch);

            Assert.Equal(1, parentImporter.ImportCompletedCallCount);
            Assert.Equal(1, childImporter.ImportCompletedCallCount);
            Assert.Equal(1, child2Importer.ImportCompletedCallCount);

            MyNotifyImportExporter parentExporter = parent.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, parentExporter.ImportCompletedCallCount);

            MyNotifyImportExporter childExporter = child.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, childExporter.ImportCompletedCallCount);

            MyNotifyImportExporter child2Exporter = child2.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, child2Exporter.ImportCompletedCallCount);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCompletedChildDoesnotNeedParentContainer()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var parent = new CompositionContainer(cat);

            CompositionBatch parentBatch = new CompositionBatch();
            CompositionBatch childBatch = new CompositionBatch();

            parentBatch.AddExportedValue<ICompositionService>(parent);
            parent.Compose(parentBatch);

            var child = new CompositionContainer(parent);

            var parentImporter = new MyNotifyImportImporter(parent);
            var childImporter = new MyNotifyImportImporter(child);

            parentBatch = new CompositionBatch();
            parentBatch.AddPart(parentImporter);

            childBatch.AddParts(childImporter, new MyNotifyImportExporter());

            child.Compose(childBatch);

            Assert.Equal(0, parentImporter.ImportCompletedCallCount);
            Assert.Equal(1, childImporter.ImportCompletedCallCount);

            // Parent will become bound at this point.
            MyNotifyImportExporter parentExporter = parent.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            parent.Compose(parentBatch);
            Assert.Equal(1, parentImporter.ImportCompletedCallCount);
            Assert.Equal(1, parentExporter.ImportCompletedCallCount);

            MyNotifyImportExporter childExporter = child.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, childExporter.ImportCompletedCallCount);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCompletedBindChildIndirectlyThroughParentContainerBind()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            var parent = new CompositionContainer(cat);

            CompositionBatch parentBatch = new CompositionBatch();
            CompositionBatch childBatch = new CompositionBatch();

            parentBatch.AddExportedValue<ICompositionService>(parent);
            parent.Compose(parentBatch);
            var child = new CompositionContainer(parent);

            var parentImporter = new MyNotifyImportImporter(parent);
            var childImporter = new MyNotifyImportImporter(child);

            parentBatch = new CompositionBatch();
            parentBatch.AddPart(parentImporter);
            childBatch.AddParts(childImporter, new MyNotifyImportExporter());

            parent.Compose(parentBatch);
            child.Compose(childBatch);

            Assert.Equal(1, parentImporter.ImportCompletedCallCount);
            Assert.Equal(1, childImporter.ImportCompletedCallCount);

            MyNotifyImportExporter parentExporter = parent.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, parentExporter.ImportCompletedCallCount);

            MyNotifyImportExporter childExporter = child.GetExportedValue<MyNotifyImportExporter>("MyNotifyImportExporter");
            Assert.Equal(1, childExporter.ImportCompletedCallCount);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCompletedGetExportedValueLazy()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            CompositionContainer container = new CompositionContainer(cat);

            NotifyImportExportee.InstanceCount = 0;
            NotifyImportExportsLazy notifyee = container.GetExportedValue<NotifyImportExportsLazy>("NotifyImportExportsLazy");
            Assert.NotNull(notifyee);
            Assert.NotNull(notifyee.Imports);
            Assert.True(notifyee.NeedRefresh);
            Assert.Equal(3, notifyee.Imports.Count);
            Assert.Equal(0, NotifyImportExportee.InstanceCount);
            Assert.Equal(0, notifyee.realImports.Count);
            Assert.Equal(2, notifyee.RealImports.Count);
            Assert.Equal(1, notifyee.RealImports[0].Id);
            Assert.Equal(3, notifyee.RealImports[1].Id);
            Assert.Equal(2, NotifyImportExportee.InstanceCount);
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void ImportCompletedGetExportedValueEager()
        {
            var cat = CatalogFactory.CreateDefaultAttributed();
            CompositionContainer container = new CompositionContainer(cat);

            NotifyImportExportee.InstanceCount = 0;
            var notifyee = container.GetExportedValue<NotifyImportExportsEager>("NotifyImportExportsEager");
            Assert.NotNull(notifyee);
            Assert.NotNull(notifyee.Imports);
            Assert.Equal(3, notifyee.Imports.Count);
            Assert.Equal(2, NotifyImportExportee.InstanceCount);
            Assert.Equal(2, notifyee.realImports.Count);
            Assert.Equal(2, notifyee.RealImports.Count);
            Assert.Equal(1, notifyee.RealImports[0].Id);
            Assert.Equal(3, notifyee.RealImports[1].Id);
            Assert.Equal(2, NotifyImportExportee.InstanceCount);
        }
    }

    public class NotifyImportExportee
    {
        public NotifyImportExportee(int id)
        {
            Id = id;
            InstanceCount++;
        }

        public int Id { get; set; }

        public static int InstanceCount { get; set; }
    }

    public class NotifyImportExporter
    {

        public NotifyImportExporter()
        {
        }

        [Export()]
        [ExportMetadata("Filter", false)]
        public NotifyImportExportee Export1
        {
            get
            {
                return new NotifyImportExportee(1);
            }
        }

        [Export()]
        [ExportMetadata("Filter", true)]
        public NotifyImportExportee Export2
        {
            get
            {
                return new NotifyImportExportee(2);
            }
        }

        [Export()]
        [ExportMetadata("Filter", false)]
        public NotifyImportExportee Export3
        {
            get
            {
                return new NotifyImportExportee(3);
            }
        }

    }

    [Export("NotifyImportExportsLazy")]
    public class NotifyImportExportsLazy : IPartImportsSatisfiedNotification
    {
        public NotifyImportExportsLazy()
        {
            NeedRefresh = false;
        }

        [ImportMany(typeof(NotifyImportExportee))]
        public Collection<Lazy<NotifyImportExportee, IDictionary<string, object>>> Imports { get; set; }

        public bool NeedRefresh { get; set; }

        public void OnImportsSatisfied()
        {
            NeedRefresh = true;
        }

        internal Collection<NotifyImportExportee> realImports = new Collection<NotifyImportExportee>();

        public Collection<NotifyImportExportee> RealImports
        {
            get
            {
                if (NeedRefresh)
                {
                    realImports.Clear();
                    foreach (var import in Imports)
                    {
                        if (!((bool)import.Metadata["Filter"]))
                        {
                            realImports.Add(import.Value);
                        }
                    }
                    NeedRefresh = false;
                }
                return realImports;
            }
        }
    }

    [Export("NotifyImportExportsEager")]
    public class NotifyImportExportsEager : IPartImportsSatisfiedNotification
    {
        public NotifyImportExportsEager()
        {
        }

        [ImportMany]
        public Collection<Lazy<NotifyImportExportee, IDictionary<string, object>>> Imports { get; set; }

        public void OnImportsSatisfied()
        {
            realImports.Clear();
            foreach (var import in Imports)
            {
                if (!((bool)import.Metadata["Filter"]))
                {
                    realImports.Add(import.Value);
                }
            }
        }

        internal Collection<NotifyImportExportee> realImports = new Collection<NotifyImportExportee>();

        public Collection<NotifyImportExportee> RealImports
        {
            get
            {
                return realImports;
            }
        }
    }

    public class MyEventDrivenNotifyImporter : IPartImportsSatisfiedNotification
    {
        [Import]
        public ICompositionService ImportSomethingSoIGetImportCompletedCalled { get; set; }

        public event EventHandler ImportsSatisfied;

        public void OnImportsSatisfied()
        {
            if (this.ImportsSatisfied != null)
            {
                this.ImportsSatisfied(this, new EventArgs());
            }
        }
    }

    [Export]
    public class MyEventDrivenFullComposedNotifyImporter1 : MyEventDrivenNotifyImporter
    {
        [Import]
        public MyEventDrivenFullComposedNotifyImporter2 FullyComposedImport { get; set; }

        public bool AreAllImportsSet
        {
            get
            {
                return (this.ImportSomethingSoIGetImportCompletedCalled != null)
                    && (this.FullyComposedImport != null);
            }
        }

        public bool AreAllImportsFullyComposed
        {
            get
            {
                return this.AreAllImportsSet && this.FullyComposedImport.AreAllImportsSet;
            }
        }
    }

    [Export]
    public class MyEventDrivenFullComposedNotifyImporter2 : MyEventDrivenNotifyImporter
    {
        [Import]
        public MyEventDrivenFullComposedNotifyImporter1 FullyComposedImport { get; set; }

        public bool AreAllImportsSet
        {
            get
            {
                return (this.ImportSomethingSoIGetImportCompletedCalled != null)
                    && (this.FullyComposedImport != null);
            }
        }

        public bool AreAllImportsFullyComposed
        {
            get
            {
                return this.AreAllImportsSet && this.FullyComposedImport.AreAllImportsSet;
            }
        }
    }

    [Export("MyNotifyImportExporter")]
    public class MyNotifyImportExporter : IPartImportsSatisfiedNotification
    {
        [Import]
        public ICompositionService ImportSomethingSoIGetImportCompletedCalled { get; set; }

        public int ImportCompletedCallCount { get; set; }
        public void OnImportsSatisfied()
        {
            ImportCompletedCallCount++;
        }
    }

    public class MyNotifyImportImporter : IPartImportsSatisfiedNotification
    {
        private CompositionContainer container;
        public MyNotifyImportImporter(CompositionContainer container)
        {
            this.container = container;
        }

        [Import("MyNotifyImportExporter")]
        public MyNotifyImportExporter MyNotifyImportExporter { get; set; }

        public int ImportCompletedCallCount { get; set; }
        public void OnImportsSatisfied()
        {
            ImportCompletedCallCount++;
        }
    }

    [Export("LowerCaseString")]
    public class LowerCaseString
    {
        public string String { get; private set; }
        public LowerCaseString(string s)
        {
            String = s.ToLower();
        }
    }

    public class UpperCaseStringComponent : IPartImportsSatisfiedNotification
    {
        public UpperCaseStringComponent()
        {
            UpperCaseStrings = new List<string>();
        }
        Collection<Lazy<LowerCaseString>> lowerCaseString = new Collection<Lazy<LowerCaseString>>();

        [ImportMany("LowerCaseString", AllowRecomposition = true)]
        public Collection<Lazy<LowerCaseString>> LowerCaseStrings
        {
            get { return lowerCaseString; }
            set { lowerCaseString = value; }
        }

        public List<string> UpperCaseStrings { get; set; }

        public int ImportCompletedCallCount { get; set; }

        // This method gets called whenever a bind is completed and any of 
        // of the imports have changed, but ar safe to use now.
        public void OnImportsSatisfied()
        {
            UpperCaseStrings.Clear();
            foreach (var i in LowerCaseStrings)
                UpperCaseStrings.Add(i.Value.String.ToUpper());

            ImportCompletedCallCount++;
        }
    }
}
