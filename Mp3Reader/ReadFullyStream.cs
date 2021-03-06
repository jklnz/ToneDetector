﻿using System;
using System.IO;

namespace Mp3Reader
{
    //Lightly modified version of the ReadFullyStream class from the NAudio examples project
    //https://github.com/naudio/NAudio
    public class ReadFullyStream : Stream
    {
        private readonly Stream _sourceStream;
        private long _pos;
        private readonly byte[] _readAheadBuffer;
        private int _readAheadLength;
        private int _readAheadOffset;

        public ReadFullyStream(Stream sourceStream)
        {
            _sourceStream = sourceStream;
            _readAheadBuffer = new byte[4096];
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override long Length => _pos;

        public override long Position
        {
            get
            {
                return _pos;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = 0;
            while (bytesRead < count)
            {
                var readAheadAvailableBytes = _readAheadLength - _readAheadOffset;
                var bytesRequired = count - bytesRead;
                if (readAheadAvailableBytes > 0)
                {
                    var toCopy = Math.Min(readAheadAvailableBytes, bytesRequired);
                    Array.Copy(_readAheadBuffer, _readAheadOffset, buffer, offset + bytesRead, toCopy);
                    bytesRead += toCopy;
                    _readAheadOffset += toCopy;
                }
                else
                {
                    _readAheadOffset = 0;
                    _readAheadLength = _sourceStream.Read(_readAheadBuffer, 0, _readAheadBuffer.Length);
                    if (_readAheadLength == 0)
                    {
                        break;
                    }
                }
            }
            _pos += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }
    }
}
