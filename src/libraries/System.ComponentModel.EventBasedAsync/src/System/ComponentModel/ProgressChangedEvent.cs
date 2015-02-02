// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);

    public class ProgressChangedEventArgs : EventArgs
    {
        private readonly int progressPercentage;
        private readonly object userState;

        public ProgressChangedEventArgs(int progressPercentage, object userState)
        {
            this.progressPercentage = progressPercentage;
            this.userState = userState;
        }

        public int ProgressPercentage
        {
            get { return progressPercentage; }
        }

        public object UserState
        {
            get { return userState; }
        }
    }
}

