using System;
using System.IO;
using System.Text;
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
    public class NativeMessagingHost
    {
        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private readonly Encoding _encoding;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly byte[] _sendLengthBuffer = new byte[sizeof(int)];
        private readonly byte[] _receiveLengthBuffer = new byte[sizeof(int)];

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
            : this(Console.OpenStandardInput(), Console.OpenStandardOutput(), Encoding.UTF8, DefaultSerializerSettings)
        {
        }

        public NativeMessagingHost(Stream inStream, Stream outStream, Encoding encoding, JsonSerializerSettings jsonSerializerSettings)
        {
            _inStream = inStream ?? throw new ArgumentNullException(nameof(inStream));
            _outStream = outStream ?? throw new ArgumentNullException(nameof(outStream));
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
        }

        /// <summary>
        /// Serializes an object to a JSON string and sends the result to the 
        /// Native Messaging output stream.
        /// </summary>
        /// <param name="value">The object to serialize and send.</param>
        public async Task Write(object value)
        {
            var jsonString = JsonConvert.SerializeObject(value, _jsonSerializerSettings);

            // Use the encoding to get the length of the 
            // buffer, it may not be the number of chars
            var byteBuffer = _encoding.GetBytes(jsonString);
            var byteBufferLength = byteBuffer.Length;

            _sendLengthBuffer[0] = (byte)byteBufferLength;
            _sendLengthBuffer[1] = (byte)(byteBufferLength >> 8);
            _sendLengthBuffer[2] = (byte)(byteBufferLength >> 16);
            _sendLengthBuffer[3] = (byte)(byteBufferLength >> 24);

            // Send the buffer length (Int32) then the JSON
            await _outStream.WriteAsync(_sendLengthBuffer, 0, sizeof(int));
            await _outStream.WriteAsync(byteBuffer, 0, byteBufferLength);
        }

        /// <summary>
        /// Reads and deserializes a JSON message from the Native Messaging input stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <returns>The deserialized object from the JSON string.</returns>
        public async Task<T> Read<T>()
        {
            var jsonString = await ReadAsString();

            return JsonConvert.DeserializeObject<T>(jsonString, _jsonSerializerSettings);
        }

        /// <summary>
        /// Reads a JSON string from the Native Messaging input stream.
        /// </summary>
        public async Task<string> ReadAsString()
        {
            var length = await ReadInt32();

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length of message cannot be negative");
            }

            var jsonString = await ReadString(length);

            return jsonString;
        }

        private async Task<int> ReadInt32()
        {
            var bytesRead = 0;

            do
            {
                var read = await _inStream.ReadAsync(_receiveLengthBuffer, 0, sizeof(int) - bytesRead);

                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                bytesRead += read;
            } while (bytesRead < sizeof(int));

            return _receiveLengthBuffer[0] | _receiveLengthBuffer[1] << 8 | _receiveLengthBuffer[2] << 16 | _receiveLengthBuffer[3] << 24;
        }

        private async Task<string> ReadString(int numberOfBytes)
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
                var read = await _inStream.ReadAsync(byteBuffer, bytesRead, numberOfBytes - bytesRead);

                if (read == 0)
                {
                    // Disconnected
                    throw new EndOfStreamException();
                }

                bytesRead += read;

            } while (bytesRead < numberOfBytes);

            return _encoding.GetString(byteBuffer);
        }
    }
}
