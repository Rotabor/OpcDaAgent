using System;
using System.Runtime.Remoting.Lifetime;

namespace nsDataRemoting {

    public class MySponsor : MarshalByRefObject, ISponsor {

        public override object InitializeLifetimeService() => null;

        public TimeSpan Renewal(ILease lease) => TimeSpan.FromSeconds(30);
    }

    [Serializable]
    public class ValueResult {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public object Value { get; set; }
        public int Satus { get; set; }
    }

    public class CallbackHandler : MarshalByRefObject {

        public void OnValueChanged(ValueResult[] vra) { }
    }
}
