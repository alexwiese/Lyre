using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lyre
{
    internal class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter()
            : base(new DebugStream(Encoding.UTF8), Encoding.UTF8)
        {
            this.AutoFlush = true;
        }
        
        private class DebugStream : Stream
        {
            private readonly Encoding _encoding;

            public DebugStream(Encoding encoding)
            {
                _encoding = encoding;
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new InvalidOperationException();

            public override long Position
            {
                get => throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Debug.Write(_encoding.GetString(buffer, offset, count));
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException();
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException();
            }
        }
    }
}