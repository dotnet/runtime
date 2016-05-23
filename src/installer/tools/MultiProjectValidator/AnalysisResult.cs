using System.Collections.Generic;

namespace MultiProjectValidator
{
    public class AnalysisResult
    {

        private IEnumerable<string> _messages;
        private bool _passed;

        public AnalysisResult(IEnumerable<string> messages, bool passed)
        {
            _messages = messages;
            _passed = passed;
        }

        public IEnumerable<string> Messages
        {
            get
            {
                return _messages;
            }
        }

        public bool Passed
        {
            get
            {
                return _passed;
            }
        }
    }
}
