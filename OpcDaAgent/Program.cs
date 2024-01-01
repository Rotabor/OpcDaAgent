using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;

namespace OpcDaAgent {

    public static class Program {

        static void Main() {
            Trace.Listeners.Add(new TextWriterTraceListener($"{ConfigurationManager.AppSettings["logpath"]}\\Log.txt")); Trace.AutoFlush = true;
            ServiceBase.Run(new ServiceBase[] { new OpcDaAgent() });
        }
    }
}
