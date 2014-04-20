#region Using Directives

using System.Text;
using log4net.Appender;
using log4net.Core;

#endregion

namespace Endeca.Control.EacToolkit
{
    public class StringAppender : AppenderSkeleton
    {
        private static readonly StringBuilder sb = new StringBuilder();

        protected override bool RequiresLayout
        {
            get { return true; }
        }

        public string GetLog()
        {
            return sb.ToString();
        }

        public void ResetLog()
        {
            sb.Length = 0;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            sb.Append(RenderLoggingEvent(loggingEvent));
        }
    }
}