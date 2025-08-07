using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lyre
{
    /// <summary>
    /// Provides methods for communicating over streams via the Chrome Native Messaging protocol.
    /// </summary>
    /// <remarks>
    /// Typically Chrome Native Messaging communicates via the stdin and stdout streams, 
    /// but any streams that support reading and writing can be used.
    /// </remarks>
    public class NativeMessagingHost : IDisposable
    {
        /// <summary>
        /// Maximum message size allowed by Chrome Native Messaging protocol (1MB)
        /// </summary>
        public const int MaxMessageSize = 1024 * 1024;

        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly bool _ownsStreams;

        private readonly byte[] _sendLengthBuffer = new byte[sizeof(int)];
        private readonly byte[] _receiveLengthBuffer = new byte[sizeof(int)];

        private bool _disposed = false;

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMessagingHost"/> using the 
        /// stdin input stream, the stdout ouput stream, <see cref="UTF8Encoding"/> and the 
        /// default serialization settings.
        /// </summary>
        public NativeMessagingHost()
            : this(Console.OpenStandardInput(), Console.OpenStandardOutput(), Encoding.UTF8, DefaultSerializerSettings, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMessagingHost"/> using the 
        /// provided input stream, ouput stream, <see cref="Encoding"/> and <see cref="JsonSerializerSettings"/>.
        /// </summary>
        public NativeMessagingHost(Stream inStream, Stream outStream, Encoding encoding, JsonSerializerSettings jsonSerializerSettings)
            : this(inStream, outStream, encoding, jsonSerializerSettings, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMessagingHost"/> using the 
        /// provided input stream, ouput stream, <see cref="Encoding"/>, <see cref="JsonSerializerSettings"/>,
        /// and a value indicating whether this instance owns the streams.
        /// </summary>
        public NativeMessagingHost(Stream inStream, Stream outStream, Encoding encoding, JsonSerializerSettings jsonSerializerSettings, bool ownsStreams)
        {
            _inStream = inStream ?? throw new ArgumentNullException(nameof(inStream));
            _outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _ownsStreams = ownsStreams;
        }

        /// <summary>
        /// Serializes an object to a JSON string and sends the result to the 
        /// Native Messaging output stream.
        /// </summary>
        /// <param name="value">The object to serialize and send.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <exception cref="ArgumentException">Thrown when the serialized message exceeds the maximum allowed size.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the host has been disposed.</exception>
        public async Task Write(object value, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var jsonString = JsonConvert.SerializeObject(value, _jsonSerializerSettings);

            // Use the encoding to get the length of the 
            // buffer, it may not be the number of chars
            var byteBuffer = _encoding.GetBytes(jsonString);
            var byteBufferLength = byteBuffer.Length;

            // Validate message size according to Chrome Native Messaging limits
            if (byteBufferLength > MaxMessageSize)
            {
                throw new ArgumentException($"Message size ({byteBufferLength} bytes) exceeds Chrome Native Messaging maximum of {MaxMessageSize} bytes", nameof(value));
            }

            _sendLengthBuffer[0] = (byte)byteBufferLength;
            _sendLengthBuffer[1] = (byte)(byteBufferLength >> 8);
            _sendLengthBuffer[2] = (byte)(byteBufferLength >> 16);
            _sendLengthBuffer[3] = (byte)(byteBufferLength >> 24);

            // Send the buffer length (Int32) then the JSON
            await _outStream.WriteAsync(_sendLengthBuffer, 0, sizeof(int), cancellationToken);
            await _outStream.WriteAsync(byteBuffer, 0, byteBufferLength, cancellationToken);
        }

        /// <summary>
        /// Serializes an object to a JSON string and sends the result to the 
        /// Native Messaging output stream.
        /// </summary>
        /// <param name="value">The object to serialize and send.</param>
        public async Task Write(object value)
        {
            await Write(value, CancellationToken.None);
        }

        /// <summary>
        /// Reads and deserializes a JSON message from the Native Messaging input stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>The deserialized object from the JSON string.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the host has been disposed.</exception>
        public async Task<T> Read<T>(CancellationToken cancellationToken = default)
        {
            var jsonString = await ReadAsString(cancellationToken);

            return JsonConvert.DeserializeObject<T>(jsonString, _jsonSerializerSettings);
        }

        /// <summary>
        /// Reads and deserializes a JSON message from the Native Messaging input stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <returns>The deserialized object from the JSON string.</returns>
        public async Task<T> Read<T>()
        {
            return await Read<T>(CancellationToken.None);
        }

        /// <summary>
        /// Reads a JSON string from the Native Messaging input stream.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the message length is negative or exceeds the maximum allowed size.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the host has been disposed.</exception>
        public async Task<string> ReadAsString(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var length = await ReadInt32(cancellationToken);

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length of message cannot be negative");
            }

            if (length > MaxMessageSize)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, $"Message size ({length} bytes) exceeds Chrome Native Messaging maximum of {MaxMessageSize} bytes");
            }

            var jsonString = await ReadString(length, cancellationToken);

            return jsonString;
        }

        /// <summary>
        /// Reads a JSON string from the Native Messaging input stream.
        /// </summary>
        public async Task<string> ReadAsString()
        {
            return await ReadAsString(CancellationToken.None);
        }

        private async Task<int> ReadInt32(CancellationToken cancellationToken = default)
        {
            var bytesRead = 0;

            do
            {
                var read = await _inStream.ReadAsync(_receiveLengthBuffer, 0, sizeof(int) - bytesRead, cancellationToken);

                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                bytesRead += read;
            } while (bytesRead < sizeof(int));

            return _receiveLengthBuffer[0] | _receiveLengthBuffer[1] << 8 | _receiveLengthBuffer[2] << 16 | _receiveLengthBuffer[3] << 24;
        }

        private async Task<string> ReadString(int numberOfBytes, CancellationToken cancellationToken = default)
        {
            if (numberOfBytes == 0)
            {
                return string.Empty;
            }

            // We know the length of the expected message
            var byteBuffer = new byte[numberOfBytes];

            var bytesRead = 0;

            // This will read from the stream into the byteBuffer 
            // until all bytes have been received
            do
            {
                var read = await _inStream.ReadAsync(byteBuffer, bytesRead, numberOfBytes - bytesRead, cancellationToken);

                if (read == 0)
                {
                    // Disconnected
                    throw new EndOfStreamException();
                }

                bytesRead += read;

            } while (bytesRead < numberOfBytes);

            return _encoding.GetString(byteBuffer);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NativeMessagingHost));
            }
        }

        /// <summary>
        /// Releases all resources used by the NativeMessagingHost.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the NativeMessagingHost and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_ownsStreams)
                {
                    _inStream?.Dispose();
                    _outStream?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
