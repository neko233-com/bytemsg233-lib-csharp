namespace ByteMsg233
{
    public readonly struct ByteMsgFieldHeader
    {
        public ByteMsgFieldHeader(int tag, ByteMsgWireType wireType)
        {
            Tag = tag;
            WireType = wireType;
        }

        public int Tag { get; }
        public ByteMsgWireType WireType { get; }
    }
}
