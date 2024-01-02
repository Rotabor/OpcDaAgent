using System;
using System.Runtime.Remoting.Lifetime;

namespace nsDataRemoting {
    /// <summary>
    /// Leasing sponsor dummy
    /// Only requested public methods should be declared with no implementation
    /// </summary>
    public class MySponsor : MarshalByRefObject, ISponsor {
        /// <summary>
        /// Sets infinite lifetime for the sponsor
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService() => null;
        /// <summary>
        /// Renewal method called for servers's DataRemotingClient 
        /// </summary>
        /// <param name="lease">not used</param>
        /// <returns></returns>
        public TimeSpan Renewal(ILease lease) => TimeSpan.FromSeconds(30);
    }
    /// <summary>
    /// Class to receive data using CallbackHnadler
    /// </summary>
    [Serializable]
    public class ValueResult {
        /// <summary>
        /// Index matches the index in the array of parameters passed to DataRemotingClient.CreateGroup
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Timestamp provided by the data source
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Value boxed in the object
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Status of the value
        /// </summary>
        public int Satus { get; set; }
    }
    /// <summary>
    /// Callback handler dummy
    /// </summary>
    public class CallbackHandler : MarshalByRefObject {
        /// <summary>
        /// Remotely called callback method
        /// </summary>
        /// <param name="vra">Array of results</param>
        public void OnValueChanged(ValueResult[] vra) { }
    }
}
