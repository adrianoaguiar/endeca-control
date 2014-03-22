#region Using Directives

using System;
using System.Collections.ObjectModel;

#endregion

namespace Endeca.Control.EacToolkit
{
    public class HostCollection : KeyedCollection<string, Host>
    {
        protected override string GetKeyForItem(Host item)
        {
            return item.HostId;
        }

        /// <summary>
        ///     Returns host by host name
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public Host GetHostByName(string hostName)
        {
            var hosts = new Host[Count];
            Items.CopyTo(hosts, 0);
            Predicate<Host> finder = delegate(Host item) { return item.EacHostName == hostName; };
            var result = Array.Find(hosts, finder);
            return result;
        }
    }
}