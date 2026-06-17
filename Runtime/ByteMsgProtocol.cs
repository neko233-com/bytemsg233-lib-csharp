using System;

namespace ByteMsg233
{
    public readonly struct ByteMsgProtocolHello
    {
        public ByteMsgProtocolHello(ulong version, ulong fingerprint, ulong minCompatible)
        {
            Version = version;
            Fingerprint = fingerprint;
            MinCompatible = minCompatible;
        }

        public ulong Version { get; }
        public ulong Fingerprint { get; }
        public ulong MinCompatible { get; }
    }

    public static class ByteMsgProtocol
    {
        public static void WriteHello(ByteMsgWriter writer, ByteMsgProtocolHello hello)
        {
            writer.WriteULongField(1, hello.Version);
            writer.WriteULongField(2, hello.Fingerprint);
            writer.WriteULongField(3, hello.MinCompatible);
        }

        public static ByteMsgProtocolHello ReadHello(byte[] data)
        {
            var reader = new ByteMsgReader(data);
            ulong version = 0;
            ulong fingerprint = 0;
            ulong minCompatible = 0;

            while (!reader.IsEof)
            {
                var header = reader.ReadFieldHeader();
                switch (header.Tag)
                {
                    case 1:
                        version = reader.ReadVarint();
                        break;
                    case 2:
                        fingerprint = reader.ReadVarint();
                        break;
                    case 3:
                        minCompatible = reader.ReadVarint();
                        break;
                    default:
                        reader.SkipField(header.WireType);
                        break;
                }
            }

            return new ByteMsgProtocolHello(version, fingerprint, minCompatible);
        }

        public static void CheckCompatible(ByteMsgProtocolHello local, ByteMsgProtocolHello remote)
        {
            if (local.Fingerprint != 0 && remote.Fingerprint != 0 && local.Fingerprint != remote.Fingerprint)
            {
                throw new InvalidOperationException("ByteMsg233 protocol fingerprint mismatch.");
            }

            if (remote.Version < local.MinCompatible || local.Version < remote.MinCompatible)
            {
                throw new InvalidOperationException("ByteMsg233 protocol version mismatch.");
            }
        }
    }
}
