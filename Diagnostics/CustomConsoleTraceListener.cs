using System.Diagnostics;

namespace Diagnostics
{
    public class CustomConsoleTraceListener : ConsoleTraceListener
    {
        public CustomConsoleTraceListener(int maxLength, bool doNotTruncate) : base()
        {
            this.maxLength = maxLength;
            this.doNotTruncate = doNotTruncate;
        }

        public override void Write(string? message)
        {
            // do nothing
        }

        public override void WriteLine(string? message)
        {
            message = doNotTruncate || message?.Length < maxLength
                ? message
                : $"{message?.Substring(0, maxLength - 4)} ...";
            base.WriteLine(message);
        }

        private int maxLength;
        private bool doNotTruncate;
    }
}