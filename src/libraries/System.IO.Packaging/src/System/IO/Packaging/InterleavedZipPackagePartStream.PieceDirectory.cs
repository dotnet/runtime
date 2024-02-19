// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;

namespace System.IO.Packaging
{

    internal partial class InterleavedZipPackagePartStream
    {
        /// <summary>
        /// A PieceDirectory maps a part offset to a piece number and a piece number to piece data
        /// (e.g. piece stream and piece start offset).
        /// </summary>

        // Implementation outline:
        //  A PieceDirectory is based on an array of PieceStreamInfo descriptors that are sorted by start offsets.
        //  At any point in time, the members of the sorted array _pieceStreamInfoArray are *adjacent* pieces whose
        //  index reflects their order in the part. On the other hand, _pieceStreamInfoArray is not required to represent
        //  all the pieces in the part. (The last useful entry in _pieceStreamInfoArray is at _indexOfLastPieceStreamInfoAccessed.)
        //  At init time, the first piece descriptor is loaded into _pieceStreamInfoArray. Thereafter, descriptors are loaded
        //  on demand in piece order and without any gap between them.

        private sealed class PieceDirectory
        {
            /// <summary>
            /// PieceStreamInfo Descriptor.
            /// Provides access to piece's stream and start offset.
            /// </summary>
            /// <remarks>
            /// This is a private class. All of the above information, together with position
            /// information (piece number, last-piece status), is accessed through the
            /// PieceDirectory class by providing offsets or piece numbers.
            /// </remarks>
            private sealed class PieceStreamInfo : IComparable<PieceStreamInfo>
            {

                /// <summary>
                /// Initialize a PieceStreamInfo.
                /// </summary>
                internal PieceStreamInfo(Stream stream, long pieceStart)
                {
                    Debug.Assert(stream != null);
                    Debug.Assert(pieceStart >= 0);
                    Stream = stream;
                    StartOffset = pieceStart;
                }

                /// <summary>
                /// Returns the startOffset of the piece in the part
                /// </summary>
                internal long StartOffset { get; }


                /// <summary>
                /// Returns the stream for this piece
                /// </summary>
                internal Stream Stream { get; set; }


                /// <summary>
                /// IComparable.CompareTo implementation which allows sorting
                /// descriptors by range of offsets.
                /// </summary>
                int IComparable<PieceStreamInfo>.CompareTo(PieceStreamInfo? pieceStreamInfo)
                {
                    return pieceStreamInfo == null
                        ? 1
                        : StartOffset.CompareTo(pieceStreamInfo.StartOffset);
                }

            }

            /// <summary>
            /// Load the descriptor of the first piece into _pieceStreamInfoArray and store context data
            /// extracted from the arguments.
            /// </summary>
            internal PieceDirectory(List<ZipPackagePartPiece> sortedPieceInfoList, ZipStreamManager zipStreamManager, FileAccess access)
            {
                Debug.Assert(sortedPieceInfoList.Count > 0);

                // Initialize the first piece.
                _pieceStreamInfoList =
                [
                    new PieceStreamInfo(zipStreamManager.Open(sortedPieceInfoList[0].ZipArchiveEntry, access), pieceStart: 0)
                ];

                //Index of the last piece stream that has been accessed
                _indexOfLastPieceStreamInfoAccessed = 0;

                //Last Piece number
                //Its guaranteed to be non-negative based on the assert above
                _lastPieceIndex = sortedPieceInfoList.Count - 1;

                // Store information necessary to build following piece streams.
                _zipStreamManager = zipStreamManager;
                _fileAccess = access;
                _sortedPieceInfoList = sortedPieceInfoList;
                _zipArchive = sortedPieceInfoList[0].ZipArchiveEntry.Archive;
            }

            //------------------------------------------------------
            //
            //   Internal Methods
            //
            //------------------------------------------------------

