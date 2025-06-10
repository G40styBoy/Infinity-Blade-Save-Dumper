namespace SaveDumper.FArrayManager;

public enum ArrayType
{
    Static,
    Dynamic
}

public enum AlternateName
{
    None,
    CurrencyStruct,
    PlayerSavedStats,
    PlayerEquippedItemList,
    NumConsumable,
    GemCookerData,
    ItemForgeData,
    PotionCauldronData,
    SavedCheevoData,
    ShowConsumableBadge,
    LastEquippedWeaponOfType
}

public enum ArrayName
{
    // IB1 //
        PlaythroughItemsGiven,

    // IB2 //
        PlayerCookerGems,
        SuperBoss,
        ActiveBattlePotions,

    // VOTE //

    // IB3 //
        // Static Arrays
        Currency,
        Stats,
        NumConsumable,
        ShowConsumableBadge,
        GemCooker,
        ItemForge,
        PotionCauldron,
        SavedCheevo,
        LastEquippedWeaponOfType,

        // Dynamic Arrays
        EquippedItemNames,
        EquippedItems,
        LinkNotificationBadges,
        CurrentKeyItemList,
        UsedKeyItemList,
        PlayerInventory,
        PlayerUnequippedGems,
        CurrentStoreGems,
        InActivePotionList,
        ActivePotions,
        PurchasedPerks,
        GameFlagList,
        BossFixedWorldInfo,
        TouchTreasureAwards,
        WorldItemOrderList,
        TreasureChestOpened,
        BossesGeneratedThisBloodline,
        PotentialBossElementalAttacks,
        PerLevelData,
        CurrentBattleChallengeList,
        SavedPersistentBossData,
        HardCoreCurrentQuestData,
        LoggedAnalyticsAchievements,
        McpAuthorizedServices,

        // Subset Arrays
        SocketedGemData,
        Gems,
        Reagents,
        BossElementalRandList,
        PersistActorCounts,
        DontClearPersistActorCounts,
        SavedItems,
        Quests,
        PendingAction
}

public record ArrayMetadata
{
    public ArrayName ArrayName;
    public AlternateName AlternateName;
    public ValueType ValueType;
    public ArrayType ArrayType;

    public ArrayMetadata(ArrayName arrayName, AlternateName alternateName, ValueType valueType, ArrayType type)
    {
        ArrayName = arrayName;
        AlternateName = alternateName;
        ValueType = valueType;
        ArrayType = type;
    }

    public void PrintInfo()
    {
        Console.WriteLine($"Array Name: {ArrayName}");
        Console.WriteLine($"Alternate Name: {AlternateName}");
        Console.WriteLine($"Value Type: {ValueType}");
        Console.WriteLine($"Static: {ArrayType}\n");
    }
}


public class FArrayInitializer
{
    public static List<ArrayMetadata> PopulateArrayInfo()
    {
        var arraysToInstantiate = new List<ArrayMetadata>();
        arraysToInstantiate.AddRange(new List<ArrayMetadata>
        {
            // Static Arrays
            new ArrayMetadata(ArrayName.Currency, AlternateName.CurrencyStruct, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.Stats, AlternateName.PlayerSavedStats, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.NumConsumable, AlternateName.NumConsumable, ValueType.IntProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.ShowConsumableBadge, AlternateName.ShowConsumableBadge, ValueType.ByteProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.GemCooker, AlternateName.GemCookerData, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.ItemForge, AlternateName.ItemForgeData, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.PotionCauldron, AlternateName.PotionCauldronData, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.SavedCheevo, AlternateName.SavedCheevoData, ValueType.StructProperty, ArrayType.Static),
            new ArrayMetadata(ArrayName.LastEquippedWeaponOfType, AlternateName.LastEquippedWeaponOfType, ValueType.StrProperty, ArrayType.Static),

            // Dynamic Arrays
            new ArrayMetadata(ArrayName.EquippedItemNames, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.EquippedItems, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.LinkNotificationBadges, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.CurrentKeyItemList, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.UsedKeyItemList, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PlayerInventory, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PlayerUnequippedGems, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.CurrentStoreGems, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.InActivePotionList, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.ActivePotions, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PurchasedPerks, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.GameFlagList, AlternateName.None, ValueType.IntProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.BossFixedWorldInfo, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.TouchTreasureAwards, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.WorldItemOrderList, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.TreasureChestOpened, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.BossesGeneratedThisBloodline, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PotentialBossElementalAttacks, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PerLevelData, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.CurrentBattleChallengeList, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.SavedPersistentBossData, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.HardCoreCurrentQuestData, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.LoggedAnalyticsAchievements, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.McpAuthorizedServices, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),

            // Subset Arrays
            new ArrayMetadata(ArrayName.Gems, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.SocketedGemData, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.Reagents, AlternateName.None, ValueType.StrProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.BossElementalRandList, AlternateName.None, ValueType.FloatProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PersistActorCounts, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.DontClearPersistActorCounts, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.SavedItems, AlternateName.None, ValueType.NameProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.Quests, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.PendingAction, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),

            // IB2
            new ArrayMetadata(ArrayName.PlayerCookerGems, AlternateName.None, ValueType.StructProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.SuperBoss, AlternateName.None, ValueType.IntProperty, ArrayType.Dynamic),
            new ArrayMetadata(ArrayName.ActiveBattlePotions, AlternateName.None, ValueType.NameProperty, ArrayType.Dynamic),

            // IB1
            new ArrayMetadata(ArrayName.PlaythroughItemsGiven, AlternateName.None, ValueType.NameProperty, ArrayType.Dynamic)

        });

        return arraysToInstantiate;
    }
}
