#region Using Directives

using System;

#endregion

namespace Endeca.Control.EacToolkit
{
    [Serializable]
    public class ControlScriptException : Exception
    {
        public ControlScriptException(string message) : base(message)
        {
        }
    }
}