            /// <summary>
            /// Given an offset in the part, locate the piece that contains it.
            /// </summary>
            internal int GetPieceNumberFromOffset(long offset)
            {
                // Find the piece whose range includes offset.
                PieceStreamInfo temporaryPieceInfo = new PieceStreamInfo(_temporaryMemoryStream, offset);
                int pieceNumber = _pieceStreamInfoList.BinarySearch(temporaryPieceInfo);

                if (pieceNumber >= 0)
                {
                    // Found the piece that starts at offset 'offset'.
                    return pieceNumber;
                }

                // ~pieceNumber represents the place at which we would insert a piece starting at offset.
                // offset belongs therefore to the preceding piece.
                pieceNumber = (~pieceNumber - 1);

                // If the list contains data about pieces following pieceNumber, then we know offset precedes those
                // and is therefore in the scope of the piece at pieceNumber.
                if (pieceNumber < _indexOfLastPieceStreamInfoAccessed)
                    return pieceNumber;

                // The following tests may have to be repeated until we load enough descriptors to cover offset.
                // If there is no error in part numbering, we'll eventually find either the last part
                // or an intermediate part whose range contains offset.
                while (pieceNumber < _lastPieceIndex)
                {
                    // Make sure we have a descriptor and stream for piece pieceNumber.
                    PieceStreamInfo currentPieceInfo = RetrievePiece(pieceNumber);

                    // If the piece at pieceNumber is not expandable, then its length has to be taken into account.
                    // currentPieceInfo.Stream is guaranteed to be non-null
                    if (offset < checked(currentPieceInfo.StartOffset + currentPieceInfo.Stream.Length))
                        break;
                    // offset is not covered by any piece whose descriptor has been loaded.
                    // Keep loading piece descriptors.
                    checked
                    {
                        ++pieceNumber;
                    }
                }

                // If pieceNumber is the number of the last piece in the part, it is expandable and therefore
                // contains offset.
                // Else the pieceNumber should be less than the _lastPieceIndex
                Debug.Assert(pieceNumber <= _lastPieceIndex, "We should have found the valid pieceNumber earlier");

                return pieceNumber;
            }

            /// <summary>
            /// Return the piece stream for piece number pieceNumber.
            /// </summary>
            /// <remarks>
            /// pieceNumber is assumed to be a valid number. If it isn't, an assertion will fail.
            /// </remarks>
            internal Stream GetStream(int pieceNumber)
            {
                //Make sure that the stream has been initialized for this piece number
                PieceStreamInfo pieceStreamInfo = RetrievePiece(pieceNumber);
                return pieceStreamInfo.Stream;
            }

            /// <summary>
            /// Forces the piece stream for piece number pieceNumber to the beginning.
            /// </summary>
            /// <param name="pieceNumber"></param>
            /// <returns></returns>
            internal Stream ResetStream(int pieceNumber)
            {
                // Clean up and reinitialise the stream for this piece number
                PieceStreamInfo pieceStreamInfo = RetrievePiece(pieceNumber);

                pieceStreamInfo.Stream.Dispose();
                pieceStreamInfo.Stream = _zipStreamManager.Open(_sortedPieceInfoList[pieceNumber].ZipArchiveEntry,
                        _fileAccess);
                return pieceStreamInfo.Stream;
            }

            /// <summary>
            /// Return the start offset for piece number pieceNumber.
            /// </summary>
            /// <remarks>
            /// pieceNumber is assumed to be a valid number. If it isn't, an assertion will fail.
            /// </remarks>
            internal long GetStartOffset(int pieceNumber)
            {
                //Make sure that the stream has been initialized for this piece number
                PieceStreamInfo pieceStreamInfo = RetrievePiece(pieceNumber);
                return pieceStreamInfo.StartOffset;
            }

            /// <summary>
            /// Return true if pieceNumber is the number of the last piece, false if it precedes
            /// the number of the last piece, and raise an assertion violation if it follows it.
            /// </summary>
            internal bool IsLastPiece(int pieceNumber)
            {
                return _lastPieceIndex == pieceNumber;
            }

            /// <summary>
            /// This method is called to implement SetLength. If it changes
            /// the actual last piece, the next call to flush will perform the
            /// necessary renaming and deletion(s).
            /// </summary>
            internal void SetLogicalLastPiece(int pieceNumber)
            {
                //The Logical piece that we are setting should not be greater than the
                //last piece index
                Debug.Assert(pieceNumber <= _lastPieceIndex);

                //Make sure that the stream has been initialized for this piece number
                PieceStreamInfo _ = RetrievePiece(pieceNumber);

                // Update _lastPiece and record whether this invalidates physical pieces.
                if (_lastPieceIndex > pieceNumber)
                {
                    _logicalEndPrecedesPhysicalEnd = true;
                    _lastPieceIndex = pieceNumber;
                    // To avoid any potential for confusion, remove any invalidated piece from _pieceStreamInfoArray.
                    // We also need to dispose the underlying stream to ensure that the zip entries can be removed.
                    _indexOfLastPieceStreamInfoAccessed = _lastPieceIndex;

                    for (int i = _indexOfLastPieceStreamInfoAccessed + 1; i < _pieceStreamInfoList.Count; i++)
                    {
                        _pieceStreamInfoList[i].Stream.Dispose();
                    }
                    _pieceStreamInfoList.RemoveRange(_indexOfLastPieceStreamInfoAccessed + 1, _pieceStreamInfoList.Count - (_indexOfLastPieceStreamInfoAccessed + 1));
                }
            }

