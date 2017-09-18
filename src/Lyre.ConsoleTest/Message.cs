using System;

namespace Lyre.ConsoleTest
{
    public class Message
    {
        public Message(string message)
        {
            Value = message;
        }

        public DateTime DateTime { get; set; } = DateTime.Now;
        public string Value { get; set; }
    }
}