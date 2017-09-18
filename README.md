# Lyre
Chrome Native Messaging implementation for .NET and .NET Core.
Allows easy communication with a Chrome extension using Chrome Native Messaging protocol.

## Usage

    var host = new NativeMessagingHost();

    try
    {
        while (true)
        {
            var response = await host.Read<dynamic>();

            // Echo response
            await host.Write(new { value = $"You said {response.value} at {response.dateTime}", dateTime = DateTime.Now });
        }
    }
    catch (EndOfStreamException)
    {
        // Disconnected
    }
    
## Customization

The NativeMessagingHost uses JSON.NET for serialization. This can be customized by passing in a `JsonSerializerSettings` object.
The `Encoding` and `Stream` objects used for communications can also be passed into the constructor.

    var host = new NativeMessagingHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), Encoding.UTF8, new JsonSerializerSettings{ Formatting = Formatting.None});
