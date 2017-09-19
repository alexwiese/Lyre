using System;
using System.IO;

namespace Lyre
{
    public static class NativeMessagingEnvironment
    {
        public static void SupressConsoleOutput()
        {
            RedirectConsoleOutput(TextWriter.Null);
        }

        public static void RedirectConsoleOutputToDebugStream()
        {
            RedirectConsoleOutput(new DebugTextWriter());
        }

        public static void RedirectConsoleOutputToErrorOutput()
        {
            RedirectConsoleOutput(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true});
        }

        public static void RedirectConsoleOutput(TextWriter textWriter)
        {
            Console.SetOut(textWriter);
        }
    }
}