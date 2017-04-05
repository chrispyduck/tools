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
        private bool stop = false;

        protected override Task ProcessRecordAsync()
        {
            this.timeout = this.Timeout ?? 1000;
            this.interval = this.Interval ?? 1500;
            if (interval < timeout)
                interval = timeout;
            this.hostRecords = Task.WhenAll(this.Hosts.Select(this.PrepareHost)).Result;
            this.context = SynchronizationContext.Current;
            this.ping = new Ping();
            this.ping.PingCompleted += this.OnPingCompleted;
            return this.MainLoop();
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
            this.stop = true;
            return Task.FromResult(0);
        }

        private async Task MainLoop()
        {
            while (!base.Stopping && !this.stop)
            {
                foreach (var hostRecord in hostRecords)
                {
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
                    ping.SendAsync(hostRecord.IP, timeout, hostRecord);

                    await Task.Delay(interval);
                }
            }
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
