#region Using Directives

using System;
using log4net;
using log4net.Layout;

#endregion

namespace Endeca.Control.EacToolkit
{
    public static class Logger
    {
        private static readonly ILog logger = LogManager.GetLogger("Endeca.Control.EacToolkit");
        private static readonly ILog notifier = LogManager.GetLogger("Notifier");
        private static readonly StringAppender stringAppender = new StringAppender();

        static Logger()
        {
            stringAppender.Name = "StringAppender";
            var layout = new PatternLayout();
            layout.ConversionPattern = "%date{dd.MMM.yy HH:mm:ss} - %message%newline";
            layout.ActivateOptions();
            stringAppender.Layout = layout;
            stringAppender.ActivateOptions();
            var l = (log4net.Repository.Hierarchy.Logger) logger.Logger;
            l.AddAppender(stringAppender);
        }

        public static void Info(string msg)
        {
            if (logger.IsInfoEnabled)
            {
                logger.Info(msg);
            }
        }

        public static void Error(string msg)
        {
            Error(msg, null);
        }

        public static void Error(string msg, Exception e)
        {
            if (logger.IsErrorEnabled)
            {
                if (e == null)
                {
                    logger.Error(msg);
                }
                else
                {
                    logger.Error(msg, e);
                }
            }
        }

        public static void Fatal(string msg, Exception e)
        {
            if (logger.IsFatalEnabled)
            {
                if (e == null)
                {
                    logger.Fatal(msg);
                }
                else
                {
                    logger.Fatal(msg, e);
                }
            }
        }

        public static void Fatal(string msg)
        {
            Fatal(msg, null);
        }

        public static void Warn(string msg)
        {
            if (logger.IsWarnEnabled)
            {
                logger.Warn(msg);
            }
        }

        public static void Debug(string msg)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(msg);
            }
        }

        public static void Notify(string msg)
        {
            notifier.Info(msg);
        }

        public static void NotifyOnError(string msg)
        {
            notifier.Fatal(msg);
        }

        public static string GetLog()
        {
            var s = stringAppender.GetLog();
            stringAppender.ResetLog();
            return s;
        }
    }
}