            /// <summary>
            /// Flush each underlying stream accessed so far and update the physical
            /// last piece if it differs from the logical one.
            /// </summary>
            internal void Flush()
            {
                UpdatePhysicalEndIfNecessary();

                for (int i = 0; i <= _indexOfLastPieceStreamInfoAccessed; ++i)
                {
                    _pieceStreamInfoList[i].Stream.Flush();
                }
            }

            /// <summary>
            /// Close each underlying stream accessed so far. Commit the last-part information
            /// if necessary.
            /// </summary>
            /// <remarks>
            /// Underlying streams will throw ObjectDisposedException on subsequent access attempts.
            /// </remarks>
            internal void Close()
            {
                UpdatePhysicalEndIfNecessary();

                for (int i = 0; i <= _indexOfLastPieceStreamInfoAccessed; ++i)
                {
                    _pieceStreamInfoList[i].Stream.Close();
                }
            }

            /// <summary>
            /// Returns the number of pieces that make up the entire part.
            /// Note: The streams for all the pieces may not be loaded at
            /// the time this method is called
            /// When individual piece streams will be asked for that is when
            /// we will try to load the streams.
            /// </summary>
            internal int GetNumberOfPieces()
            {
                return _lastPieceIndex + 1;
            }

            /// <summary>
            /// Return the descriptor for piece number pieceNumber.
            /// This method does lazy initializations of the streams corresponding
            /// to the pieces that make up the part.
            /// </summary>
            /// <remarks>
            /// pieceNumber is assumed to be a valid number. If it isn't, an assertion will fail.
            /// </remarks>
            private PieceStreamInfo RetrievePiece(int pieceNumber)
            {
                if (pieceNumber > _lastPieceIndex)
                    throw new ArgumentException(SR.PieceDoesNotExist);

                if (_indexOfLastPieceStreamInfoAccessed >= pieceNumber)
                    return _pieceStreamInfoList[pieceNumber];

                // The search below supposes the list is initially non-empty.
                // This invariant is enforced by the constructor.

                // Load descriptors for all pieces from _indexOfLastPieceStreamInfoAccessed+1 through pieceNumber.

                PieceStreamInfo currentPieceStreamInfo = _pieceStreamInfoList[_indexOfLastPieceStreamInfoAccessed];

                //we retrieve piece streams "upto the requested piece number" rather than getting just the
                //stream corresponding "to the requested piece number" for the following two reasons -
                //a. We need to be able to calculate the correct startOffset and as such need the lengths
                //   of all the intermediate streams
                //b. We also want to make sure that the intermediate streams do exists as it would be an
                //   error if they are missing or corrupt.
                for (int i = _indexOfLastPieceStreamInfoAccessed + 1; i <= pieceNumber; ++i)
                {
                    // Compute StartOffset.
                    long newStartOffset = checked(currentPieceStreamInfo.StartOffset + currentPieceStreamInfo.Stream.Length);

                    // Compute pieceInfoStream.Stream.
                    Stream pieceStream = _zipStreamManager.Open(_sortedPieceInfoList[pieceNumber].ZipArchiveEntry,
                        _fileAccess);

                    // Update _pieceStreamInfoArray.
                    _indexOfLastPieceStreamInfoAccessed = i;
                    currentPieceStreamInfo = new PieceStreamInfo(pieceStream, newStartOffset);

                    // !!!Implementation Note!!!
                    // List<> always adds the new item at the end of the list.
                    // _sortedPieceInfoList is sorted by the piecenumbers and so, when we add
                    // members to _pieceStreamInfoList they also get added in a sorted manner.
                    // If every the implementation changes, we must make sure that the
                    // _pieceStreamInfoList still remains sorted by the piecenumbers as we
                    // perform a binary search on this list in GetPieceNumberFromOffset method
                    _pieceStreamInfoList.Add(currentPieceStreamInfo);
                }
                return _pieceStreamInfoList[pieceNumber];
            }

