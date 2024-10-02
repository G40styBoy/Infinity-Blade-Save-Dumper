using System.IO;
public class Globals
{
    //public const string saveFile = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Input\\*.bin"; 
    public static string[] saveFile = Directory.GetFiles("C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Input", "*.bin");
    public const string outputFile = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Deserialized Saves\\Deserialized.json"; 
    public const string binaryOutput = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\Serialized Save Output\\UnencryptedSave0.bin"; 
    public const string someOuput = "C:\\Users\\G40sty\\Documents\\VS Code\\Infinity Blade\\Save Editor\\SAVE\\someOutput.txt"; 

    public const int nullTerm = 1;
    public const int packageBegin = 8;
    public const int InfoGrab = 4;


    #pragma warning disable IDE0051
    #pragma warning disable CS0169

    // these are types that arent structs
    public struct DynamicArrayTypes
    {
        private readonly string EquippedItemNames;
        private readonly string CurrentKeyItemList;
        private readonly string EquippedItems;
        private readonly string UsedKeyItemList;
        private readonly string PurchasedPerks;
        private readonly int GameFlagList;
        private readonly string WorldItemOrderList;
        private readonly string TreasureChestOpened;
        private readonly string BossesGeneratedThisBloodline;
        private readonly string PotentialBossElementalAttacks;
        private readonly string CurrentBattleChallengeList;
        private readonly string LoggedAnalyticsAchievements;
        private readonly string McpAuthorizedServices;
        private readonly float BossElementalRandList;
        public enum DynamicArrayList
        {
            EquippedItemNames,
            EquippedItems,
            CurrentKeyItemList,
            UsedKeyItemList,
            PurchasedPerks,
            GameFlagList,
            WorldItemOrderList,
            TreasureChestOpened,
            BossesGeneratedThisBloodline,
            PotentialBossElementalAttacks,
            CurrentBattleChallengeList,
            LoggedAnalyticsAchievements,
            McpAuthorizedServices,
            BossElementalRandList,
        }
    }


    public struct StaticArrayTypes
    {

        private readonly int NumConsumable;
        private readonly byte ShowConsumableBadge;
        private readonly string LastEquippedWeaponOfType;
        private readonly struct Currency;
        private readonly struct Stats;
        private readonly struct CharacterEquippedList;
        private readonly struct GemCooker;
        private readonly struct ItemForge;
        private readonly struct PotionCauldron;
        private readonly struct SavedCheevo;
        public enum StaticArrayNameList
        {
            NumConsumable,
            ShowConsumableBadge,
            LastEquippedWeaponOfType,
            Currency,
            Stats,
            CharacterEquippedList,
            GemCooker,
            ItemForge,
            PotionCauldron,
            SavedCheevo
        }
        public enum StaticArrayAltVariableList
        {
            NumConsumable,
            ShowConsumableBadge,
            LastEquippedWeaponOfType,
            CurrencyStruct,
            PlayerSavedStats,
            PlayerEquippedItemList,
            GemCookerData,
            ItemForgeData,
            PotionCauldronData,
            SavedCheevoData
        }
    }

    #pragma warning restore CS0169
    #pragma warning restore IDE0051


