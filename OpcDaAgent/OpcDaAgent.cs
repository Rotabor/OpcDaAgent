using nsDataRemoting;
using System.Collections;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.ServiceProcess;
using System;
using System.Diagnostics;
using System.Configuration;
using System.Runtime.Remoting.Lifetime;

namespace OpcDaAgent {

    public partial class OpcDaAgent : ServiceBase {

        TcpChannel _tc;

        public OpcDaAgent() {
            InitializeComponent();
            try {
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
                RemotingConfiguration.ApplicationName = "DataRemote";
                RemotingConfiguration.RegisterActivatedServiceType(typeof(DataRemotingClient));
                ActivatedClientTypeEntry myActivatedClientTypeEntry =
                    new ActivatedClientTypeEntry(typeof(CallbackHandler),
                    $"tcp://{ConfigurationManager.AppSettings["remotehost"]}:{ConfigurationManager.AppSettings["port"]}/{RemotingConfiguration.ApplicationName}");
                RemotingConfiguration.RegisterActivatedClientType(myActivatedClientTypeEntry);
                LifetimeServices.LeaseTime = TimeSpan.FromSeconds(30);
                LifetimeServices.SponsorshipTimeout = TimeSpan.FromSeconds(20);
                LifetimeServices.RenewOnCallTime = TimeSpan.FromSeconds(5);
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Sart service"); }
        }

        protected override void OnStart(string[] args) {
            Trace.Listeners.Clear(); Trace.Listeners.Add(new TextWriterTraceListener($"{ConfigurationManager.AppSettings["logpath"]}\\Log.txt")); Trace.AutoFlush = true;
            try {
                TraceMsg("Service started");
                DataRemotingClient.Host = ConfigurationManager.AppSettings["host"];
                DataRemotingClient.Server = ConfigurationManager.AppSettings["server"];
                var port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
                _tc = new TcpChannel(new Hashtable() { ["port"] = port }, null, new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
                ChannelServices.RegisterChannel(_tc, false);
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Start service"); }
        }

        protected override void OnStop() {
            try {
                TraceMsg("Service stopped");
                ChannelServices.UnregisterChannel(_tc);
            }
            catch (Exception ex) { ErrorTraceEx(ex, "Stop service"); }
            Trace.Close(); Trace.Listeners.Clear();
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
