using System;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TTRider.PowerShellAsync;

namespace chrispyduck.ps.NetTools
{
    [Cmdlet(VerbsDiagnostic.Ping, "Continuously")]
    public class ContinuousPing : AsyncCmdlet 
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public string[] Hosts { get; set; }

        [Parameter(Mandatory = false)]
        public int? Timeout { get; set; }

        [Parameter(Mandatory=false)]
        public int? Interval { get; set; }

        private HostRecord[] hostRecords;
        private SynchronizationContext context;
        private Ping ping;
        private int interval;
        private int timeout;
        private Task mainLoop;
        private CancellationTokenSource cts;

        protected override Task ProcessRecordAsync()
        {
            this.timeout = this.Timeout ?? 1000;
            this.interval = this.Interval ?? 1000;
            this.hostRecords = Task.WhenAll(this.Hosts.Select(this.PrepareHost)).Result;
            this.context = SynchronizationContext.Current;
            this.ping = new Ping();
            this.ping.PingCompleted += this.OnPingCompleted;
            this.cts = new CancellationTokenSource();
            return this.mainLoop = this.MainLoop();
        }

        private async Task<HostRecord> PrepareHost(string host)
        {
            try
            {
                IPAddress ip;
                if (IPAddress.TryParse(host, out ip))
                    return new HostRecord(host, ip);
                var entry = await Dns.GetHostEntryAsync(host);
                return new HostRecord(host, entry.AddressList.First());
            }
            catch (Exception e)
            {
                throw new Exception("Failed to resolve host '" + host + "' to an IP address", e);
            }
        }

        protected override Task StopProcessingAsync()
        {
            this.cts.Cancel();
            return this.mainLoop;
        }

        private async Task MainLoop()
        {
            var hrPairs = this.hostRecords.Select(r => new Tuple<HostRecord, Ping>(r, new Ping())).ToArray();

            // register event handlers
            foreach (var hrPair in hrPairs)
                hrPair.Item2.PingCompleted += this.OnPingCompleted;

            while (!base.Stopping && !this.cts.IsCancellationRequested)
            {
                foreach (var hrPair in hrPairs)
                {
                    var hostRecord = hrPair.Item1;
                    // skip if in progress
                    if (hostRecord.InProgress)
                        continue;
                    lock (hostRecord)
                    {
                        if (hostRecord.InProgress)
                            continue;
                        hostRecord.InProgress = true;
                    }

                    // send new ping
                    hostRecord.LastAttempt = DateTime.Now;
                    hrPair.Item2.SendAsync(hostRecord.IP, timeout, hostRecord);

                    await Task.Delay(interval, this.cts.Token);
                }
            }

            // unregister event handlers and cleanup
            foreach (var hrPair in hrPairs)
            {
                base.WriteVerbose($"Disposing of ping wrapper for host {hrPair.Item1.Host}");
                hrPair.Item2.PingCompleted -= this.OnPingCompleted;
                hrPair.Item2.Dispose();
            }

            base.WriteVerbose("Exiting main loop");
        }

        private void OnPingCompleted(object sender, PingCompletedEventArgs e)
        {
            var hostRecord = (HostRecord)e.UserState;
            hostRecord.RTT = e.Reply.RoundtripTime;
            hostRecord.LastStatus = e.Reply.Status;
            context.Send(x => this.WriteObject(hostRecord), null);
            lock (hostRecord) // i don't like this, but i'm feeling lazy right now
            {
                hostRecord.InProgress = false;
            }
        }

        class HostRecord
        {
            public HostRecord(string host, IPAddress ip)
            {
                this.Host = host;
                this.IP = ip;
                this.RTT = null;
                this.LastAttempt = DateTime.MinValue;
            }

            public string Host { get; }
            public IPAddress IP { get; }
            public decimal? RTT { get; set; }
            public IPStatus LastStatus { get; set; }
            public DateTime LastAttempt { get; set; }

            internal bool InProgress
            {
                [MethodImpl(MethodImplOptions.Synchronized)] get;
                [MethodImpl(MethodImplOptions.Synchronized)] set;
            }
        }
        
    }
}
