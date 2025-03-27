public class Globals
{
    public const string IB2AESKEY = " |FK}S];v]!!cw@E4l-gMXa9yDPvRfF*B";
}

public enum PackageType : byte
{
    VOTE,
    IB1,
    IB2,
    IB3
}

public static class FType
{
    public const string INT_PROPERTY = "IntProperty";
    public const string FLOAT_PROPERTY = "FloatProperty";
    public const string BYTE_PROPERTY = "ByteProperty";
    public const string BOOL_PROPERTY = "BoolProperty";
    public const string STR_PROPERTY = "StrProperty";
    public const string NAME_PROPERTY = "NameProperty";
    public const string STRUCT_PROPERTY = "StructProperty";
    public const string ARRAY_PROPERTY = "ArrayProperty";
    public const string NONE = "None";
}

public enum ValueType
{
    StructProperty,
    IntProperty,
    StrProperty,
    NameProperty,
    FloatProperty,
    BoolProperty,
    ByteProperty
}

