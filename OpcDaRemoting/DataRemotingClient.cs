using Opc.Da;
using Opc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Remoting.Lifetime;
using System.Threading;

namespace nsDataRemoting {

    public class DataRemotingClient : MarshalByRefObject, IDisposable {

        #region [Fields]
        static public string Host, Server;
        static internal readonly Dictionary<int, string> DVOPCDAErrCodes = new Dictionary<int, string>() {
            { 264193, "E_NOTCOMMUNICATEWITHDEVICEBUTITEMADDED" }, // 0x00040801
            { 264204, "E_NODEVICECONNECTED" }, // 0x0004080C
            { 264301, "E_NOCBENTRYPOINTPROVIDED" }, // 0x0004086D
            { -2147022986, "E_OBJECTEXPORTERNOTFOUND" }, // 0x80070776
            { -2146959355, "E_SERVEREXECUTIONFAILED" }, // 0x80080005
            { -1073479679, "E_INVALIDHANDLE" }, // 0xC0040001 
            { -1073479678, "E_PARAMETERDUPLICATION" }, // 0xC0040002
            { -1073479677, "E_LOCALEIDNOTSUPPORTED" }, // 0xC0040003
            { -1073477632, "E_NOTCOMMUNICATEWITHDEVICE" }, // 0xC0040800
            { -1073477630, "E_FBMODEDISCREPANCY" }, // 0xC0040802
            { -1073477629, "E_COULDNOTOPENDATABASEFORBROWSING" }, // 0xC0040803
            { -1073477628, "E_NOBRANCHESFORBROWSE" }, // 0xC0040804
            { -1073477627, "E_SUPPLIEDISNOTVALIDFORNAME" }, // 0xC0040805
            { -1073477626, "E_FBITEMREADONLY" }, // 0xC0040806
            { -1073477625, "E_SOFTWAREREVISIONDOESMATCHFORWORKSTATION" }, // 0xC0040807
            { -1073477624, "E_DEVICENAMECOULDNOTBEFOUND" }, // 0xC0040808
            { -1073477623, "E_DEVICEIDCOULDNOTBEFOUND" }, // 0xC0040809
            { -1073477622, "E_NODEVICEASSOCIATEDTHISOBJECTANYMORE" }, // 0xC004080A
            { -1073477621, "E_LICENCELIMETEXCEEDED1" }, // 0xC004080B
            { -1073477620, "E_LICENCELIMETEXCEEDED2" }, // 0xC004080C
            { -1073477532, "E_INCONSISTENTHANDLER" }, // 0xC0040864
            { -1073477531, "E_GROUPNOTINHANDLER" }, // 0xC0040865
            { -1073477530, "E_ITEMNOTINHANDLER" }, // 0xC0040866
            { -1073477529, "E_HANDLERCOUNTEROVERFLOWS" }, // 0xC0040867
            { -1073477528, "E_NOINTERFACEPOINTERTOREMOTEOBJECT" }, // 0xC0040868
            { -1073477527, "E_NOREALCONNECTIONFOUND" }, // 0xC0040869
            { -1073477526, "E_SOLITARYGROUP" }, // 0xC004086A
            { -1073477525, "E_SOLITARYITEM" }, // 0xC004086B
            { -1073477524, "E_NOTRANSACTIONFORID" }, // 0xC004086C
        };
        Opc.Da.Server _server; CallbackHandler _cbh; bool _disposed = false; ILease _lts; Timer _tmr;
        #endregion Fields

        ~DataRemotingClient() { if (!_disposed) Dispose(); }

        public bool Initialize() {
            bool result = false;
            try {
                _server = new Opc.Da.Server(new OpcCom.Factory(), new URL($"opcda://{Host}/{Server}"));
                _lts = (ILease)GetLifetimeService();
                _tmr = new Timer(OnFire, null, 30000, 10000);
                result = true;
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Initialize"); }
            TraceMsg($"Initialize: {result}");
            return result;
        }

        void OnFire(object state) {
            if (_lts.CurrentState == LeaseState.Expired) { _tmr?.Dispose(); Dispose(); TraceMsg($"{_lts.CurrentState}"); }
        }

        public bool Connect() {
            bool result = false;
            try {
                _server.Connect(new ConnectData(null, null));
                result = true;
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Connect"); }
            TraceMsg($"Connect: {result}");
            return result;
        }

        public bool CreateGroup(string[] sa) {
            bool result = false;
            try {
                if (sa == null || sa.Length == 0) return result; //  || !_server.IsConnected
                TraceMsg("Going to create the group");
                if (_cbh == null) _cbh = new CallbackHandler(); ((ILease)_cbh.GetLifetimeService()).Register(new MySponsor());
                Subscription group = null;
                int i = 0;
                var items = Array.ConvertAll(sa, p => new Item() { ItemName = p, ClientHandle = i++ });
                group = (Subscription)_server?.CreateSubscription(new SubscriptionState() { Name = "Default group", UpdateRate = 1000, Active = true, ClientHandle = 1 });
                var r = group.AddItems(items);
                foreach (var itm in r) if (itm.ResultID.Code != 0) {
                        int err = itm.ResultID.Code;
                        if (!DVOPCDAErrCodes.TryGetValue(err, out string errdesc)) errdesc = itm.ResultID.Name.Name;
                        Trace.WriteLine($"OPC parameter '{itm.ItemName}' connection error '{errdesc}'");
                    }
                if (group != null) group.DataChanged += OnValueChange;
                result = true;
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Create group"); }
            TraceMsg($"CreateGroup: {result}");
            return result;
        }

        void OnValueChange(object subscriptionHandle, object requestHandle, ItemValueResult[] values) {
            try {
                var measurements = new ValueResult[values.Length]; int i = 0;
                foreach (var ivr in values) measurements[i++] = new ValueResult() {
                    Index = (int)ivr.ClientHandle, Timestamp = ivr.Timestamp, Value = ivr.Value,
                    Satus = ivr.Quality.GetCode()
                };
                _cbh.OnValueChanged(measurements);
            }
            catch (Exception ex) { ErrorTraceEx(ex, "OnValueChange"); }
        }

        public bool Disconnect() {
            bool result = false;
            try {
                if (_server.IsConnected) {
                    foreach (Subscription group in _server.Subscriptions) _server.CancelSubscription(group);
                    _server.Disconnect();
                }
                result = true;
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Disconnect"); }
            TraceMsg($"Disconnect: {result}");
            return result;
        }

        public void Dispose() {
            if (_disposed) return;
            try {
                try {
                    if (_server.IsConnected) {
                        foreach (Subscription group in _server.Subscriptions) _server.CancelSubscription(group);
                        _server.Disconnect();
                    }
                }
                catch (Exception ex) { ErrorTraceEx(ex, "Disconnect in Dispose"); }
                _tmr?.Dispose(); _server.Dispose(); _disposed = true;
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Dispose"); }
            TraceMsg($"Dispose: done");
        }

        static void TraceMsg(string msg) {
            Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}");
        }

        static void ErrorTraceEx(Exception ex, string msg) {
            string exmsg = string.Concat($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}: ",
                ex.Message, "\r\nSource: ", ex.Source, "\r\nMember: ", ex.TargetSite, "\r\nStack trace: ", ex.StackTrace);
            if (ex.InnerException != null) exmsg = String.Concat(exmsg, "\r\nInnerException: ", ex.InnerException.Message, "\r\nSource: ",
                ex.InnerException.Source, "\r\nMember: ", ex.InnerException.TargetSite, "\r\nStack trace: ", ex.InnerException.StackTrace);
            Trace.WriteLine(exmsg);
        }
    }
}