            /// <summary>
            /// If the logical end precedes the physical end, delete invalidated pieces
            /// and rename the logical end to a name containing ".last".
            /// </summary>
            private void UpdatePhysicalEndIfNecessary()
            {
                if (!_logicalEndPrecedesPhysicalEnd)
                    return;

                // Close the pieces' streams for writing, then delete the invalidated pieces.
                int pieceNumber = _lastPieceIndex + 1;
                while (pieceNumber < _sortedPieceInfoList.Count)
                {
                    _sortedPieceInfoList[pieceNumber].ZipArchiveEntry.Delete();
                    pieceNumber++;
                }

                _sortedPieceInfoList.RemoveRange(_lastPieceIndex + 1, _sortedPieceInfoList.Count - (_lastPieceIndex + 1));

                // Since there is no rename in Zip I/O, getting the last piece to have .last
                // in its name necessarily involves creating a new piece. The simplest and most
                // effective solution consists in adding an empty terminal piece.

                // Number of the new physical last piece.
                int lastPiece = _lastPieceIndex + 1;

                // Record the compression parameters of the first piece to apply them to the new piece.
                // (Though this part will be created as empty, it may grow later.)
                ZipArchiveEntry firstPieceInfo = _sortedPieceInfoList[0].ZipArchiveEntry;

                // We have to special-case SetLength(0), because in that case, there is no nonempty
                // piece at all; and only the last piece is allowed to be empty.
                if (_lastPieceIndex == 0 && _pieceStreamInfoList[0].Stream.Length == 0)
                {
                    _pieceStreamInfoList[0].Stream.Dispose();
                    firstPieceInfo.Delete();

                    // The list of piece descriptors now becomes totally empty.
                    // This temporarily violates an invariant that should obtain again
                    // on exiting this function.
                    _indexOfLastPieceStreamInfoAccessed = -1;
                    //Remove all the items in the list
                    _pieceStreamInfoList.Clear();
                    lastPiece = 0; // Create "[0].last.piece"
                }

                // TODO: For correctness we should use nostore if first peice info specifies nostore. However that information is not available to us.
                ZipPackagePartPiece newLastPieceDescriptor = ZipPackagePartPiece.Create(_zipArchive, _sortedPieceInfoList[0].PartUri,
                    _sortedPieceInfoList[0].PrefixName, lastPiece, true /* last piece */);

                _lastPieceIndex = lastPiece;

                //We need to update the _sortedPieceInfoList with this new last piece information
                _sortedPieceInfoList[0] = newLastPieceDescriptor;

                // If we have been creating [0].last.piece, create a stream descriptor for it.
                // (In other cases, create on demand, as usual.)
                if (lastPiece == 0)
                {
                    Stream pieceStream = _zipStreamManager.Open(newLastPieceDescriptor.ZipArchiveEntry, _fileAccess);
                    _indexOfLastPieceStreamInfoAccessed = 0;

                    //The list should be empty at this point
                    Debug.Assert(_pieceStreamInfoList.Count == 0);
                    _pieceStreamInfoList.Add(new PieceStreamInfo(pieceStream, 0 /*startOffset*/));
                }

                // Mark update complete.
                _logicalEndPrecedesPhysicalEnd = false;
            }

            /// <summary>
            /// List of PieceStreamInfo objects, ordered by piece-stream offset.
            /// </summary>
            /// <remarks>
            /// <para>
            /// A PieceStreamInfo is specific to this class. It is an [offset, stream] pair.
            /// Note : If ever we invoke .Contains method on this list then we must implement
            /// the IEquatable interface on the PieceStreamInfo class.
            /// </para>
            /// </remarks>
            private int _indexOfLastPieceStreamInfoAccessed; // its _pieceStreamInfoList.Count - 1
            private readonly List<PieceStreamInfo> _pieceStreamInfoList;

            /// <summary>
            /// Array of piece descriptors, sorted by file name ignoring case.
            /// </summary>
            /// <remarks>
            /// A PieceInfo is a descriptor found in a ZipPackagePart. It is a
            /// [ZipFileInfo, PieceNameInfo] pair.
            /// </remarks>
            private readonly List<ZipPackagePartPiece> _sortedPieceInfoList;

            private readonly ZipStreamManager _zipStreamManager;

            private readonly ZipArchive _zipArchive;
            private readonly FileAccess _fileAccess;

            private int _lastPieceIndex;
            private bool _logicalEndPrecedesPhysicalEnd; //defaults to false;

            //We need this stream on for creating dummy PieceInfo object for comparison purposes
            private readonly Stream _temporaryMemoryStream = new MemoryStream(0);

        }

    }
}
