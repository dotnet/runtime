// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Melanzana.MachO
{
    public class MachDyldInfo : MachLoadCommand
    {
        public MachDyldInfo(MachObjectFile objectFile)
        {
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