    public enum eTouchRewardActor
    {
        TRA_Random,                     // 0
        TRA_Random_Potion,              // 1
        TRA_Random_Gold,                // 2
        TRA_Random_Key,                 // 3
        TRA_Random_Gem,                 // 4
        TRA_Random_Item,                // 5
        TRA_Random_World,               // 6
        TRA_None,                       // 7
        TRA_Gold_Small,                 // 8
        TRA_Gold_Medium,                // 9
        TRA_Gold_Large,                 // 10
        TRA_Key_Small,                  // 11
        TRA_Key_Medium,                 // 12
        TRA_Key_Large,                  // 13
        TRA_Key_Item,                   // 14
        TRA_Gem_Fixed,                  // 15
        TRA_Item_Fixed,                 // 16
        TRA_Item_Weapon,                // 17
        TRA_Item_Shield,                // 18
        TRA_Item_Armor,                 // 19
        TRA_Item_Helmet,                // 20
        TRA_Item_Magic,                 // 21
        TRA_GrabBag_Small,              // 22
        TRA_GrabBag_Medium,             // 23
        TRA_GrabBag_Large,              // 24
        TRA_GrabBag_SmallGem,           // 25
        TRA_GrabBag_MediumGem,          // 26
        TRA_GrabBag_LargeGem,           // 27
        TRA_GrabBag_Uber,               // 28
        TRA_Potion_HealthL,             // 29
        TRA_Potion_Fixed,               // 30
        TRA_World_1_Cactus,             // 31
        TRA_World_2_Berries,            // 32
        TRA_World_3_PinkPlant,          // 33
        TRA_World_4_Reeds,              // 34
        TRA_World_5_DesertBulb,         // 35
        TRA_World_6_Mushroom,           // 36
        TRA_World_7_Root,               // 37
        TRA_World_8_Butterfly,          // 38
        TRA_World_9_Skullfly,           // 39
        TRA_World_10_Cocoon,            // 40
        TRA_World_11_Bones,             // 41
        TRA_World_12_Future,            // 42
        TRA_World_13_Future,            // 43
        TRA_World_14_Future,            // 44
        TRA_World_15_Future,            // 45
        TRA_World_16_Future,            // 46
        TRA_Chips_Small,                // 47
        TRA_Chips_Medium,               // 48
        TRA_Chips_Large,                // 49
        TRA_MAX                         // 50
    };


    enum eAchievements
    {
        A_NONE,                         // 0
        TRACK_Combo,                    // 1
        TRACK_Parry,                    // 2
        TRACK_Dodge,                    // 3
        TRACK_Block,                    // 4
        TRACK_Magic,                    // 5
        TRACK_BonusCombo,               // 6
        TRACK_SuperMove,                // 7
        TRACK_Stab,                     // 8
        TRACK_FinishingHit,             // 9
        TRACK_PerfectParry,             // 10
        TRACK_PerfectBlock,             // 11
        TRACK_SuperDodge,               // 12
        TRACK_SirisLevel,               // 13
        TRACK_IsaLevel,                 // 14
        TRACK_MasteredItem,             // 15
        TRACK_Forge,                    // 16
        TRACK_ItemLevel,                // 17
        TRACK_GemCook,                  // 18
        TRACK_PotionCraft,              // 19
        TRACK_PotionUse,                // 20
        TRACK_GrabBags,                 // 21
        TRACK_Ingredients,              // 22
        TRACK_Merchant,                 // 23
        TRACK_ClashMob,                 // 24
        GOAL_Gold,                      // 25
        GOAL_InfinityBlade,             // 26
        GOAL_Combat1,                   // 27
        GOAL_Combat2,                   // 28
        GOAL_Combat3,                   // 29
        GOAL_Combat4,                   // 30
        GOAL_Combat5,                   // 31
        GOAL_Combat6,                   // 32
        PROG_Story1,                    // 33
        PROG_Story2,                    // 34
        PROG_Story3,                    // 35
        PROG_Story4,                    // 36
        PROG_Story5,                    // 37
        PROG_Story6,                    // 38
        PROG_Story7,                    // 39
        PROG_Story8,                    // 40
        PROG_Story9,                    // 41
        PROG_Story10,                   // 42
        PROG_Story11,                   // 43
        PROG_Story12,                   // 44
        PROG_NewGamePlus,               // 45
        TRACK_TitanKill,                // 46
        TRACK_Slash,                    // 47
        TRACK_RealPerfectParry,         // 48
        TRACK_ArenaMode,                // 49
        TRACK_DeathlessQuest,           // 50
        TRACK_BossPerkKils,             // 51
        GOAL_Collector1,                // 52
        GOAL_Collector2,                // 53
        GOAL_HolidayHelm1,              // 54
        GOAL_HolidayHelm2,              // 55
        GOAL_HolidayHelm3,              // 56
        GOAL_HolidayHelm4,              // 57
        GOAL_HolidayHelm5,              // 58
        GOAL_HolidayHelm6,              // 59
        GOAL_HolidayHelm7,              // 60
        GOAL_HolidayHelm8,              // 61
        GOAL_HolidayHelm9,              // 62
        GOAL_HolidayHelm10,             // 63
        GOAL_HolidayHelmAll,            // 64
        TRACK_AvatarEquipmentCost,      // 65
        AIB3_MAX_CHEEVO,                // 66
        TRACK_Weekly,                   // 67
        eAchievements_MAX               // 68
    };
}