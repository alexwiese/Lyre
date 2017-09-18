using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lyre
{
    public class NativeMessagingHost
    {
        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly byte[] _buffer = new byte[sizeof(int)];

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

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

            _decoder = encoding.GetDecoder();
        }
        
        public async Task Write(object value)
        {
            var jsonString = JsonConvert.SerializeObject(value, _jsonSerializerSettings);

            // Use the encoding to get the length of the 
            // buffer, may not be the number of chars
            var byteBuffer = _encoding.GetBytes(jsonString);
            var lengthBuffer = BitConverter.GetBytes(byteBuffer.Length);

            // Send buffer length (Int32) then the JSON
            await _outStream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
            await _outStream.WriteAsync(byteBuffer, 0, byteBuffer.Length);
        }
        
        public async Task<T> Read<T>()
        {
            var length = await ReadInt32();

            var jsonString = await ReadString(length);
            
            return JsonConvert.DeserializeObject<T>(jsonString, _jsonSerializerSettings);
        }

        private async Task<int> ReadInt32()
        {
            var bytesRead = 0;

            do
            {
                var read = await _inStream.ReadAsync(_buffer, 0, sizeof(int));

                if (read == 0)
                {
                    throw new EndOfStreamException();
                }

                bytesRead += read;
            } while (bytesRead < sizeof(int));

            return _buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24;
        }

        private async Task<string> ReadString(int numberOfBytes)
        {
            if (numberOfBytes == 0)
            {
                return string.Empty;
            }

            var byteBuffer = new byte[numberOfBytes];

            // The number of characters depends on the encoding
            // The char buffer length is defined as the 
            // maximum number of chars that can be represented in 
            // this many bytes for the Encoding used
            var charBuffer = new char[_encoding.GetMaxCharCount(numberOfBytes)];

            var bytesRead = 0;
            var charactersRead = 0;

            do
            {
                var read = await _inStream.ReadAsync(byteBuffer, 0, numberOfBytes - bytesRead);

                if (read == 0)
                {
                    // Disconnected
                    throw new EndOfStreamException();
                }

                charactersRead += _decoder.GetChars(byteBuffer, 0, read, charBuffer, charactersRead);
                bytesRead += read;

            } while (bytesRead < numberOfBytes);

            return new string(charBuffer, 0, charactersRead);
        }
    }
}
