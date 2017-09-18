using System;
using System.IO;
using System.Threading.Tasks;

namespace Lyre.ConsoleTest
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new NativeMessagingHost();

            try
            {
                while (true)
                {
                    var response = await host.Read<Message>();
                    
                    // Echo response
                    await host.Write(new Message($"You said {response.Value} at {response.DateTime}"));
                }
            }
            catch (EndOfStreamException)
            {
                // Disconnected
            }
            catch (Exception exception)
            {
                await host.Write(new Message($"Error: {exception}"));

                Console.WriteLine($"Oh 💩: {exception}");
                Environment.FailFast("Oh 💩", exception);
            }
        }
    }
}
