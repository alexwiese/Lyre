using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lyre.Tests
{
    public class NativeMessagingHostTests
    {
        [Fact]
        public void Constructor_WithNullInputStream_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NativeMessagingHost(null, new MemoryStream(), Encoding.UTF8, null));
        }

        [Fact]
        public void Constructor_WithNullOutputStream_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NativeMessagingHost(new MemoryStream(), null, Encoding.UTF8, null));
        }

        [Fact]
        public void MaxMessageSize_ShouldBe1MB()
        {
            // Arrange & Act & Assert
            Assert.Equal(1024 * 1024, NativeMessagingHost.MaxMessageSize);
        }

        [Fact]
        public async Task Write_WithValidMessage_ShouldSucceed()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            using var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);
            
            var message = new { text = "Hello, World!" };

            // Act
            await host.Write(message);

            // Assert
            Assert.True(outputStream.Length > 0);
        }

        [Fact]
        public async Task Write_WithLargeMessage_ThrowsArgumentException()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            using var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);
            
            // Create a message that's larger than 1MB
            var largeString = new string('A', NativeMessagingHost.MaxMessageSize);
            var message = new { text = largeString };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => host.Write(message));
            Assert.Contains("exceeds Chrome Native Messaging maximum", exception.Message);
        }

        [Fact]
        public async Task Write_DisposedHost_ThrowsObjectDisposedException()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);
            host.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => host.Write(new { text = "test" }));
        }

        [Fact]
        public async Task ReadWriteRoundTrip_ShouldPreserveMessage()
        {
            // Arrange
            using var stream = new MemoryStream();
            using var host = new NativeMessagingHost(stream, stream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);
            
            var originalMessage = new { text = "Hello, World!", number = 42 };

            // Act
            await host.Write(originalMessage);
            
            // Reset stream position for reading
            stream.Position = 0;
            
            var receivedMessage = await host.Read<dynamic>();

            // Assert
            Assert.Equal("Hello, World!", (string)receivedMessage.text);
            Assert.Equal(42L, (long)receivedMessage.number); // JSON.NET deserializes numbers as long by default
        }

        [Fact]
        public async Task ReadAsString_WithNegativeLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            
            // Write a negative length (all bits set)
            var negativeLength = BitConverter.GetBytes(-1);
            inputStream.Write(negativeLength, 0, negativeLength.Length);
            inputStream.Position = 0;
            
            using var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.ReadAsString());
        }

        [Fact]
        public async Task ReadAsString_WithMessageTooLarge_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            
            // Write a length larger than MaxMessageSize
            var largeLength = BitConverter.GetBytes(NativeMessagingHost.MaxMessageSize + 1);
            inputStream.Write(largeLength, 0, largeLength.Length);
            inputStream.Position = 0;
            
            using var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.ReadAsString());
            Assert.Contains("exceeds Chrome Native Messaging maximum", exception.Message);
        }

        [Fact]
        public async Task ReadAsString_DisposedHost_ThrowsObjectDisposedException()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);
            host.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => host.ReadAsString());
        }

        [Fact]
        public void Dispose_ShouldNotThrowWhenCalledMultipleTimes()
        {
            // Arrange
            using var inputStream = new MemoryStream();
            using var outputStream = new MemoryStream();
            var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), true);

            // Act & Assert
            host.Dispose();
            host.Dispose(); // Should not throw
        }

        [Fact]
        public void Dispose_WithOwnsStreamsFalse_ShouldNotDisposeStreams()
        {
            // Arrange
            var inputStream = new MemoryStream();
            var outputStream = new MemoryStream();
            var host = new NativeMessagingHost(inputStream, outputStream, Encoding.UTF8, new Newtonsoft.Json.JsonSerializerSettings(), false);

            // Act
            host.Dispose();

            // Assert
            Assert.True(inputStream.CanRead); // Stream should still be usable
            Assert.True(outputStream.CanWrite); // Stream should still be usable
            
            // Clean up
            inputStream.Dispose();
            outputStream.Dispose();
        }
    }
}