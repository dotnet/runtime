// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);

    public class ProgressChangedEventArgs : EventArgs
    {
        private readonly int _progressPercentage;
        private readonly object _userState;

        public ProgressChangedEventArgs(int progressPercentage, object userState)
        {
            _progressPercentage = progressPercentage;
            _userState = userState;
        }

        public int ProgressPercentage
        {
            get
            {
                return _progressPercentage;
            }
        }

        public object UserState
        {
            get
            {
                return _userState; 
            }
        }
    }
}
