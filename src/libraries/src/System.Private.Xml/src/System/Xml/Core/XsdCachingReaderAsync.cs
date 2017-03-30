// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;
using System.Collections;

using System.Threading.Tasks;

namespace System.Xml
{
    internal partial class XsdCachingReader : XmlReader, IXmlLineInfo
    {
        // Gets the text value of the current node.
        public override Task<string> GetValueAsync()
        {
            if (_returnOriginalStringValues)
            {
                return Task.FromResult(_cachedNode.OriginalStringValue);
            }
            else
            {
                return Task.FromResult(_cachedNode.RawValue);
            }
        }

        // Reads the next node from the stream/TextReader.
        public override async Task<bool> ReadAsync()
        {
            switch (_cacheState)
            {
                case CachingReaderState.Init:
                    _cacheState = CachingReaderState.Record;
                    goto case CachingReaderState.Record;

                case CachingReaderState.Record:
                    ValidatingReaderNodeData recordedNode = null;
                    if (await _coreReader.ReadAsync().ConfigureAwait(false))
                    {
                        switch (_coreReader.NodeType)
                        {
                            case XmlNodeType.Element:
                                //Dont record element within the content of a union type since the main reader will break on this and the underlying coreReader will be positioned on this node
                                _cacheState = CachingReaderState.ReaderClosed;
                                return false;

                            case XmlNodeType.EndElement:
                                recordedNode = AddContent(_coreReader.NodeType);
                                recordedNode.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);  //Only created for element node type
                                recordedNode.SetLineInfo(_lineInfo);
                                break;

                            case XmlNodeType.Comment:
                            case XmlNodeType.ProcessingInstruction:
                            case XmlNodeType.Text:
                            case XmlNodeType.CDATA:
                            case XmlNodeType.Whitespace:
                            case XmlNodeType.SignificantWhitespace:
                                recordedNode = AddContent(_coreReader.NodeType);
                                recordedNode.SetItemData(await _coreReader.GetValueAsync().ConfigureAwait(false));
                                recordedNode.SetLineInfo(_lineInfo);
                                recordedNode.Depth = _coreReader.Depth;
                                break;

                            default:
                                break;
                        }
                        _cachedNode = recordedNode;
                        return true;
                    }
                    else
                    {
                        _cacheState = CachingReaderState.ReaderClosed;
                        return false;
                    }

                case CachingReaderState.Replay:
                    if (_currentContentIndex >= _contentIndex)
                    { //When positioned on the last cached node, switch back as the underlying coreReader is still positioned on this node
                        _cacheState = CachingReaderState.ReaderClosed;
                        _cacheHandler(this);
                        if (_coreReader.NodeType != XmlNodeType.Element || _readAhead)
                        { //Only when coreReader not positioned on Element node, read ahead, otherwise it is on the next element node already, since this was not cached
                            return await _coreReader.ReadAsync().ConfigureAwait(false);
                        }
                        return true;
                    }
                    _cachedNode = _contentEvents[_currentContentIndex];
                    if (_currentContentIndex > 0)
                    {
                        ClearAttributesInfo();
                    }
                    _currentContentIndex++;
                    return true;

                default:
                    return false;
            }
        }

        // Skips to the end tag of the current element.
        public override async Task SkipAsync()
        {
            //Skip on caching reader should move to the end of the subtree, past all cached events
            switch (_cachedNode.NodeType)
            {
                case XmlNodeType.Element:
                    if (_coreReader.NodeType != XmlNodeType.EndElement && !_readAhead)
                    { //will be true for IsDefault cases where we peek only one node ahead
                        int startDepth = _coreReader.Depth - 1;
                        while (await _coreReader.ReadAsync().ConfigureAwait(false) && _coreReader.Depth > startDepth)
                            ;
                    }
                    await _coreReader.ReadAsync().ConfigureAwait(false);
                    _cacheState = CachingReaderState.ReaderClosed;
                    _cacheHandler(this);
                    break;

                case XmlNodeType.Attribute:
                    MoveToElement();
                    goto case XmlNodeType.Element;

                default:
                    Debug.Assert(_cacheState == CachingReaderState.Replay);
                    await ReadAsync().ConfigureAwait(false);
                    break;
            }
        }

        //Private methods
        internal Task SetToReplayModeAsync()
        {
            _cacheState = CachingReaderState.Replay;
            _currentContentIndex = 0;
            _currentAttrIndex = -1;
            return ReadAsync(); //Position on first node recorded to begin replaying
        }
    }
}
