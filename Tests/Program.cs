using System;
using System.Collections.Generic;
using ByteMsg233;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

var writer = new ByteMsgWriter();
writer.WriteUIntField(1, 123);
writer.WriteStringField(2, "Hero");
writer.WriteEnumField(3, (int)HeroState.Moving);
writer.WriteListField(4, new List<string> { "a", "b" }, (w, value) => w.WriteString(value));
writer.WriteMapField(5, new Dictionary<string, string> { ["hp"] = "99" }, (w, key) => w.WriteString(key), (w, value) => w.WriteString(value));

var reader = new ByteMsgReader(writer.ToArray());
var hero = Hero.Rent();

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
        case 5:
            hero.Attrs = reader.ReadMap(r => r.ReadString(), r => r.ReadString());
            break;
        default:
            reader.SkipField(header.WireType);
            break;
    }
}

Assert(hero.Id == 123, "id should roundtrip");
Assert(hero.Name == "Hero", "name should roundtrip");
Assert(hero.State == HeroState.Moving, "enum should restore");
Assert(hero.Tags.Count == 2 && hero.Tags[1] == "b", "list should roundtrip");
Assert(hero.Attrs["hp"] == "99", "map should roundtrip");

hero.Release();
var reused = Hero.Rent();
Assert(reused.Id == 0, "pool should reset id");
Assert(reused.Tags.Count == 0, "pool should reset list");

Console.WriteLine("ByteMsg233 C# runtime tests passed.");

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
    public Dictionary<string, string> Attrs { get; set; } = new();

    public static Hero Rent() => Pool.Rent();
    public void Release() => Pool.Return(this);

    public void Reset()
    {
        Id = 0;
        Name = string.Empty;
        State = HeroState.Idle;
        Tags.Clear();
        Attrs.Clear();
    }
}
