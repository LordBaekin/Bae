using System;

namespace Bae.Models
{
    public class PacketInfo
    {
        public DateTime Timestamp { get; set; }
        public required string Source { get; set; }
        public required string Destination { get; set; }
        public required string Protocol { get; set; }
        public int Length { get; set; }
        public required string Info { get; set; }
        public required string Payload { get; set; }
        public bool ShowPayload { get; set; }


        public PacketInfo()


        {
            Source = string.Empty;
            Destination = string.Empty;
            Protocol = string.Empty;
            Info = string.Empty;
            Payload = string.Empty;
        }
    }
}