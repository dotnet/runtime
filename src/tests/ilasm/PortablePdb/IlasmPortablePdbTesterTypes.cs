using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace IlasmPortablePdbTests
{
    public class DocumentStub
    {
        public string Name { get; set; }
        public Guid HashAlgorithm { get; set; }
        public byte[] Hash { get; set; }
        public Guid Language { get; set; }
        public DocumentStub(string name)
        {
            Name = name;
        }
    }

    public class SequencePointStub
    {
        public DocumentStub Document { get; set; }
        public int EndColumn { get; set; }
        public int EndLine { get; set; }
        public bool IsHidden { get; set; }
        public int Offset { get; set; }
        public int StartColumn { get; set; }
        public int StartLine { get; set; }

        public SequencePointStub(DocumentStub document,
            bool isHidden = true,
            int offset = 0,
            int startLine = SequencePoint.HiddenLine,
            int endLine = SequencePoint.HiddenLine,
            int startCol = 0,
            int endCol = 0)
        {
            Document = document;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startCol;
            EndColumn = endCol;
            IsHidden = isHidden;
            Offset = offset;
        }
    }

    public class MethodDebugInformationStub
    {
        public string Name { get; set; }
        public DocumentStub Document { get; set; }
        public List<SequencePointStub> SequencePoints { get; set; }
        public MethodDebugInformationStub(string name, DocumentStub document, List<SequencePointStub> sequencePoints = null)
        {
            Name = name;
            Document = document;
            SequencePoints = sequencePoints ?? new List<SequencePointStub>();
        }
    }

    public class VariableStub
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public bool IsDebuggerHidden { get; set; }
        public VariableStub(string name, int index, bool isDebuggerHidden = false)
        {
            Name = name;
            Index = index;
            IsDebuggerHidden = isDebuggerHidden;
        }
    }

    public class LocalScopeStub
    {
        public string MethodName { get; set; }
        public int StartOffset { get; set;  }
        public int EndOffset { get; set; }
        public int Length { get; set; }
        public List<VariableStub> Variables { get; set; }
        public LocalScopeStub(string methodName, int startOffset, int endOffset, int length, List<VariableStub> variables = null)
        {
            MethodName = methodName;
            StartOffset = startOffset;
            EndOffset = endOffset;
            Length = length;
            Variables = variables ?? new List<VariableStub>();
        }
    }
}
