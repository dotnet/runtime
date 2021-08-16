// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal;

namespace System.Speech.Recognition
{
    public class SpeechUI
    {
        internal SpeechUI()
        {
        }
        public static bool SendTextFeedback(RecognitionResult result, string feedback, bool isSuccessfulAction)
        {
            Helpers.ThrowIfNull(result, nameof(result));
            Helpers.ThrowIfEmptyOrNull(feedback, nameof(feedback));

            return result.SetTextFeedback(feedback, isSuccessfulAction);
        }
    }
}
