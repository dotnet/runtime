// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.DirectoryServices
{
    public class DirectoryVirtualListView
    {
        private int _beforeCount = 0;
        private int _afterCount = 0;
        private int _offset = 0;
        private string _target = "";
        private int _approximateTotal = 0;
        private int _targetPercentage = 0;

        public DirectoryVirtualListView()
        {
        }

        public DirectoryVirtualListView(int afterCount)
        {
            AfterCount = afterCount;
        }

        public DirectoryVirtualListView(int beforeCount, int afterCount, int offset)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Offset = offset;
        }

        public DirectoryVirtualListView(int beforeCount, int afterCount, string target)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Target = target;
        }

        public DirectoryVirtualListView(int beforeCount, int afterCount, int offset, DirectoryVirtualListViewContext context)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Offset = offset;
            DirectoryVirtualListViewContext = context;
        }

        public DirectoryVirtualListView(int beforeCount, int afterCount, string target, DirectoryVirtualListViewContext context)
        {
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            Target = target;
            DirectoryVirtualListViewContext = context;
        }

        [DefaultValue(0)]
        public int BeforeCount
        {
            get => _beforeCount;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.DSBadBeforeCount);
                }

                _beforeCount = value;
            }
        }

        [DefaultValue(0)]
        public int AfterCount
        {
            get => _afterCount;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.DSBadAfterCount);
                }

                _afterCount = value;
            }
        }

        [DefaultValue(0)]
        public int Offset
        {
            get => _offset;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.DSBadOffset);
                }

                _offset = value;
                if (_approximateTotal != 0)
                {
                    _targetPercentage = (int)((double)_offset / _approximateTotal * 100);
                }
                else
                {
                    _targetPercentage = 0;
                }
            }
        }

        [DefaultValue(0)]
        public int TargetPercentage
        {
            get => _targetPercentage;
            set
            {
                if (value > 100 || value < 0)
                {
                    throw new ArgumentException(SR.DSBadTargetPercentage);
                }

                _targetPercentage = value;
                _offset = _approximateTotal * _targetPercentage / 100;
            }
        }

        public string Target
        {
            get => _target;
            set => _target = value ?? string.Empty;
        }

        [DefaultValue(0)]
        public int ApproximateTotal
        {
            get => _approximateTotal;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(SR.DSBadApproximateTotal);
                }

                _approximateTotal = value;
            }
        }

        [DefaultValue(null)]
        public DirectoryVirtualListViewContext DirectoryVirtualListViewContext { get; set; }
    }
}
