using System.Diagnostics;

namespace Diagnostics
{
    public class CustomTextWriterTraceListener : TextWriterTraceListener
    {
        public CustomTextWriterTraceListener(string? fileName) : base(fileName) { }

        public override void Write(string? message)
        {
            // do nothing
        }
    }
}