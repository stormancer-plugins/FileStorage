using System;
using System.IO;

namespace Stormancer.Server.FileStorage
{
    /// <summary>
    /// Describes a Stream that wraps an underlying stream but
    /// which limits the length. This is used for processing
    /// length-prefied messages (string wire-type) so that no
    /// complex code is required to manage the end of each
    /// object.
    /// </summary>
    internal sealed class SubStream : Stream
    {
        private Stream _parent;

        private readonly long _length;

        private readonly bool _closesParent;

        private long _position;

        public bool ThrowErrorOnEof { get; set; } = true;


        public SubStream(Stream parent, bool closesParent)
            : this(parent, parent.Length - parent.Position, closesParent)
        {

        }

        public SubStream(Stream parent, long length, bool closesParent)
        {

            if (parent == null) throw new ArgumentNullException("parent");

            if (length < 0) throw new ArgumentOutOfRangeException("length");


            this._parent = parent;

            this._length = length;

            this._closesParent = closesParent;

        }

        private void CheckDisposed()
        {

            if (_parent == null) throw new ObjectDisposedException(GetType().Name);

        }

        protected override void Dispose(bool disposing)
        {
            const int DEFAULT_SIZE = 4096;

            var parent = this._parent;
            this._parent = null;
            if (disposing && parent != null)
            {

                if (_closesParent)
                { // close the parent completely

                    using (parent) { }

                }

                else
                { // move the parent past this sub-data

                    long remaining = _length - _position, bytes;

                    if (remaining > 0)
                    {

                        if (CanSeek)
                        {   // seek the stream

                            parent.Seek(remaining, SeekOrigin.Current);

                        }

                        else if (CanRead)
                        {   // burn up the stream


                            var buffer = new byte[remaining < DEFAULT_SIZE ? remaining : DEFAULT_SIZE];

                            while (remaining > 0 && (bytes = parent.Read(buffer, 0, buffer.Length)) > 0)
                            {

                                remaining -= bytes;
                            }

                        }
                        else if (CanWrite)
                        {
                            // write 0s to the underlying stream until meeting the end of the reserved section

                            var buffer = new byte[remaining < DEFAULT_SIZE ? remaining : DEFAULT_SIZE];

                            while (remaining > 0)
                            {
                                parent.Write(buffer, 0, buffer.Length);
                                remaining -= buffer.Length;
                            }
                        }

                    }

                }

            }       
            base.Dispose(disposing);
        }
    
        public override bool CanRead => _parent != null && _parent.CanRead;

        public override bool CanWrite => _parent != null && _parent.CanWrite;

        public override bool CanSeek => _parent != null && _parent.CanSeek;

        public override void Flush()
        {
            _parent.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_parent.CanWrite)
            {
                throw new NotSupportedException();
            }
            if (count > Length - Position)
            {
                if (ThrowErrorOnEof)
                {
                    throw new InvalidOperationException("Cannot write outside the limits of this stream.");
                }
                else
                {
                    count = (int)(Length - Position);
                }
            }

            _parent.Write(buffer, offset, count);
            _position += count;
        }

        public override long Length
        {

            get { return _length; }

        }

        public override long Position
        {

            get
            {

                return _position;

            }



            set
            {

                if (value < 0 || value >= _length) throw new ArgumentOutOfRangeException("value", "Cannot seek outide of the Stream's bounds");

                _parent.Position += (value - _position);

                _position = value;
            }

        }



        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this.CanSeek)
            {
                throw new InvalidOperationException("Can't seek on this stream.");
            }

            switch (origin)
            {

                case SeekOrigin.Begin:

                    Position = offset;

                    break;

                case SeekOrigin.Current:

                    Position += offset;

                    break;

                case SeekOrigin.End:

                    Position = Length + offset;

                    break;

                default:

                    throw new ArgumentException("Unknown seek-origin", "origin");

            }



            return Position;

        }



        public override void SetLength(long value)
        {

            throw new NotSupportedException();

        }



        public override int ReadByte()
        {

            CheckDisposed();

            if (_position >= _length)
            {

                return -1;

            }



            int result = _parent.ReadByte();

            if (result >= 0) _position++;

            return result;

        }



        public override int Read(byte[] buffer, int offset, int count)
        {

            CheckDisposed();

            long remaining = _length - _position;

            if (remaining <= 0) return 0;

            if (count > remaining) count = (int)remaining;

            count = _parent.Read(buffer, offset, count);

            if (count > 0)
            {

                _position += count;

            }



            return count;

        }

    }

}

