using System;
using System.Diagnostics;
using System.Text;
using PacketDotNet;
using SharpPcap;
using Bae.Models;
using PacketDotNet.Tcp;

namespace Bae.Services
{
    public class PacketProcessor
    {
        public static PacketInfo ProcessPacket(PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

            string protocol = "Unknown";
            string source = "Unknown";
            string destination = "Unknown";
            string info = "";
            string payload = "";

            if (packet is EthernetPacket ethernetPacket)
            {
                var ipPacket = ethernetPacket.PayloadPacket as IPPacket;
                var tcpPacket = ipPacket?.PayloadPacket as TcpPacket;
                var udpPacket = ipPacket?.PayloadPacket as UdpPacket;

                if (ipPacket != null)
                {
                    protocol = ipPacket.Protocol.ToString();
                    source = ipPacket.SourceAddress.ToString();
                    destination = ipPacket.DestinationAddress.ToString();

                    if (tcpPacket != null)
                    {
                        info = AnalyzeTcpPacket(tcpPacket);
                        payload = ExtractPayload(tcpPacket);
                    }
                    else if (udpPacket != null)
                    {
                        info = AnalyzeUdpPacket(udpPacket);
                        payload = ExtractPayload(udpPacket);
                    }
                }
            }
            else
            {
                // Handle non-Ethernet packets
                protocol = packet.GetType().Name;
                info = "Non-Ethernet packet";
                payload = ExtractPayload(packet);
            }

            return new PacketInfo
            {
                Timestamp = rawPacket.Timeval.Date,
                Source = source,
                Destination = destination,
                Protocol = protocol,
                Length = rawPacket.Data.Length,
                Info = info,
                Payload = payload
            };
        }

        private static string AnalyzeTcpPacket(TcpPacket tcpPacket)
        {
            StringBuilder info = new StringBuilder($"TCP {tcpPacket.SourcePort} -> {tcpPacket.DestinationPort}");

            // Access TCP flags
            if (tcpPacket.Synchronize) info.Append(" [SYN]");
            if (tcpPacket.Acknowledgment) info.Append(" [ACK]");
            if (tcpPacket.Finished) info.Append(" [FIN]");
            if (tcpPacket.Reset) info.Append(" [RST]");

            // Identify common protocols
            switch (tcpPacket.DestinationPort)
            {
                case 80: info.Append(" (HTTP)"); break;
                case 443: info.Append(" (HTTPS)"); break;
                case 22: info.Append(" (SSH)"); break;
                // Add more protocol identifications as needed
            }

            return info.ToString();
        }

        private static string AnalyzeUdpPacket(UdpPacket udpPacket)
        {
            StringBuilder info = new StringBuilder($"UDP {udpPacket.SourcePort} -> {udpPacket.DestinationPort}");
            
            // Identify common protocols
            switch (udpPacket.DestinationPort)
            {
                case 53: info.Append(" (DNS)"); break;
                case 67: case 68: info.Append(" (DHCP)"); break;
                // Add more protocol identifications as needed
            }

            return info.ToString();
        }

        private static string ExtractPayload(Packet packet)
        {
            if (packet.PayloadData != null && packet.PayloadData.Length > 0)
            {
                // Convert binary payload to hex string
                string hexPayload = BitConverter.ToString(packet.PayloadData).Replace("-", " ");
                
                // Try to interpret as ASCII if it seems to be text
                string asciiPayload = Encoding.ASCII.GetString(packet.PayloadData)
                    .Replace("\r", "\\r").Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                if (asciiPayload.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                {
                    // If it contains control characters, it's probably not text
                    return $"Hex: {hexPayload}";
                }
                else
                {
                    return $"ASCII: {asciiPayload}\nHex: {hexPayload}";
                }
            }
            return "No payload";
        }
    }
}