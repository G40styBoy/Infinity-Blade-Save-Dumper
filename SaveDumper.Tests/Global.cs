class Global
{
    public const string BaseLocation = @"C:\Users\G40sty\Documents\VS Code\Infinity Blade\Infinity Blade Save Dumper\BACKUP\IB3 Backup";

    public const string save1_IB3 = $@"{BaseLocation}\Afc save.bin";
    public const string save2_IB3 = $@"{BaseLocation}\Bu's Most Recent Save.bin";
    public const string save3_IB3 = $@"{BaseLocation}\Bu's Save.bin";
    public const string save4_IB3 = $@"{BaseLocation}\Testing Save From US_$.bin";
    public const string save5_IB3 = $@"{BaseLocation}\Max Stats save.bin";

    public static IEnumerable<object[]> SaveList =>
    new List<object[]>
    {
        new object[] { save1_IB3 },
        new object[] { save2_IB3 },
        new object[] { save3_IB3 },
        new object[] { save4_IB3 },
        new object[] { save5_IB3 },
    };
}