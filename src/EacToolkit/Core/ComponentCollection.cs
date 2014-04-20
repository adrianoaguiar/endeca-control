#region Using Directives

using System;
using System.Collections.ObjectModel;

#endregion

namespace EndecaControl.EacToolkit.Core
{
    public class ComponentCollection<T> : KeyedCollection<string, T> where T : Component
    {
        /// <summary>
        ///     When implemented in a derived class, extracts the key from the specified element.
        /// </summary>
        /// <returns>
        ///     The key for the specified element.
        /// </returns>
        /// <param name="item">The element from which to extract the key.</param>
        protected override string GetKeyForItem(T item)
        {
            return item.ComponentId;
        }

        /// <summary>
        ///     Searches for a component by hostId
        /// </summary>
        /// <param name="hostId"></param>
        /// <returns> Component that matches the hostId</returns>
        public T FindOne(string hostId)
        {
            var t = new T[Count];
            Items.CopyTo(t, 0);
            Predicate<T> finder = delegate(T item) { return item.HostId == hostId; };
            var result = Array.Find(t, finder);
            return result;
        }

        /// <summary>
        ///     Retrieves all components that match the hostId
        /// </summary>
        /// <param name="hostId"></param>
        /// <returns>Array of components that match the hostId</returns>
        public T[] FindAll(string hostId)
        {
            var t = new T[Count];
            Items.CopyTo(t, 0);
            Predicate<T> finder = delegate(T item) { return item.HostId == hostId; };
            var result = Array.FindAll(t, finder);
            return result;
        }
    }
}