# bytemsg233-lib-csharp

Unity-friendly C# runtime for `bytemsg233`.

This repository is designed for two use cases:

- Unity projects through UPM with `com.neko233.bytemsg233`
- generated C# code from `bytemsg233`

The runtime stays small and native-feeling: writer, reader, single-threaded object pool, enum helpers, and clean collection helpers without external dependencies.

## Unity Install

Add this Git URL in Unity Package Manager:

```text
https://github.com/neko233-com/bytemsg233-lib-csharp.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.neko233.bytemsg233": "https://github.com/neko233-com/bytemsg233-lib-csharp.git"
  }
}
```

Copy-based install from the main repository:

```bash
bytemsg233 install-lib csharp --to ./Assets/Plugins/ByteMsg233
```

## Runtime Shape

```csharp
using ByteMsg233;

public enum HeroState
{
    Idle = 0,
    Moving = 1,
    Dead = 2,
}

public sealed class Hero : IByteMsgResettable
{
    private static readonly ByteMsgPool<Hero> Pool = new(() => new Hero());

    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public HeroState State { get; set; } = HeroState.Idle;
    public List<string> Tags { get; set; } = new();

    public static Hero Rent() => Pool.Rent();
    public void Release() => Pool.Return(this);

    public void Reset()
    {
        Id = 0;
        Name = string.Empty;
        State = HeroState.Idle;
        Tags.Clear();
    }

    public byte[] Encode()
    {
        var writer = new ByteMsgWriter();
        writer.WriteUIntField(1, Id);
        writer.WriteStringField(2, Name);
        writer.WriteEnumField(3, (int)State);
        writer.WriteListField(4, Tags, (w, value) => w.WriteString(value));
        return writer.ToArray();
    }

    public static Hero Decode(byte[] data)
    {
        var hero = Rent();
        var reader = new ByteMsgReader(data);

        while (!reader.IsEof)
        {
            var header = reader.ReadFieldHeader();
            switch (header.Tag)
            {
                case 1:
                    hero.Id = (uint)reader.ReadVarint();
                    break;
                case 2:
                    hero.Name = reader.ReadString();
                    break;
                case 3:
                    hero.State = ByteMsgEnum.FromValue<HeroState>((int)reader.ReadVarint());
                    break;
                case 4:
                    hero.Tags = reader.ReadList(r => r.ReadString());
                    break;
                default:
                    reader.SkipField(header.WireType);
                    break;
            }
        }

        return hero;
    }
}
```

## API

- `ByteMsgWriter`: write varint, zigzag, string, bytes, message, list, map, and field helpers
- `ByteMsgReader`: read and skip fields with bounded length checks
- `ByteMsgPool<T>`: Unity-safe object pool for generated models
- `ByteMsgEnum`: enum value restore and validation helpers

## Development

```bash
dotnet test
```
