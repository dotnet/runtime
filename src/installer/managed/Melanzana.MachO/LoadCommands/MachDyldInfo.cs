// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public class MachDyldInfo : MachLoadCommand
    {
        public MachDyldInfo(MachObjectFile objectFile)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));

            RebaseData = new MachLinkEditData();
            BindData = new MachLinkEditData();
            WeakBindData = new MachLinkEditData();
            LazyBindData = new MachLinkEditData();
            ExportData = new MachLinkEditData();
        }

        public MachDyldInfo(
            MachObjectFile objectFile,
            MachLinkEditData rebaseData,
            MachLinkEditData bindData,
            MachLinkEditData weakBindData,
            MachLinkEditData lazyBindData,
            MachLinkEditData exportData)
        {
            if (objectFile is null) throw new ArgumentNullException(nameof(objectFile));
            if (rebaseData is null) throw new ArgumentNullException(nameof(rebaseData));
            if (bindData is null) throw new ArgumentNullException(nameof(bindData));
            if (weakBindData is null) throw new ArgumentNullException(nameof(weakBindData));
            if (lazyBindData is null) throw new ArgumentNullException(nameof(lazyBindData));
            if (exportData is null) throw new ArgumentNullException(nameof(exportData));

            RebaseData = rebaseData;
            BindData = bindData;
            WeakBindData = weakBindData;
            LazyBindData = lazyBindData;
            ExportData = exportData;
        }

        public MachLinkEditData RebaseData { get; private init; }
        public MachLinkEditData BindData { get; private init; }
        public MachLinkEditData WeakBindData { get; private init; }
        public MachLinkEditData LazyBindData { get; private init; }
        public MachLinkEditData ExportData { get; private init; }

        internal override IEnumerable<MachLinkEditData> LinkEditData
        {
            get
            {
                yield return RebaseData;
                yield return BindData;
                yield return WeakBindData;
                yield return LazyBindData;
                yield return ExportData;
            }
        }
    }
}
