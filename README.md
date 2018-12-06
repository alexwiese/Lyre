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
    
## Example/Chrome Extension

See https://github.com/alexwiese/Lyre/tree/master/src/Lyre.ConsoleTest for an example of a Chrome extension communicating with a Native Messaging Host.
    
## Customization

The NativeMessagingHost uses JSON.NET for serialization. This can be customized by passing in a `JsonSerializerSettings` object.
The `Encoding` and `Stream` objects used for communications can also be passed into the constructor.

    var host = new NativeMessagingHost(Console.OpenStandardInput(), Console.OpenStandardOutput(), Encoding.UTF8, new JsonSerializerSettings{ Formatting = Formatting.None});

## Supressing Console Output

By default Chrome Native Messaging uses `stdin` and `stdout` to communicate. If any other code writes to the `Console`, for example by calling `Console.WriteLine(string)`, then this would cause the Chrome Native Messaging pipe to fail due to unexpected output. Helper methods are provided by the `NativeMessagingEnvironment` class to supress or redirect the Console output.

### Supress console output

    // Redirects any calls to Console.Write() or Console.WriteLine() to TextWriter.Null
    NativeMessagingEnvironment.SupressConsoleOutput();
    
### Redirect to stderr

    // This will redirect calls to Console.Write() and Console.WriteLine() to stderr
    NativeMessagingEnvironment.RedirectConsoleOutputToErrorOutput();

### Redirect to Debug output

    // Redirects any calls to Console.Write() or Console.WriteLine() to the Debug output
    NativeMessagingEnvironment.RedirectConsoleOutputToDebugStream();
    
### Redirect to a TextWriter

    // This will redirect calls to Console.Write() and Console.WriteLine() to a file
    var writer = new StreamWriter(File.OpenWrite("console.out")) { AutoFlush = true };
    NativeMessagingEnvironment.RedirectConsoleOutput(writer);


    
    
    
